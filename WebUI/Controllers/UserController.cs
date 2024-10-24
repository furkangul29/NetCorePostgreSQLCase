using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebUI.Services.UserIdentityServices;

namespace WebUI.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserIdentityService userIdentityService,
            ILogger<UserController> logger)
        {
            _userIdentityService = userIdentityService;
            _logger = logger;
        }

        public async Task<IActionResult> UserList()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "Anonymous";
                _logger.LogInformation(
                    "User {UserId} accessing user list at {Time}",
                    userId,
                    DateTime.Now);

                var userList = await _userIdentityService.GetAllUserListAsync();

                _logger.LogInformation(
                    "Retrieved {Count} users from database",
                    userList.Count());

                return View(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while retrieving user list");
                throw;
            }
        }
    }
}