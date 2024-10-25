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
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using WebUI.DTO.IdentityDtos.UserDtos;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, ILogger<UsersController> logger, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser()
        {
            _logger.LogWarning("GetUser method started at {Time}", DateTime.UtcNow.AddHours(3));

            try
            {
                var userClaim = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userClaim == null)
                {
                    _logger.LogCritical("User claim not found at {Time}", DateTime.UtcNow.AddHours(3));
                    return Unauthorized("User claim not found.");
                }

                var user = await _userManager.FindByIdAsync(userClaim.Value);
                if (user == null)
                {
                    _logger.LogCritical("User not found for ID: {UserId} at {Time}", userClaim.Value, DateTime.UtcNow.AddHours(3));
                    return NotFound("User not found.");
                }

                _logger.LogWarning("User found: {UserId} at {Time}", user.Id, DateTime.UtcNow.AddHours(3));

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogWarning("Roles fetched for user: {UserId} - Roles: {Roles} at {Time}", user.Id, string.Join(",", roles), DateTime.UtcNow.AddHours(3));

                var result = new
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles
                };

                _logger.LogWarning("GetUser method completed successfully at {Time}", DateTime.UtcNow.AddHours(3));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetUser method at {Time}", DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "An internal error occurred.");
            }
        }

        [HttpGet("GetAllUserList")]
        public async Task<IActionResult> GetAllUserList()
        {
            try
            {
                var usersWithRoles = await (from user in _context.Users
                                            select new
                                            {
                                                UserId = user.Id,
                                                UserName = user.UserName,
                                                Name = user.Name,
                                                Surname = user.Surname,
                                                Email = user.Email,
                                                CreatedAt = user.CreatedAt,
                                                UpdatedAt = user.UpdatedAt,
                                                Roles = (from userRole in _context.UserRoles
                                                         join role in _context.Roles on userRole.RoleId equals role.Id
                                                         where userRole.UserId == user.Id
                                                         select role.Name).ToList()
                                            }).ToListAsync();

                var resultUserList = usersWithRoles.Select(u => new UserWithRolesDto
                {
                    Id = u.UserId,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    Username = u.UserName,
                    Roles = u.Roles,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                }).ToList();

                return Ok(resultUserList); // Liste olarak döndürülüyor
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllUsersWithRoles metodunda bir hata oluştu");
                return StatusCode(500, "Bir hata oluştu.");
            }
        }



            [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            _logger.LogInformation("GetUserById method started at {Time}", DateTime.UtcNow.AddHours(3));

            try
            {
                // Kullanıcı bilgilerini getir
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId} at {Time}", id, DateTime.UtcNow.AddHours(3));
                    return NotFound("User not found.");
                }

                // Kullanıcının rollerini getir
                var roles = await _userManager.GetRolesAsync(user);

                var result = new
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles 
                };

                _logger.LogInformation("GetUserById method completed successfully at {Time}", DateTime.UtcNow.AddHours(3));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetUserById method at {Time}", DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "An internal error occurred.");
            }
        }


        [HttpPost("UpdateUserRole")]
        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleDto model)
        {
            _logger.LogInformation("UpdateUserRole method started at {Time}", DateTime.UtcNow.AddHours(3));
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId} at {Time}", model.UserId, DateTime.UtcNow.AddHours(3));
                    return NotFound("User not found.");
                }

                // Önce rol adını bulalım
                var role = await _roleManager.FindByIdAsync(model.NewRoleId);
                if (role == null)
                {
                    _logger.LogWarning("Role not found for ID: {RoleId} at {Time}", model.NewRoleId, DateTime.UtcNow.AddHours(3));
                    return NotFound("Role not found.");
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                // Güncellenme tarihini ayarlayalım
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);

                // Rol ID'si yerine rol adını kullanıyoruz
                await _userManager.AddToRoleAsync(user, role.Name);

                _logger.LogInformation("User role updated successfully for user: {UserId} at {Time}", model.UserId, DateTime.UtcNow.AddHours(3));
                return Ok("User role updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateUserRole method at {Time}", DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "An internal error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            _logger.LogInformation("DeleteUser method started at {Time}", DateTime.UtcNow.AddHours(3));

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId} at {Time}", id, DateTime.UtcNow.AddHours(3));
                    return NotFound("User not found.");
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User deleted successfully for ID: {UserId} at {Time}", id, DateTime.UtcNow.AddHours(3));
                    return Ok("User deleted successfully.");
                }

                _logger.LogWarning("User deletion failed for ID: {UserId} at {Time}", id, DateTime.UtcNow.AddHours(3));
                return BadRequest("Failed to delete user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteUser method at {Time}", DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "An internal error occurred.");
            }
        }
        [HttpGet("GetAllRoles")]
        public async Task<IActionResult> GetAllRoles()
        {
            _logger.LogInformation("GetAllRoles method started at {Time}", DateTime.UtcNow.AddHours(3));

            try
            {
                var roles = await _roleManager.Roles.Select(role => new
                {
                    Id = role.Id,
                    Name = role.Name
                }).ToListAsync();

                _logger.LogInformation("Fetched {RoleCount} roles from the database at {Time}", roles.Count, DateTime.UtcNow.AddHours(3));
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllRoles method at {Time}", DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "An internal error occurred.");
            }
        }

    }
}
