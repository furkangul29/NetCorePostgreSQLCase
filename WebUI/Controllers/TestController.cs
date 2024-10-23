using Microsoft.AspNetCore.Mvc;
using WebUI.Interfaces;

namespace WebUI.Controllers
{
    public class TestController : Controller
    {
        private readonly IUserService _userService;
        public TestController(IUserService userService)
        {
            _userService = userService;

        }
        public async Task<IActionResult> Index()
        {
            var values = await _userService.GetUserInfo();
            return View(values);
        }

    }
}
