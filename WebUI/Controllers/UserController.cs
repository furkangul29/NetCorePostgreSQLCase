using MailKit.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Security.Claims;
using WebUI.DTO.IdentityDtos.RegisterDtos;
using WebUI.DTO.IdentityDtos.UserDtos;
using WebUI.Models;
using WebUI.Services.TokenServices;
using WebUI.Services.UserIdentityServices;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme, Policy = "customers.read")]
    public class UserController : Controller
    {
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenService _tokenService;

        public UserController(
            IUserIdentityService userIdentityService,
            ILogger<UserController> logger,
            IHttpClientFactory httpClientFactory,

            IConfiguration configuration,
            ITokenService tokenService)
        {
            _userIdentityService = userIdentityService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _tokenService = tokenService;
        }
        [RequireScope("users.read")]
        public async Task<IActionResult> UserList()
        {
            UserViewbagList();

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            
            try
            {
                _logger.LogInformation(
                    "{TarihSaat} tarihinde {userName} tarafından kullanıcı listesi görüntülenmeye çalışıldı.",
                    DateTime.UtcNow, userName);

                var userList = await _userIdentityService.GetAllUserListAsync();

                _logger.LogInformation(
                    "{TarihSaat} tarihinde {userName} tarafından {Count} kullanıcı başarıyla görüntülendi.",
                    DateTime.UtcNow, userName, userList.Count());

                return View(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{TarihSaat} tarihinde {userName} tarafından kullanıcı listesi alınırken bir hata oluştu.",
                    DateTime.UtcNow, userName);
                throw;
            }
        }

        public async Task<IActionResult> CreateUser()
        {
            UserViewbagList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            UserViewbagList();

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            string token = _tokenService.GetToken();

            HttpContext.Session.SetString("RoleId", model.RoleId);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning(
                    "{TarihSaat} tarihinde {userName} tarafından token bulunamadı.",
                    DateTime.UtcNow, userName);
                return BadRequest("Token bulunamadı. Lütfen tekrar giriş yapın.");
            }

            var registrationLink = Url.Action("Index", "Register", null, Request.Scheme);

            await SendUserInvitationEmail(model.Email, registrationLink);

            _logger.LogInformation(
                "{TarihSaat} tarihinde {userName} tarafından {Email} adresine davet e-postası gönderildi.",
                DateTime.UtcNow, userName, model.Email);

            return Ok(new { success = true }); // Başarılı yanıt döndür
        }


        private async Task SendUserInvitationEmail(string email, string link)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var server = smtpSettings["Server"];
            var port = int.Parse(smtpSettings["Port"]);
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];

            var body = $@"
            <html>
                <body>
                    <p>Merhaba,</p>
                    <p>Bir hesap oluşturmanız için davet edildiniz. Aşağıdaki bağlantıya tıklayarak kaydınızı tamamlayabilirsiniz:</p>
                    <a href='{link}'>Kaydınızı Tamamlayın</a>
                    <p>Teşekkürler,<br/>Uygulama Ekibiniz</p>
                </body>
            </html>";

            using (var message = new MimeMessage())
            {
                message.From.Add(new MailboxAddress("Uygulamanız", username));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Hesap Daveti";
                message.Body = new TextPart("html") { Text = body };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(server, port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(username, password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateUser(string id)
        {
            UserViewbagList();

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            try
            {
                _logger.LogInformation(
                    "{TarihSaat} tarihinde {userName} tarafından ID'si {UserId} olan kullanıcı düzenleme sayfasına erişilmeye çalışıldı.",
                    DateTime.UtcNow, userName, id);

                var user = await _userIdentityService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    _logger.LogWarning(
                        "{TarihSaat} tarihinde {userName} tarafından ID'si {UserId} olan kullanıcı bulunamadı.",
                        DateTime.UtcNow, userName, id);
                    return RedirectToAction(nameof(UserList));
                }

                var viewModel = new UpdateUserDto
                {
                    Id = user.id,
                    Username = user.userName,
                    Name = user.name,
                    Surname = user.surname,
                    Email = user.email,
                    CurrentRoleName = user.Role,
                    AvailableRoles = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "1", Text = "Admin" },
                        new SelectListItem { Value = "2", Text = "Manager" },
                        new SelectListItem { Value = "3", Text = "User" }
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{TarihSaat} tarihinde {userName} tarafından kullanıcı bilgileri alınırken bir hata oluştu.",
                    DateTime.UtcNow, userName);
                TempData["ErrorMessage"] = "Kullanıcı bilgileri alınırken bir hata oluştu.";
                return RedirectToAction(nameof(UserList));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(UpdateUserDto model)
        {
            UserViewbagList();

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            try
            {
                var updateResult = await _userIdentityService.UpdateUserRoleAsync(new UpdateUserRoleDto
                {
                    UserId = model.Id,
                    NewRoleId = model.NewRoleId
                });

                if (updateResult)
                {
                    _logger.LogInformation(
                        "{TarihSaat} tarihinde {userName} tarafından ID'si {UserId} olan kullanıcının rolü başarıyla güncellendi.",
                        DateTime.UtcNow, userName, model.Id);
                    TempData["SuccessMessage"] = "Kullanıcı rolü başarıyla güncellendi.";
                    return RedirectToAction(nameof(UserList));
                }

                TempData["ErrorMessage"] = "Rol güncellenirken bir hata oluştu.";
                _logger.LogWarning(
                    "{TarihSaat} tarihinde {userName} tarafından ID'si {UserId} olan kullanıcının rolü güncellenirken hata oluştu.",
                    DateTime.UtcNow, userName, model.Id);
                return RedirectToAction(nameof(UpdateUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{TarihSaat} tarihinde {userName} tarafından rol güncellenirken bir hata oluştu.",
                    DateTime.UtcNow, userName);
                TempData["ErrorMessage"] = "Rol güncellenirken bir hata oluştu.";
                return RedirectToAction(nameof(UpdateUser));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            UserViewbagList();

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            try
            {
                var result = await _userIdentityService.DeleteUserAsync(id);
                if (result)
                {
                    _logger.LogInformation(
                        "{TarihSaat} tarihinde {userName} tarafından ID'si {UserId} olan kullanıcı başarıyla silindi.",
                        DateTime.UtcNow, userName, id);
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
                }
                else
                {
                    _logger.LogWarning(
                        "{TarihSaat} tarihinde {userName} tarafından ID'si {UserId} olan kullanıcı silinirken bir hata oluştu.",
                        DateTime.UtcNow, userName, id);
                    TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{TarihSaat} tarihinde {userName} tarafından kullanıcı silinirken bir hata oluştu.",
                    DateTime.UtcNow, userName);
                TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(UserList));
        }


        void UserViewbagList()
        {
            ViewBag.v1 = "Ana Sayfa";
            ViewBag.v2 = "Kullanıcılar";
            ViewBag.v3 = "Kullanıcı Listesi";
            ViewBag.v0 = "Kullanıcı İşlemleri";
        }

    }
}
