using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebUI.Models; // CreateUserViewModel ve diğer modellerin bulunduğu namespace
using Microsoft.Extensions.Configuration;
using WebUI.Services.UserServices;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using WebUI.DTO.IdentityDtos.RegisterDtos;
using static Duende.IdentityServer.IdentityServerConstants;
using System.Net.Http.Headers;
using WebUI.Services.TokenServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme, Policy = "users.write")]
    public class RegisterController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RegisterController> _logger;
        private readonly ITokenService _tokenService;

        public RegisterController(IConfiguration configuration, IUserService userService, IHttpClientFactory httpClientFactory, ILogger<RegisterController> logger, ITokenService tokenService)
        {
            _configuration = configuration;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _tokenService = tokenService;
        }

        [HttpGet]
        [RequireScope("users.write")]
        public IActionResult Index()
        {
            // Token ve roleId'yi oturumdan al
            var token = _tokenService.GetToken();
            var roleId = HttpContext.Session.GetString("RoleId");

            _logger.LogInformation("Kayıt formu gösteriliyor. Token: {Token}, RoleId: {RoleId}", token, roleId);

            // Kayıt formunu döndür
            var model = new CreateUserViewModel { RoleId = roleId, Token = token };
            return View(model); // Register.cshtml sayfasına gönderir
        }

        [HttpPost]
        [RequireScope("users.write")]
        public async Task<IActionResult> Index(CreateUserViewModel createUserViewModel)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Geçersiz model durumu. Kayıt formu yeniden gösteriliyor.");
                return View(createUserViewModel);
            }

            // Şifre doğrulama
            if (createUserViewModel.Password != createUserViewModel.ConfirmPassword)
            {
                _logger.LogWarning("Şifreler eşleşmiyor.");
                ModelState.AddModelError("ConfirmPassword", "Şifreler eşleşmiyor.");
                return View(createUserViewModel);
            }

            try
            {
 
                   // Kullanıcı kaydı için DTO oluşturun
                   var registerDto = new
                {
                    Username = createUserViewModel.Username,
                    Password = createUserViewModel.Password,
                    Email = createUserViewModel.Email,
                    Name = createUserViewModel.Name,
                    Surname = createUserViewModel.Surname,
                    RoleId = createUserViewModel.RoleId
                };

                _logger.LogInformation("Kayıt işlemi başlatılıyor. Kullanıcı: {Username}, Email: {Email}, RoleId: {RoleId}",
                    createUserViewModel.Username, createUserViewModel.Email, createUserViewModel.RoleId);

                // Token ile client oluşturup istek gönderin
                var client = _tokenService.CreateClientWithToken(); // Token'i kullanarak client oluşturun
                var jsonData = JsonConvert.SerializeObject(registerDto);
                var stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");

                _logger.LogInformation("Kayıt isteği API'ye gönderiliyor: {Endpoint}", "http://localhost:3001/api/Registers");

                var response = await client.PostAsync("http://localhost:3001/api/Registers", stringContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Kullanıcı başarıyla oluşturuldu: {Username}", createUserViewModel.Username);
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla oluşturuldu.";
                    return RedirectToAction("Index", "Login");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) // 400 Bad Request kontrolü
                {
                     responseContent = await response.Content.ReadAsStringAsync();
                    // API'den dönen hata mesajını alın ve ModelState'e ekleyin
                    var error = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ModelState.AddModelError(error.Key, error.Message);
                    return View(createUserViewModel);
                }

                // API'den dönen hata mesajını kontrol et ve ekrana yansıt
                var errorMessage = "Kayıt işlemi başarısız oldu.";
                try
                {
                    var error = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    errorMessage = error?.Message ?? responseContent;
                }
                catch
                {
                    errorMessage = responseContent;
                }

                _logger.LogWarning("API isteği başarısız. Hata: {ErrorMessage}", errorMessage);
                ModelState.AddModelError("", errorMessage);
                return View(createUserViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı sırasında bir hata oluştu. Kullanıcı: {Username}, Email: {Email}", createUserViewModel.Username, createUserViewModel.Email);
                ModelState.AddModelError("", "İşlem sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(createUserViewModel);
            }
        }

    }

    // API'den gelen hata mesajları için yardımcı sınıf
    public class ErrorResponse
    {
        public string Key { get; set; } // Bu özellik eksikti
        public string Message { get; set; }
    }

}
