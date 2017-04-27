using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class Startup
    {
        public IConfiguration Configure()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(env))
            {
                throw new Exception("Environment variable ASPNETCORE_ENVIRONMENT must be set");
            }

            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("Settings//appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"Settings//appsettings.{env}.json", optional: false, reloadOnChange: true)
               .AddJsonFile("Settings//loggingsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"Settings//loggingsettings.{env}.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
              .ReadFrom.ConfigurationSection(configuration.GetSection("Logging"))
              .CreateLogger();

            Log.Logger.Information($"Application started with {env} environment variable.");
            
            return configuration;
        }
    }
}
