using IdentityServer.Dtos;
using IdentityServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegistersController> _logger;

        public RegistersController(UserManager<ApplicationUser> userManager, ILogger<RegistersController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UserRegister(UserRegisterDto userRegisterDto)
        {
            _logger.LogInformation("Registration attempt for user: {Username} at {Time}", userRegisterDto.Username, DateTime.UtcNow);

            // Kullanıcı bilgilerini doldur
            var values = new ApplicationUser()
            {
                UserName = userRegisterDto.Username,
                Email = userRegisterDto.Email,
                Name = userRegisterDto.Name,
                Surname = userRegisterDto.Surname,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            try
            {
                var result = await _userManager.CreateAsync(values, userRegisterDto.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User registration successful for: {Username} at {Time}", userRegisterDto.Username, DateTime.UtcNow);
                    return Ok("Kullanıcı başarıyla eklendi");
                }
                else
                {
                    _logger.LogWarning("User registration failed for: {Username} at {Time}. Errors: {Errors}", userRegisterDto.Username, DateTime.UtcNow, string.Join(", ", result.Errors));
                    return BadRequest(result.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the registration process for user: {Username} at {Time}", userRegisterDto.Username, DateTime.UtcNow);
                return StatusCode(500, "An internal error occurred during registration.");
            }
        }
    }
}
