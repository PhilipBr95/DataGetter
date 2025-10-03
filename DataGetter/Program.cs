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
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.Sources.Clear();
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("Config/hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "APP_");
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("Config/appsettings.json", optional: true);
                    configApp.AddJsonFile($"Config/appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    configApp.AddEnvironmentVariables();
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<Settings>(provider =>
                    {
                        //IOptions???
                        var settings = new Settings();
                        return settings;
                    });
                    services.AddTransient<IMqttService, MqttService>();
                    services.AddHostedService<ConsoleService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.ClearProviders();
                    configLogging.AddSimpleConsole(options =>
                    {
                        options.TimestampFormat = "HH:mm:ss "; // Format: Hour:Minute:Second                     
                        options.SingleLine = true; // Optional: log to a single line
                    });                    
                    configLogging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}
