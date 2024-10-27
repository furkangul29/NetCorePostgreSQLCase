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
            string currentUserId = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            _logger.LogWarning("GetUser metodu başlatıldı, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));

            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogCritical("Kullanıcı talebi bulunamadı, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));
                    return Unauthorized("Kullanıcı talebi bulunamadı.");
                }

                var user = await _userManager.FindByIdAsync(currentUserId);
                if (user == null)
                {
                    _logger.LogCritical("Kullanıcı bulunamadı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}", currentUserId, currentUserId, DateTime.UtcNow.AddHours(3));
                    return NotFound("Kullanıcı bulunamadı.");
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("Kullanıcı bulundu, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Roller: {Roles}, Zaman: {Time}", currentUserId, user.Id, string.Join(", ", roles), DateTime.UtcNow.AddHours(3));

                var result = new
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles
                };

                _logger.LogInformation("GetUser metodu başarıyla tamamlandı, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUser metodunda hata oluştu, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }

        [HttpGet("GetAllUserList")]
        public async Task<IActionResult> GetAllUserList()
        {
            string currentUserId = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            _logger.LogInformation("GetAllUserList metodu başlatıldı, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));

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

                _logger.LogInformation("Kullanıcı listesi başarıyla alındı, İşlem Yapan Kullanıcı ID: {UserId}, Kullanıcı Sayısı: {UserCount}, Zaman: {Time}", currentUserId, resultUserList.Count, DateTime.UtcNow.AddHours(3));
                return Ok(resultUserList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllUserList metodunda hata oluştu, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            string currentUserId = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            _logger.LogInformation("GetUserById metodu başlatıldı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}",
                currentUserId, id, DateTime.UtcNow.AddHours(3));

            try
            {
                var userWithRole = await (
                         from user in _context.Users
                         where user.Id == id
                         select new
                         {
                             UserId = user.Id,
                             UserName = user.UserName,
                             Name = user.Name,
                             Surname = user.Surname,
                             Email = user.Email,
                             CreatedAt = user.CreatedAt,
                             UpdatedAt = user.UpdatedAt,
                             Role = (from userRole in _context.UserRoles
                                     join role in _context.Roles on userRole.RoleId equals role.Id
                                     where userRole.UserId == user.Id
                                     select role.Name).FirstOrDefault()
                         }
                     ).FirstOrDefaultAsync();

                if (userWithRole == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}",
                        currentUserId, id, DateTime.UtcNow.AddHours(3));
                    return NotFound("Kullanıcı bulunamadı.");
                }

                var result = new GetUserWithRole
                {
                    Id = userWithRole.UserId,
                    Name = userWithRole.Name,
                    Surname = userWithRole.Surname,
                    Email = userWithRole.Email,
                    Username = userWithRole.UserName,
                    Role = userWithRole.Role,
                    CreatedAt = userWithRole.CreatedAt,
                    UpdatedAt = userWithRole.UpdatedAt
                };

                _logger.LogInformation("GetUserById metodu başarıyla tamamlandı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}",
                    currentUserId, result.Id, DateTime.UtcNow.AddHours(3));

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserById metodunda hata oluştu, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Hata: {Error}, Zaman: {Time}",
                    currentUserId, id, ex.Message, DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }


        [HttpPost("UpdateUserRole")]
        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleDto model)
        {
            string currentUserId = User.Claims.FirstOrDefault(x =>
                x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            _logger.LogInformation("UpdateUserRole metodu başlatıldı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}",
                currentUserId, model.UserId, DateTime.UtcNow.AddHours(3));

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}",
                        currentUserId, model.UserId, DateTime.UtcNow.AddHours(3));
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                var role = await _roleManager.FindByIdAsync(model.NewRoleId);
                if (role == null)
                {
                    _logger.LogWarning("Rol bulunamadı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Rol ID: {TargetRoleId}, Zaman: {Time}",
                        currentUserId, model.NewRoleId, DateTime.UtcNow.AddHours(3));
                    return NotFound(new { success = false, message = "Rol bulunamadı." });
                }

                // Mevcut rolleri temizle
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Yeni rolü ekle
                var result = await _userManager.AddToRoleAsync(user, role.Name);

                if (result.Succeeded)
                {
                    // UpdatedAt alanını güncelle
                    user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("Kullanıcı rolü başarıyla güncellendi, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Rol: {RoleName}, Zaman: {Time}",
                        currentUserId, user.Id, role.Name, DateTime.UtcNow.AddHours(3));

                    return Ok(new { success = true, message = "Kullanıcı rolü başarıyla güncellendi." });
                }
                else
                {
                    _logger.LogWarning("Rol güncellenemedi, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Hatalar: {Errors}",
                        currentUserId, user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

                    return BadRequest(new { success = false, message = "Rol güncellenirken bir hata oluştu." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUserRole metodunda hata oluştu, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}",
                    currentUserId, DateTime.UtcNow.AddHours(3));

                return StatusCode(500, new { success = false, message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            string currentUserId = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            _logger.LogInformation("DeleteUser metodu başlatıldı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}", currentUserId, id, DateTime.UtcNow.AddHours(3));

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}", currentUserId, id, DateTime.UtcNow.AddHours(3));
                    return NotFound("Kullanıcı bulunamadı.");
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Kullanıcı silme işlemi başarısız oldu, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}", currentUserId, id, DateTime.UtcNow.AddHours(3));
                    return BadRequest("Kullanıcı silme işlemi başarısız oldu.");
                }

                _logger.LogInformation("Kullanıcı başarıyla silindi, İşlem Yapan Kullanıcı ID: {UserId}, Hedef Kullanıcı ID: {TargetUserId}, Zaman: {Time}", currentUserId, id, DateTime.UtcNow.AddHours(3));
                return Ok("Kullanıcı başarıyla silindi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUser metodunda hata oluştu, İşlem Yapan Kullanıcı ID: {UserId}, Zaman: {Time}", currentUserId, DateTime.UtcNow.AddHours(3));
                return StatusCode(500, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }
       

    }
}
