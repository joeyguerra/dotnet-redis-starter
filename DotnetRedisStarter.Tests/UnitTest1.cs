using System;
using Xunit;

namespace DotnetRedisStarter.Tests
{
    [Collection("Main Collection")]
    public class UnitTest1
    {
        private readonly MainFixture _fixture;

        public UnitTest1(MainFixture fixture){
            _fixture = fixture;
        }
        
        [Fact]
        public async void Test1()
        {
            var messageId = await _fixture.RedisPersistence.AddMessage("test", "test:docs", "joey");
            var entries = await _fixture.RedisPersistence.ReadAllMessages("test", "test:docs");
            foreach(var e in entries){
                foreach(var kv in e.Values){
                    Console.WriteLine($"{e.Id}: {kv.Name} = {kv.Value}");
                }
            }
            Assert.True(messageId.HasValue);
            Assert.True(entries.Length > 0);

        }
    }
}
