
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Mozilla;
using System.Net.Http.Headers;
using System.Text;
using WebUI.DTO.IdentityDtos.LoginDtos;
using WebUI.DTO.IdentityDtos.UserDtos;
using WebUI.Models;
using WebUI.Services.ClientCredentialTokenServices;
using WebUI.Services.RegistrationTokenServices;
using WebUI.Services.TokenServices;
using WebUI.Services.UserIdentityServices;

namespace WebUI.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRegistrationTokenService _registrationTokenService;
        private readonly IClientCredentialTokenService _clientCredentialTokenService;
        private  readonly ITokenService _tokenService;

        public UserController(
            IUserIdentityService userIdentityService,
            ILogger<UserController> logger,
            IClientCredentialTokenService clientCredentialTokenService,
            IHttpClientFactory httpClientFactory,
            IRegistrationTokenService registrationTokenService,
            IConfiguration configuration,
            ITokenService tokenService)
        {
            _userIdentityService = userIdentityService;
            _logger = logger;
            _clientCredentialTokenService = clientCredentialTokenService;
            _httpClientFactory = httpClientFactory;
            _registrationTokenService = registrationTokenService;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        public async Task<IActionResult> UserList()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "Anonymous";
                _logger.LogInformation(
                    "User {UserId} accessing user list at {Time}",
                    userId,
                    DateTime.Now);

                var userList = await _userIdentityService.GetAllUserListAsync();

                _logger.LogInformation(
                    "Retrieved {Count} users from database",
                    userList.Count());

                return View(userList);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while retrieving user list");
                throw;
            }
        }
        public async Task<IActionResult> CreateUser()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            // Session'dan kaydedilmiş token'ı al
            string token = _tokenService.GetToken(); // TokenService kullanarak token alın

            HttpContext.Session.SetString("RoleId", model.RoleId);

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token bulunamadı. Lütfen tekrar giriş yapın.");
            }

            // Kullanıcıyı davet etmek için kayıt bağlantısını oluşturun
            var registrationLink = Url.Action("Index", "Register", null, Request.Scheme);

            // Davet e-postasını gönderin
            await SendUserInvitationEmail(model.Email, registrationLink);

            // Kullanıcı listesine yönlendirin
            return RedirectToAction("UserList");
        }


        // Davet e-postası göndermek için method
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
            try
            {
                var user = await _userIdentityService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
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
                _logger.LogError(ex, "Error occurred while retrieving user details");
                TempData["ErrorMessage"] = "Kullanıcı bilgileri alınırken bir hata oluştu.";
                return RedirectToAction(nameof(UserList));
            }
        }



        [HttpPost]
        public async Task<IActionResult> UpdateUser(UpdateUserDto model)
        {
            try
            {       
                var updateResult = await _userIdentityService.UpdateUserRoleAsync(new UpdateUserRoleDto
                {
                    UserId = model.Id,
                    NewRoleId = model.NewRoleId
                });

                if (updateResult)
                {
                    TempData["SuccessMessage"] = "Kullanıcı rolü başarıyla güncellendi.";
                    return RedirectToAction(nameof(UserList));
                }

                TempData["ErrorMessage"] = "Rol güncellenirken bir hata oluştu.";
                return RedirectToAction(nameof(UpdateUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user role");
                TempData["ErrorMessage"] = "Rol güncellenirken bir hata oluştu.";
                return RedirectToAction(nameof(UpdateUser));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _userIdentityService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user");
                TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(UserList));
        }

    }
}
