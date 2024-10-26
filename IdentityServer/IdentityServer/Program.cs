// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;

namespace IdentityServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning() // Log seviyesini buradan ayarlayabilirsiniz
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Microsoft loglarını filtrele
                .MinimumLevel.Override("System", LogEventLevel.Warning)    // Sistem loglarını filtrele
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                var host = CreateHostBuilder(args).Build();

                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;

                    try
                    {
                        var seed = args.Contains("/seed");
                        if (seed)
                        {
                            Log.Information("Veritabanı oluşturuluyor...");
                            var config = services.GetRequiredService<IConfiguration>();
                            var connectionString = config.GetConnectionString("DefaultConnection");
                            SeedData.EnsureSeedData(connectionString);
                            Log.Information("Veritabanı oluşturma işlemi tamamlandı.");
                            return 0;
                        }

                        Log.Information("Uygulama başlatılıyor...");
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Uygulama başlatılırken bir hata oluştu.");
                        return 1;
                    }
                }

                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Uygulama beklenmedik bir şekilde sonlandırıldı.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() // Serilog'u kullan
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}