using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.DTO.IdentityDtos.UserDtos;
using WebUI.Services.UserIdentityServices;

namespace WebUI.ViewComponents
{
    public class UserHeaderViewComponent : ViewComponent
    {
        private readonly IUserIdentityService _userIdentityService;

        public UserHeaderViewComponent(IUserIdentityService userIdentityService)
        {
            _userIdentityService = userIdentityService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var userId = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return View("Default", new UserHeaderDto
                    {
                        NormalizedUserName = "Guest",
                        FullName = "Guest User",
                        Email = "",
                        ProfileImage = "/images/logos/manlogo.jpg",
                        UserId = "",
                        Role = "",
                        CreatedAt = DateTime.MinValue,
                        UpdatedAt = "Güncellenmemiş"
                    });
                }

                var user = await _userIdentityService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var model = new UserHeaderDto
                {
                    NormalizedUserName = user.userName?.ToUpper(),
                    FullName = $"{user.name} {user.surname}",
                    Email = user.email,
                    ProfileImage = "/images/logos/manlogo.jpg",
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt.HasValue
                        ? user.UpdatedAt.Value.ToString("dd.MM.yyyy HH:mm:ss")
                        : "Güncellenmemiş"
                };

                return View("Default", model);
            }
            catch (Exception ex)
            {
                // Hata kaydı yapabilirsiniz
                return View("Error");
            }
        }
    }
}
