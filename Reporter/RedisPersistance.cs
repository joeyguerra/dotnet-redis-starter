using System;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Reporter
{
    public class RedisPersistence
    {
        ConnectionMultiplexer _connectionMultiplexer;
        IDatabase _db;
        public const string STREAM_KEY = "incoming";
        public const string GROUP_NAME = "incoming:docs";

        public RedisPersistence(ConnectionMultiplexer connectionMultiplexer, string initialPosition, Action<bool, Exception> done)
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

        public async Task<RedisValue> StreamAddAsync(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return await _db.StreamAddAsync(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags);
        }

        public async Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            return await _db.StreamReadAsync(key, position, count, flags);
        }
        public async Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None){
            return await _db.StreamAcknowledgeAsync(key, groupName, messageId, flags);
        }

        public async Task<RedisValue> AddMessage(RedisKey key, RedisValue field, RedisValue value)
        {
            var messageId = await _db.StreamAddAsync(key, field, value);
            return messageId;
        }

        public async Task<StreamEntry[]> ReadAllMessages(RedisKey stream, RedisValue consumerGroup)
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
            var messages = await ReadAllMessages(stream, consumerGroup);
            var message = messages.FirstOrDefault(handler);
            return message.Id;
        }
    }
}