using System;
using System.Collections.Generic;
using Reporter;
using Xunit;

namespace DotnetRedisStarter.Tests.Integration
{
    [Collection("Main Collection")]
    public class RedisDecoratorTest
    {
        private readonly MainFixture _fixture;

        public RedisDecoratorTest(MainFixture fixture){
            _fixture = fixture;
        }
        
        ///Label - SKU - QTY - PO Number - Total Amount - Submitter - Status

        [Fact]
        public async void AddMultipleKeyValuePairs(){
            var expected = new Message(){
                Label ="/namespace/somefilename.xls",
                SKU = "12343433888",
                QTY = 500,
                PONumber = "8879988",
                TotalAmount = 20000000,
                Submitter = "Mo!!!!",
                Status = "what are valid statii?"
            };
            var messageId = await _fixture.RedisPersistence.AppendMessage(expected);
            var actual = await _fixture.RedisPersistence.ReadNewMessages();
            Assert.Equal<Message>(expected, actual[0]);
        }
    }
}
