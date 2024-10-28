using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authentication;
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
            var currentTime = DateTime.Now;
            _logger.LogInformation("Giriş sayfasına erişildi. Erişim zamanı: {Time}", currentTime);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(SignInDto signInDto)
        {
            var startTime = DateTime.Now;
            _logger.LogInformation("Giriş işlemi başlatıldı. Kullanıcı: {Username}, Zaman: {Time}", signInDto.Username, startTime);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Giriş formu geçersiz model durumu ile gönderildi. Kullanıcı: {Username}", signInDto.Username);
                ModelState.AddModelError("", "Geçersiz giriş bilgileri. Lütfen tekrar deneyin.");
                return BadRequest(ModelState);
            }

            try
            {
                // Token alımı sürecini başlatma
                _logger.LogInformation("Token alımı başlatıldı. Kullanıcı: {Username}", signInDto.Username);
                var token = await _identityService.SignIn(signInDto);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token alımı başarısız. Token alınamadı. Kullanıcı: {Username}", signInDto.Username);
                    ModelState.AddModelError("", "Giriş başarısız. Lütfen tekrar deneyin.");
                    return BadRequest(ModelState);
                }

                // Token'ı oturumda saklama
                HttpContext.Session.SetString("AccessToken", token);
                _logger.LogInformation("Token oturumda saklandı. Kullanıcı: {Username}", signInDto.Username);

               
                TempData["ShowWelcomeMessage"] = true;
                TempData["Username"] = signInDto.Username;

                _logger.LogInformation("Kullanıcı başarıyla giriş yaptı: {Username}. Giriş zamanı: {Time}", signInDto.Username, DateTime.Now);
                return Ok(new { success = true }); // Başarılı yanıt döndür
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Giriş işlemi sırasında bir hata oluştu. Kullanıcı: {Username}", signInDto.Username);
                ModelState.AddModelError("", "Giriş başarısız. Lütfen tekrar deneyin.");
                return BadRequest(ModelState);
            }
            finally
            {
                var endTime = DateTime.Now;
                _logger.LogInformation("Giriş işlemi tamamlandı. Kullanıcı: {Username}, Süre: {Duration} ms", signInDto.Username, (endTime - startTime).TotalMilliseconds);
            }
        }
        
        
        [HttpPost]
        public async Task<IActionResult> LogOut()
        {
            var username = HttpContext.Session.GetString("Username");
            var startTime = DateTime.Now;
            _logger.LogInformation("Çıkış işlemi başlatıldı. Kullanıcı: {Username}, Zaman: {Time}", username, startTime);

            try
            {
                // Session'daki token ve diğer bilgileri temizle
                HttpContext.Session.Remove("AccessToken");
                HttpContext.Session.Remove("Username");
                HttpContext.Session.Remove("ShowWelcomeMessage");

                // Identity işlemleri
                await HttpContext.SignOutAsync();

                _logger.LogInformation("Kullanıcı başarıyla çıkış yaptı: {Username}. Çıkış zamanı: {Time}",
                    username, DateTime.Now);

                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çıkış işlemi sırasında bir hata oluştu. Kullanıcı: {Username}", username);
                TempData["Error"] = "Çıkış işlemi sırasında bir hata oluştu.";
                return RedirectToAction("Index", "Login");
            }
            finally
            {
                var endTime = DateTime.Now;
                _logger.LogInformation("Çıkış işlemi tamamlandı. Kullanıcı: {Username}, Süre: {Duration} ms",
                    username, (endTime - startTime).TotalMilliseconds);
            }
        }

        [HttpGet("LogOut")]
        public IActionResult LogoutPage()
        {
            _logger.LogInformation("Çıkış sayfasına erişildi. Erişim zamanı: {Time}", DateTime.Now);
            return RedirectToAction("Index", "Login");
        }
    }
}
