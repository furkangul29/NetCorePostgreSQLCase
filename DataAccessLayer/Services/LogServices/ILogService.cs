using System;

namespace DataAccessLayer.Services.LogServices
{
    public interface ILogService
    {
        void LogInfo(string message, string userId = null);
        void LogWarning(string message, string userId = null);
        void LogError(string message, Exception ex = null, string userId = null);
        void LogUserActivity(string userId, string action, string details);
    }
}
