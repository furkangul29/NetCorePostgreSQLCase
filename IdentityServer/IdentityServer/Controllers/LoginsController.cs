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

        public LoginsController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginsController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginDto userLoginDto)
        {
            _logger.LogInformation("Login attempt for user: {Username} at {Time}", userLoginDto.Username, DateTime.UtcNow);

            try
            {
                var result = await _signInManager.PasswordSignInAsync(userLoginDto.Username, userLoginDto.Password, false, false);
                var user = await _userManager.FindByNameAsync(userLoginDto.Username);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successful login for user: {Username} at {Time}", userLoginDto.Username, DateTime.UtcNow);

                    GetCheckAppUserViewModel model = new GetCheckAppUserViewModel
                    {
                        Username = userLoginDto.Username,
                        Id = user.Id
                    };
                    var token = JwtTokenGenerator.GenerateToken(model);

                    _logger.LogInformation("JWT token generated for user: {Username} at {Time}", userLoginDto.Username, DateTime.UtcNow);
                    return Ok(token);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user: {Username} at {Time}", userLoginDto.Username, DateTime.UtcNow);
                    return Ok("Kullanıcı Adı veya Şifre Hatalı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login attempt for user: {Username} at {Time}", userLoginDto.Username, DateTime.UtcNow);
                return StatusCode(500, "An internal error occurred.");
            }
        }
    }
}
