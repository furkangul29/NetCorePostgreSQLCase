using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using static Duende.IdentityServer.IdentityServerConstants;


namespace IdentityServer.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "users.read")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser()
        {
            _logger.LogInformation("GetUser method started at {Time}", DateTime.UtcNow);

            try
            {
                var userClaim = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userClaim == null)
                {
                    _logger.LogWarning("User claim not found at {Time}", DateTime.UtcNow);
                    return Unauthorized("User claim not found.");
                }

                var user = await _userManager.FindByIdAsync(userClaim.Value);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId} at {Time}", userClaim.Value, DateTime.UtcNow);
                    return NotFound("User not found.");
                }

                _logger.LogInformation("User found: {UserId} at {Time}", user.Id, DateTime.UtcNow);

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("Roles fetched for user: {UserId} - Roles: {Roles} at {Time}", user.Id, string.Join(",", roles), DateTime.UtcNow);

                var result = new
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles
                };

                _logger.LogInformation("GetUser method completed successfully at {Time}", DateTime.UtcNow);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetUser method at {Time}", DateTime.UtcNow);
                return StatusCode(500, "An internal error occurred.");
            }
        }

        [HttpGet("GetAllUserList")]
        public async Task<IActionResult> GetAllUserList()
        {
            _logger.LogInformation("GetAllUserList method started at {Time}", DateTime.UtcNow);

            try
            {
                var users = await _userManager.Users.ToListAsync();
                _logger.LogInformation("Fetched {UserCount} users from the database at {Time}", users.Count, DateTime.UtcNow);

                var userList = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation("Roles fetched for user: {UserId} - Roles: {Roles} at {Time}", user.Id, string.Join(",", roles), DateTime.UtcNow);

                    userList.Add(new
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Surname = user.Surname,
                        Email = user.Email,
                        UserName = user.UserName,
                        Roles = roles
                    });
                }

                _logger.LogInformation("GetAllUserList method completed successfully at {Time}", DateTime.UtcNow);
                return Ok(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllUserList method at {Time}", DateTime.UtcNow);
                return StatusCode(500, "An internal error occurred.");
            }
        }
    }
}
