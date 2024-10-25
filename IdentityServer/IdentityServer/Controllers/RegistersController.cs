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
            _logger.LogInformation("Registration attempt for user: {Username} at {Time}",
                userRegisterDto.Username, DateTime.UtcNow);

            // İlk olarak rolün varlığını kontrol et
            var role = await _roleManager.FindByIdAsync(userRegisterDto.RoleId.ToString());
            if (role == null)
            {
                _logger.LogWarning("Role with ID: {RoleId} does not exist", userRegisterDto.RoleId);
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
                    _logger.LogWarning("User registration failed for: {Username}. Errors: {Errors}",
                        userRegisterDto.Username, string.Join(", ", createResult.Errors));
                    return BadRequest(createResult.Errors);
                }

                // Rol atama
                var roleAddResult = await _userManager.AddToRoleAsync(user, role.Name); // Id yerine Name kullan
                if (!roleAddResult.Succeeded)
                {
                    _logger.LogWarning("Role assignment failed for user: {Username}. Errors: {Errors}",
                        userRegisterDto.Username, string.Join(", ", roleAddResult.Errors));

                    // Rol ataması başarısız olduğunda transaction'ı geri al
                    await transaction.RollbackAsync();
                    return BadRequest(roleAddResult.Errors);
                }

                // Her şey başarılı ise transaction'ı commit et
                await transaction.CommitAsync();

                _logger.LogInformation("User registration and role assignment successful for: {Username}",
                    userRegisterDto.Username);

                return Ok(new
                {
                    UserId = user.Id,
                    Message = "Kullanıcı başarıyla oluşturuldu ve rol atandı."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred during registration for user: {Username}",
                    userRegisterDto.Username);
                return StatusCode(500, "Kayıt işlemi sırasında bir hata oluştu.");
            }
        }
    }
}
