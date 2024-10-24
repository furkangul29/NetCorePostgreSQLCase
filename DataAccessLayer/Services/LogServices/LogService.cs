using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;

namespace DataAccessLayer.Services.LogServices
{
  
    public class LogService : ILogService
    {
        private readonly object _lockObject = new object();
        private readonly string _logBasePath;
        private readonly ILogger<LogService> _logger;

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
            _logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            EnsureLogDirectoryExists();
        }

        private void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_logBasePath))
                {
                    Directory.CreateDirectory(_logBasePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log dizini oluşturulurken hata oluştu");
            }
        }

        public void LogInfo(string message, string userId = null)
        {
            WriteToLog("INFO", message, null, userId);
        }

        public void LogWarning(string message, string userId = null)
        {
            WriteToLog("WARNING", message, null, userId);
        }

        public void LogError(string message, Exception ex = null, string userId = null)
        {
            WriteToLog("ERROR", message, ex, userId);
        }

        public void LogUserActivity(string userId, string action, string details)
        {
            var message = $"Action: {action}, Details: {details}";
            WriteToLog("ACTIVITY", message, null, userId);
        }

        internal void LogRequest(string logType, object logEntry)
        {
            try
            {
                var json = JsonConvert.SerializeObject(logEntry, Formatting.Indented);
                WriteToFile(logType, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request log yazılırken hata oluştu");
            }
        }

        private void WriteToLog(string level, string message, Exception ex = null, string userId = null)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    UserId = userId ?? "System",
                    Message = message,
                    Exception = ex == null ? null : new
                    {
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        InnerException = ex.InnerException?.Message
                    }
                };

                var json = JsonConvert.SerializeObject(logEntry, Formatting.Indented);
                WriteToFile("WebUI", json);
            }
            catch (Exception writeEx)
            {
                _logger.LogError(writeEx, "Log yazılırken hata oluştu");
            }
        }

        private void WriteToFile(string logType, string content)
        {
            var fileName = $"{logType}_{DateTime.Now:yyyy-MM-dd}.log";
            var fullPath = Path.Combine(_logBasePath, fileName);

            lock (_lockObject)
            {
                try
                {
                    var logEntry = $"{content}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
                    File.AppendAllText(fullPath, logEntry, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Dosyaya yazma hatası: {fullPath}");
                }
            }
        }
    }

    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var startTime = DateTime.Now;
                var logType = context.Request.Path.StartsWithSegments("/api") ? "API" : "WebUI";

                // Request başlangıç loglaması
                var requestInfo = new
                {
                    Timestamp = startTime,
                    User = context.User?.Identity?.Name ?? "Anonymous",
                    Path = context.Request.Path.Value,
                    Method = context.Request.Method,
                    QueryString = context.Request.QueryString.Value,
                    ClientIP = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString()
                };

                await _next(context);

                // Request bitiş loglaması
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                var fullLogEntry = new
                {
                    Request = requestInfo,
                    Response = new
                    {
                        StatusCode = context.Response.StatusCode,
                        Duration = $"{duration:F2}ms"
                    }
                };

                var logService = context.RequestServices.GetRequiredService<ILogService>();
                ((LogService)logService).LogRequest(logType, fullLogEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request işlenirken hata oluştu");
                throw;
            }
        }
    }

    public static class LoggingExtensions
    {
        public static IServiceCollection AddCustomLogging(this IServiceCollection services)
        {
            services.AddSingleton<ILogService, LogService>();
            return services;
        }

        public static IApplicationBuilder UseCustomLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<LoggingMiddleware>();
        }
    }
}