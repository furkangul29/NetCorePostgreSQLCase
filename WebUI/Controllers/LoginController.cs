using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebUI.DTO.IdentityDtos.LoginDtos;
using WebUI.Services.IdentityServices;

namespace WebUI.Controllers
{
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIdentityService _identityService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            IHttpClientFactory httpClientFactory,
            IIdentityService identityService,
            ILogger<LoginController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _identityService = identityService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Login page accessed at {Time}", DateTime.Now);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(SignInDto signInDto)
        {
            try
            {
                var startTime = DateTime.Now;
                _logger.LogInformation(
                    "Login attempt for user: {Username} at {Time}",
                    signInDto.Username,
                    startTime);

                await _identityService.SignIn(signInDto);

                _logger.LogInformation(
                    "Successful login for user: {Username}",
                    signInDto.Username);

                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed login attempt for user: {Username}",
                    signInDto.Username);

                ModelState.AddModelError("", "Login failed. Please try again.");
                return View(signInDto);
            }
        }
    }
}