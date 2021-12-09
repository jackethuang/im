using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace imServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
        .MinimumLevel.Debug()
#else
        .MinimumLevel.Information()
#endif
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File($"Logs/{DateTime.Now:yyyy/MMdd}/logs.txt")
        .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }


        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Limits.MaxRequestBodySize = 10485760;
                        // Set properties and call methods on options
                    });
                    webBuilder
                    .UseKestrel(option =>
                    {
                        option.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(20);
                        option.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(20);
                    });
                    webBuilder.UseStartup<Startup>()
                    .UseUrls("http://*:5000");
                })
                .UseSerilogDefault(config =>
                {
                    config.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                          .WriteTo.File(Path.Combine(@"Log", "log.txt"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
                });


    }
}
