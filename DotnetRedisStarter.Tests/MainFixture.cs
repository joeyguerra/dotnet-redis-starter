
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using Xunit;
using Reporter;

namespace DotnetRedisStarter.Tests
{
    public class MainFixture : IDisposable
    {
        public AppSettings AppSettings { get; }
        public RedisPersistence RedisPersistence {get;}
        private readonly IServiceCollection _services;
        private readonly IServiceProvider _provider;

        public MainFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            _services = new ServiceCollection()
                .AddLogging()
                .Configure<AppSettings>(configurationBuilder)
                .AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);
            
            _provider = _services.BuildServiceProvider();
            AppSettings = _provider.GetService<AppSettings>();
            var connectionString = $"{AppSettings.RedisHost},password={AppSettings.RedisPassword}";
            var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            RedisPersistence = new RedisPersistence(connectionMultiplexer, "0-0", (didSucceed, ex) => {
                Console.WriteLine($"Was it successful {didSucceed} {ex.Message}");
            });
            _services.AddSingleton<RedisPersistence>(RedisPersistence);

            try{
                RedisPersistence.StreamCreateConsumerGroup("test", "test:docs", "0-0");
            }catch(RedisException re) {
                Console.WriteLine(re.Message);
            }

        }
        public void Dispose(){

        }
    }

    [CollectionDefinition("Main Collection")]
    public class MainCollection : ICollectionFixture<MainFixture>
    {

    }
}