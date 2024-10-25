using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
            _logger.LogWarning("Giriş sayfasına erişildi. Zaman: {Time}", DateTime.Now);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(SignInDto signInDto)
        {
            try
            {
                var startTime = DateTime.Now;
                _logger.LogWarning("Kullanıcı: {Username} için giriş denemesi. Zaman: {Time}", signInDto.Username, startTime);

                // Token'ı al ve kontrol et
                var token = await _identityService.SignIn(signInDto); // Token burada string olarak dönecek

                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("Token alınamadı. Giriş işlemi başarısız.");
                }

                // Token'ı oturumda sakla
                HttpContext.Session.SetString("AccessToken", token); // "AccessToken" anahtarı ve token değeri

                _logger.LogCritical("Kullanıcı: {Username} için başarılı giriş.", signInDto.Username);
                return RedirectToAction("UserList", "User");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Kullanıcı: {Username} için başarısız giriş denemesi.", signInDto.Username);
                ModelState.AddModelError("", "Giriş başarısız. Lütfen tekrar deneyin.");
                return View(signInDto);
            }
        }



    }
}
