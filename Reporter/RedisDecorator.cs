using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.ComponentModel;

namespace Reporter
{
    public class RedisDecorator
    {
        ConnectionMultiplexer _connectionMultiplexer;
        IDatabase _db;
        
        private string streamKey;
        private string consumerGroup;
        private string consumerGroupId;

        public RedisDecorator(ConnectionMultiplexer connectionMultiplexer, string streamKey, string consumerGroup, string consumerGroupId, string initialPosition, Action<bool, Exception> done)
        {
            this.consumerGroup = consumerGroup;
            this.streamKey = streamKey;
            this.consumerGroupId = consumerGroupId;
            _connectionMultiplexer = connectionMultiplexer;
            _db = _connectionMultiplexer.GetDatabase();
            bool didSucceed = false;
            Exception e = null;
            try{
                didSucceed = this.StreamCreateConsumerGroup(streamKey, consumerGroup, initialPosition);
            }catch(RedisServerException ex){
                didSucceed = false;
                e = ex;
            }
            done(didSucceed, e);
        }
        public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position = null, bool createStream = true, CommandFlags flags = CommandFlags.None)
        {
            return _db.StreamCreateConsumerGroup(key, groupName, position, createStream, flags);
        }

        public async Task<List<string>> AppendMessage(IEnumerable<Message> messages)
        {
            var messageIds = new List<string>();
            var type = typeof(Message);
            var properties = type.GetProperties();
            foreach(var message in messages){
                try{
                    var entries = properties.Select(prop => new NameValueEntry(prop.Name, prop.GetValue(message)?.ToString()))?.ToArray();
                    var messageId = await _db.StreamAddAsync(new RedisKey(streamKey), entries, null, null, false, CommandFlags.None);
                    messageIds.Add(messageId);
                }catch(Exception e){
                    throw new Exception($"Message is missing fields/data.", e);
                }
            }
            return messageIds;
        }

        public async Task<List<Message>> ReadNewMessages(){
            var entries = await _db.StreamReadGroupAsync(streamKey, this.consumerGroup, $"{this.consumerGroup}_1", StreamPosition.NewMessages);
            if(entries.Length == 0) return new List<Message>();
            
            var header = entries?[0].Id;
            var messages = new List<Message>();
            foreach(var entry in entries){
                if(entry.Id == header){
                    var m = new Message();
                    var type = m.GetType();
                    foreach(var e in entry.Values){
                        if(e.Value.HasValue){
                            var property = type.GetProperty(e.Name);
                            var converter = TypeDescriptor.GetConverter(e.Value);
                            if(property.PropertyType.Name == "Int32"){
                                property?.SetValue(m, int.Parse(e.Value.ToString()));
                            } else {
                                property?.SetValue(m, e.Value.ToString());
                            }
                        }
                    }
                    messages.Add(m);
                }
                header = entry.Id;
            }
            return messages;
        }
        public async Task<RedisValue> StreamAddAsync(string key, KeyValuePair<string, string>[] entries, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            var redisEntries = entries.Select(kv => new NameValueEntry(kv.Key, kv.Value)).ToArray<NameValueEntry>();
            return await _db.StreamAddAsync(new RedisKey(key), redisEntries, messageId, maxLength, useApproximateMaxLength, flags);
        }

        public async Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            return await _db.StreamReadAsync(key, position, count, flags);
        }
        public async Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None){
            return await _db.StreamAcknowledgeAsync(key, groupName, messageId, flags);
        }
        public async Task<KeyValuePair<string, string>[]> StreamReadGroupAsync(string stream, string group, string consumerName){
            var messages = await _db.StreamReadGroupAsync(stream, group, consumerName, StreamPosition.NewMessages);
            return messages.SelectMany(se => se.Values.Select(v => new KeyValuePair<string, string>(v.Name, v.Value))).ToArray();
        }
        public async Task<StreamEntry[]> ReadAllTheMessagesForThisConsumerGroup()
        {
            var pendingMessages = await _db.StreamReadGroupAsync(streamKey, consumerGroup, consumerGroupId, StreamPosition.Beginning);
            var newMessages = await _db.StreamReadGroupAsync(streamKey, consumerGroup, consumerGroupId, StreamPosition.NewMessages);
            var messages = pendingMessages.Concat(newMessages).ToArray();
            return messages;
        }

        public async Task<StreamEntry[]> ReadStreamFromBeginning(string stream)
        {
            var messages = await _db.StreamReadAsync(stream, StreamPosition.Beginning);
            return messages;
        }

        // Example: handler = x => x.Values.First(y => y.Name == "DocID").Value == docI
        public async Task<string> GetMessageId(string stream, Func<StreamEntry, bool> handler)
        {
            var messages = await ReadStreamFromBeginning(stream);
            var message = messages.LastOrDefault(handler);
            return message.Id;
        }

        // Example: handler = x => x.Values.First(y => y.Name == "DocID").Value == docId
        public async Task<string> GetPendingMessageId(Func<StreamEntry, bool> handler)
        {
            var messages = await ReadAllTheMessagesForThisConsumerGroup();
            var message = messages.FirstOrDefault(handler);
            return message.Id;
        }
    }
}