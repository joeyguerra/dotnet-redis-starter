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
        public const string STREAM_KEY = "incoming";
        public const string GROUP_NAME = "incoming:docs";

        public RedisDecorator(ConnectionMultiplexer connectionMultiplexer, string initialPosition, Action<bool, Exception> done)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _db = _connectionMultiplexer.GetDatabase();
            bool didSucceed = false;
            Exception e = null;
            try{
                didSucceed = this.StreamCreateConsumerGroup(STREAM_KEY, GROUP_NAME, initialPosition);
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

        public async Task<string> AppendMessage(Message message)
        {
            var entries = message.GetType().GetProperties().Select(prop => new NameValueEntry(prop.Name, prop.GetValue(message).ToString())).ToArray();
            var messageId = await _db.StreamAddAsync(new RedisKey(STREAM_KEY), entries, null, null, false, CommandFlags.None);
            return messageId.HasValue ? messageId.ToString() : null;
        }

        public async Task<List<Message>> ReadNewMessages(){
            var entries = await _db.StreamReadGroupAsync(STREAM_KEY, GROUP_NAME, $"{GROUP_NAME}_1", StreamPosition.NewMessages);
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
        public async Task<StreamEntry[]> ReadAllTheMessagesForThisConsumerGroup(RedisKey stream, RedisValue consumerGroup)
        {
            var pendingMessages = await _db.StreamReadGroupAsync(stream, consumerGroup, $"{consumerGroup}:consumer_1", StreamPosition.Beginning);
            var newMessages = await _db.StreamReadGroupAsync(stream, consumerGroup, $"{consumerGroup}:consumer_1", StreamPosition.NewMessages);
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
        public async Task<string> GetPendingMessageId(string stream, string consumerGroup, Func<StreamEntry, bool> handler)
        {
            var messages = await ReadAllTheMessagesForThisConsumerGroup(stream, consumerGroup);
            var message = messages.FirstOrDefault(handler);
            return message.Id;
        }
    }
}