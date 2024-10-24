// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace IdentityServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            try
            {
                var seed = args.Contains("/seed");
                if (seed)
                {
                    LogInformation(host, "Seeding database...");
                    var config = host.Services.GetRequiredService<IConfiguration>();
                    var connectionString = config.GetConnectionString("DefaultConnection");
                    SeedData.EnsureSeedData(connectionString);
                    LogInformation(host, "Done seeding database.");
                    return 0;
                }

                LogInformation(host, "Starting host...");
                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                LogError(host, ex, "Host terminated unexpectedly.");
                return 1;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void LogInformation(IHost host, string message)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation(message);
        }

        private static void LogError(IHost host, Exception ex, string message)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, message);
        }
    }
}
