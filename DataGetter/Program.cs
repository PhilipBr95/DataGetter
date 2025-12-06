using Microsoft.Extensions.Logging; 
using DataGetter.Models;
using DataGetter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataGetter
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .WithMethods("GET", "HEAD", "OPTIONS");
                });
            });
            builder.Services.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddSimpleConsole(consoleOptions =>
                {
                    consoleOptions.TimestampFormat = "HH:mm:ss ";
                    consoleOptions.SingleLine = true;
                });
                options.AddDebug().SetMinimumLevel(LogLevel.Debug);
            });

            builder.Services.AddTransient<Settings>(provider =>
            {
                //IOptions???
                var settings = new Settings();
                return settings;
            });

            builder.Services.AddSingleton<IConsoleService, ConsoleService>();
            builder.Services.AddTransient<IMqttService, MqttService>();
            builder.Services.AddHostedService<ConsoleService>();
            builder.Services.AddMemoryCache();

            var app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowSpecificOrigins");
            //app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();


            //var host = new HostBuilder()
            //    .ConfigureHostConfiguration(configHost =>
            //    {
            //        configHost.Sources.Clear();
            //        configHost.SetBasePath(Directory.GetCurrentDirectory());
            //        configHost.AddJsonFile("Config/hostsettings.json", optional: true);
            //        configHost.AddEnvironmentVariables(prefix: "APP_");
            //        configHost.AddCommandLine(args);
            //    })
            //    .ConfigureAppConfiguration((hostContext, configApp) =>
            //    {
            //        configApp.AddJsonFile("Config/appsettings.json", optional: true);
            //        configApp.AddJsonFile($"Config/appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
            //        configApp.AddEnvironmentVariables();
            //        configApp.AddCommandLine(args);
            //    })
            //    .ConfigureServices((hostContext, services) =>
            //    {
            //        services.AddTransient<Settings>(provider =>
            //        {
            //            //IOptions???
            //            var settings = new Settings();
            //            return settings;
            //        });
            //        services.AddTransient<IMqttService, MqttService>();
            //        services.AddHostedService<ConsoleService>();
            //    })
            //    .ConfigureLogging((hostContext, configLogging) =>
            //    {
            //        configLogging.ClearProviders();
            //        configLogging.AddSimpleConsole(options =>
            //        {
            //            options.TimestampFormat = "HH:mm:ss "; // Format: Hour:Minute:Second                     
            //            options.SingleLine = true; // Optional: log to a single line
            //        });                    
            //        configLogging.AddDebug();
            //    })
            //    .UseConsoleLifetime()
            //    .Build();

            //await host.RunAsync();
        }
    }
}
