using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System;
namespace Reporter
{
    public class Startup
    {
        private const string VERSION_FOR_SWAGGER = "Reporter v1";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = $"{Configuration["RedisHost"]},password={Configuration["RedisPassword"]}";
            var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            var redisPersistence = new RedisDecorator(connectionMultiplexer, Configuration["StreamKey"],
                Configuration["ConsumerGroup"], Configuration["ConsumerGroupId"], "0-0", (didSucceed, ex) => {
                Console.WriteLine($"Was it successful {didSucceed} {ex.Message}");
            });
            services.AddSingleton<RedisDecorator>(redisPersistence);
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Reporter", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", VERSION_FOR_SWAGGER));
            }

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
