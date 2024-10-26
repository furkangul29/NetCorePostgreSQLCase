using IdentityServer.Dtos;
using IdentityServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using IdentityServer.Tools;
using System.Threading.Tasks;
using System;

namespace IdentityServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginsController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginsController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginsController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginsController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginDto userLoginDto)
        {
            var kullaniciAdi = userLoginDto.Username;
            _logger.LogInformation("Kullanıcı {KullaniciAdi} için giriş denemesi yapıldı: {Zaman}", kullaniciAdi, DateTime.UtcNow);

            try
            {
                var result = await _signInManager.PasswordSignInAsync(userLoginDto.Username, userLoginDto.Password, false, false);
                var user = await _userManager.FindByNameAsync(userLoginDto.Username);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Kullanıcı {KullaniciAdi} başarıyla giriş yaptı: {Zaman}", kullaniciAdi, DateTime.UtcNow);

                    GetCheckAppUserViewModel model = new GetCheckAppUserViewModel
                    {
                        Username = userLoginDto.Username,
                        Id = user.Id
                    };
                    var token = JwtTokenGenerator.GenerateToken(model);

                    _logger.LogInformation("Kullanıcı {KullaniciAdi} için JWT token oluşturuldu: {Zaman}", kullaniciAdi, DateTime.UtcNow);
                    return Ok(token);
                }
                else
                {
                    _logger.LogWarning("Kullanıcı {KullaniciAdi} için giriş denemesi başarısız oldu: {Zaman}", kullaniciAdi, DateTime.UtcNow);
                    return Ok("Kullanıcı adı veya şifre hatalı.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı {KullaniciAdi} için giriş denemesi sırasında hata meydana geldi: {Zaman} - Hata Mesajı: {HataMesaji}", kullaniciAdi, DateTime.UtcNow, ex.Message);
                return StatusCode(500, "Bir iç hata meydana geldi.");
            }
        }
    }
}
