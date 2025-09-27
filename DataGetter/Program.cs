using DataGetter.Services;

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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<Settings>(provider =>
                    {
                        var settings = new Settings();

                        settings.Image.Password = provider.GetRequiredService<IConfiguration>()
                                                          .GetValue<string>("Image_Password");

                        if(string.IsNullOrEmpty(settings.Image.Password))
                        {
                            var logger = provider.GetRequiredService<ILogger<Settings>>();
                            logger.LogError("No image password set");
                        }

                        return settings;
                    });
                    services.AddTransient<IImageService, ImageService>();
                    services.AddTransient<IMqttService, MqttService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.ClearProviders();
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}
