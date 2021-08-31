using System;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CommandLine;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Reporter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Options options = null;
            using var host = CreateHostBuilder(args).Build();
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => options = o);
            var appSettings = host.Services.GetService<AppSettings>();
            var r = host.Services.GetService<RedisDecorator>();

            if(options.Data != null){
                try{
                    var newMessages = JsonSerializer.Deserialize<IEnumerable<Message>>(options.Data);
                    var ids = await r.AppendMessage(newMessages);
                    ids.ForEach(id => Console.WriteLine($"Added Message ID = {id}"));
                }catch(Exception e){
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            } else {
                var messages = await r.ReadNewMessages();
                foreach(var m in messages){
                    Console.WriteLine($"Message: {m.PONumber}");
                }
                foreach(var message in messages){
                    if(message.Label == null
                        || message.PONumber == null
                        || message.SKU == null
                        || message.QTY == 0){
                            throw new Exception("Message needs all the properties set.");
                        }
                }
            }

            host.Start();
            await host.StopAsync();
        }
        public static IHostBuilder CreateHostBuilder(string[] args){
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables().Build();

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging()
                        .Configure<AppSettings>(config)
                        .AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>());
                    
                    var connectionString = $"{hostContext.Configuration["RedisHost"]},password={hostContext.Configuration["RedisPassword"]}";
                    var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                    var redisPersistence = new RedisDecorator(connectionMultiplexer, hostContext.Configuration["StreamKey"],
                        hostContext.Configuration["ConsumerGroup"], hostContext.Configuration["ConsumerGroupId"], "0-0", (didSucceed, ex) => {
                        Console.WriteLine($"Was it successful {didSucceed} {ex.Message}");
                    });
                    services.AddSingleton<RedisDecorator>(redisPersistence);
                });
        }
        public class Options
        {
            [Option('d', "data", Required= false, HelpText = "Add a message to the stream: -d '{\"Label\": \"some label\", PONumber: \"234388\", SKU=\"6672\", QTY=\"23423\"}'")]
            public string Data {get;set;}
        }
    }
}
