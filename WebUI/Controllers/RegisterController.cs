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

namespace WebUI.Controllers
{
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
        public IActionResult Index()
        {
            // Token ve roleId'yi oturumdan al
            var token = _tokenService.GetToken();
            var roleId = HttpContext.Session.GetString("RoleId");


            // Kayıt formunu döndür
            var model = new CreateUserViewModel { RoleId = roleId,Token=token };
            return View(model); // Register.cshtml sayfasına gönderir
        }

        [HttpPost]
        public async Task<IActionResult> Index(CreateUserViewModel createUserViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(createUserViewModel);
            }

            // Şifre doğrulama
            if (createUserViewModel.Password != createUserViewModel.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Şifreler eşleşmiyor.");
                return View(createUserViewModel);
            }

            try
            {
                // RoleId'yi Session'dan alın
                var roleId = HttpContext.Session.GetInt32("RoleId");

                if (!roleId.HasValue)
                {
                    ModelState.AddModelError("", "Oturum bilgileriniz geçersiz. Lütfen tekrar giriş yapın.");
                    return RedirectToAction("Index", "Login");
                }

                // Kullanıcı kaydı için DTO oluşturun
                var registerDto = new
                {
                    Username = createUserViewModel.Username,
                    Password = createUserViewModel.Password,
                    Email = createUserViewModel.Email,
                    Name = createUserViewModel.Name,
                    Surname = createUserViewModel.Surname,
                    RoleId = roleId.Value
                };

                // Token ile client oluşturup istek gönderin
                var client = _tokenService.CreateClientWithToken(); // Token'i kullanarak client oluşturun
                var jsonData = JsonConvert.SerializeObject(registerDto);
                var stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://localhost:3001/api/Registers", stringContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla oluşturuldu.";
                    return RedirectToAction("Index", "Login");
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

                ModelState.AddModelError("", errorMessage);
                return View(createUserViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı sırasında bir hata oluştu");
                ModelState.AddModelError("", "İşlem sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(createUserViewModel);
            }
        }

    }

    // API'den gelen hata mesajları için yardımcı sınıf
    public class ErrorResponse
        {
            public string Message { get; set; }
            public object Errors { get; set; }
        }

    }
