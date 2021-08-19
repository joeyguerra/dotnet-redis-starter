using System;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Reporter
{
    class Program
    {
        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables().Build();

                webBuilder.UseUrls("http://*:7070");
                webBuilder.UseStartup<Startup>();
            }).Build().Run();

            Console.WriteLine("Reporter service has started.");
        }
    }
}
