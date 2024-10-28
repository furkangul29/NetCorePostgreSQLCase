using IdentityServer.Data;
using IdentityServer.Dtos;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "users.write")]
    public class RegistersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegistersController> _logger;
        private readonly ApplicationDbContext _context;

        public RegistersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<RegistersController> logger, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> UserRegister(UserRegisterDto userRegisterDto)
        {
            var kullaniciAdi = userRegisterDto.Username;
            var email = userRegisterDto.Email;
            _logger.LogInformation("Kullanıcı {KullaniciAdi} için kayıt denemesi yapıldı: {Zaman}", kullaniciAdi, DateTime.UtcNow);

            // Kullanıcı adı kontrolü
            var existingUserByUsername = await _userManager.FindByNameAsync(kullaniciAdi);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning("Kullanıcı adı zaten mevcut: {KullaniciAdi}", kullaniciAdi);
                return BadRequest(new { Key = "Username", Message = "Bu kullanıcı adı zaten alınmış." });
            }

            // E-posta kontrolü
            var existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning("E-posta zaten mevcut: {Email}", email);
                return BadRequest(new { Key = "Email", Message = "Bu e-posta sistemimizde zaten kayıtlı." });
            }
            // İlk olarak rolün varlığını kontrol et
            var role = await _roleManager.FindByIdAsync(userRegisterDto.RoleId.ToString());
            if (role == null)
            {
                _logger.LogWarning("Kullanıcı {KullaniciAdi} için belirtilen rol bulunamadı (ID: {RoleId})", kullaniciAdi, userRegisterDto.RoleId);
                return BadRequest($"Belirtilen rol bulunamadı (ID: {userRegisterDto.RoleId})");
            }
           

            var user = new ApplicationUser()
            {
                UserName = userRegisterDto.Username,
                Email = userRegisterDto.Email,
                Name = userRegisterDto.Name,
                Surname = userRegisterDto.Surname,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kullanıcı oluşturma
                var createResult = await _userManager.CreateAsync(user, userRegisterDto.Password);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("Kullanıcı kaydı başarısız oldu: {KullaniciAdi}. Hatalar: {Hatalar}",
                        kullaniciAdi, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return BadRequest(createResult.Errors);
                }

                // Rol atama
                var roleAddResult = await _userManager.AddToRoleAsync(user, role.Name); // Id yerine Name kullan
                if (!roleAddResult.Succeeded)
                {
                    _logger.LogWarning("Kullanıcı {KullaniciAdi} için rol ataması başarısız oldu. Hatalar: {Hatalar}",
                        kullaniciAdi, string.Join(", ", roleAddResult.Errors.Select(e => e.Description)));

                    // Rol ataması başarısız olduğunda transaction'ı geri al
                    await transaction.RollbackAsync();
                    return BadRequest(roleAddResult.Errors);
                }

                // Her şey başarılı ise transaction'ı commit et
                await transaction.CommitAsync();

                _logger.LogInformation("Kullanıcı kaydı ve rol ataması başarıyla yapıldı: {KullaniciAdi}", kullaniciAdi);

                return Ok(new
                {
                    UserId = user.Id,
                    Message = "Kullanıcı başarıyla oluşturuldu ve rol atandı."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Kullanıcı {KullaniciAdi} için kayıt işlemi sırasında hata oluştu: {Zaman} - Hata Mesajı: {HataMesaji}",
                    kullaniciAdi, DateTime.UtcNow, ex.Message);
                return StatusCode(500, "Kayıt işlemi sırasında bir hata oluştu.");
            }
        }
        public class ErrorResponse
        {
            public string Key { get; set; }
            public string Message { get; set; }
        }
    }
}
