using System;
using Xunit;

namespace DotnetRedisStarter.Tests.Integration
{
    [Collection("Main Collection")]
    public class RedisPersistanceTest
    {
        private readonly MainFixture _fixture;

        public RedisPersistanceTest(MainFixture fixture){
            _fixture = fixture;
        }
        
        [Fact]
        public async void AddReadAndAcknowledgeMessages()
        {
            var messageId = await _fixture.RedisPersistence.AddMessage("test", "Name", "Joey Guerra");
            await _fixture.RedisPersistence.AddMessage("test", "Name", "John Smith");
            var entries = await _fixture.RedisPersistence.ReadAllMessages("test", "test:docs");
            foreach(var e in entries){
                await _fixture.RedisPersistence.StreamAcknowledgeAsync("test", "test:docs", e.Id);
                foreach(var kv in e.Values){
                    Console.WriteLine($"{e.Id}: {kv.Name} = {kv.Value}");
                }
            }

            Assert.True(messageId.HasValue);
            Assert.True(entries.Length > 0);

        }
    }
}
