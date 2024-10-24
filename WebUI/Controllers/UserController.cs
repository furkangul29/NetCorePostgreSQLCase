using Microsoft.AspNetCore.Mvc;
using WebUI.UserIdentityServices;

namespace WebUI.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserIdentityService _userIdentityService;
        public UserController(IUserIdentityService userIdentityService)
        {
            _userIdentityService = userIdentityService;
       
        }

        public async Task<IActionResult> UserList()
        {
            var userList = await _userIdentityService.GetAllUserListAsync();
            return View(userList);
        }
    }
}
