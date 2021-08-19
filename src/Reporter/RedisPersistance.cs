using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

public class RedisPersistence
{   
    ConnectionMultiplexer redis;
    IDatabase db;
    public const string WORKLOGS_PENDING_STREAM = "worklogs:pending";
    public const string CONSUMER_GROUP_MANAGERS = "worklogs:pending:users:managers";
    public const string WORKLOGS_APPROVALS_STREAM = "worklogs:approvals";
    public const string CONSUMER_GROUP_APPROVERS = "worklogs:approvals:users:approvers";
    public const string WORKLOGS_BILLING_STREAM = "worklogs:billing";
    public const string CONSUMER_GROUP_ACCOUNTING = "worklogs:billing:users:accounting";
    const string KEY_ASSIGNMENT_ID = "AssignmentId";

    public RedisPersistence(ConnectionMultiplexer connectionMultiplexer)
    {
        redis = connectionMultiplexer;
        db = redis.GetDatabase();

        try
        {
            var success = db.StreamCreateConsumerGroup(WORKLOGS_APPROVALS_STREAM, CONSUMER_GROUP_APPROVERS, "0-0");
            Console.WriteLine($"Created CG {CONSUMER_GROUP_APPROVERS} for stream {WORKLOGS_APPROVALS_STREAM}. Status: {success}");
        }
        catch(RedisException exc)
        {
            Console.WriteLine(exc.Message);
        }

        try
        {
            var success = db.StreamCreateConsumerGroup(WORKLOGS_BILLING_STREAM, CONSUMER_GROUP_ACCOUNTING, "0-0");
            Console.WriteLine($"Created CG {CONSUMER_GROUP_ACCOUNTING} for stream {WORKLOGS_BILLING_STREAM}. Status {success}");
        }
        catch(RedisException exc)
        {
            Console.WriteLine(exc.Message);
        }
    }

    public async Task<RedisValue> AddMessage(string stream, string key, RedisValue value)
    {
        Console.WriteLine($"Redis::Adding {key} {value} to stream {stream}");
        var messageId = await db.StreamAddAsync(stream, key, value);
        return messageId;
    }

    public async Task<StreamEntry[]> ReadMessages(string stream, string consumerGroup)
    {
        var pendingMessages = await db.StreamReadGroupAsync(stream, consumerGroup, $"{consumerGroup}:consumer_1", StreamPosition.Beginning);
        var newMessages = await db.StreamReadGroupAsync(stream, consumerGroup, $"{consumerGroup}:consumer_1", StreamPosition.NewMessages);
        var messages = pendingMessages.Concat(newMessages).ToArray();
        Console.WriteLine($"Read {messages.Count()} messages from stream {stream} group {consumerGroup}");
        return messages;
    }

    public async Task<StreamEntry[]> ReadStream(string stream)
    {
        var messages = await db.StreamReadAsync(stream, StreamPosition.Beginning);
        return messages;
    }

    public async Task<string> GetMessageId(string stream, string assignmentId)
    {
        var messages = await ReadStream(stream);
        var message = messages.LastOrDefault(x => x.Values.First(y => y.Name == KEY_ASSIGNMENT_ID).Value == assignmentId);
        return message.Id;
    }

    public async Task<string> GetPendingMessageId(string stream, string consumerGroup, string assignmentId)
    {
        var messages = await ReadMessages(stream, consumerGroup);
        var message = messages.FirstOrDefault(x => x.Values.First(y => y.Name == KEY_ASSIGNMENT_ID).Value == assignmentId);
        return message.Id;
    }

    public async Task<long> AcknowledgeMessage(string stream, string consumerGroup, RedisValue messageId)
    {
        var result = await db.StreamAcknowledgeAsync(stream, consumerGroup, messageId);
        return result;
    }
}
