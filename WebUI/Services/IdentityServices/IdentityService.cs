using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // DbContext için gerekli
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using WebUI.DTO.IdentityDtos.LoginDtos;


using DataAccessLayer.Context;
using WebUI.Services.IdentityServices;

namespace WebUI.Services.Concrete
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly CRMContext _dbContext; // DbContext'i tanımlayın

        public IdentityService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, CRMContext dbContext)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _dbContext = dbContext; // DbContext'i başlatın
        }

        public async Task<bool> GetRefreshToken()
        {
            var discoveryEndPoint = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _configuration["IdentityServer:IdentityServerUrl"],
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            var userRole = GetUserRole(); // Kullanıcının rolünü al

            if (string.IsNullOrEmpty(userRole))
            {
                // Rol yoksa varsayılan bir rol kullanın veya hata işleyin
                userRole = "DefaultRole";
            }

            var clientSettings = GetClientSettingsByRole(userRole); // Rolüne göre istemci bilgilerini al

            var refreshTokenRequest = new RefreshTokenRequest
            {
                ClientId = clientSettings.ClientId,
                ClientSecret = clientSettings.ClientSecret,
                RefreshToken = refreshToken,
                Address = discoveryEndPoint.TokenEndpoint
            };

            var token = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest);

            var authenticationToken = new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = token.AccessToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = token.RefreshToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.ExpiresIn,
                    Value = DateTime.Now.AddSeconds(token.ExpiresIn).ToString()
                }
            };

            var result = await _httpContextAccessor.HttpContext.AuthenticateAsync();
            var properties = result.Properties;
            properties.StoreTokens(authenticationToken);

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal, properties);

            return true;
        }

        public async Task<bool> SignIn(SignInDto signInDto)
        {
            var discoveryEndPoint = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _configuration["IdentityServer:IdentityServerUrl"],
                Policy = new DiscoveryPolicy { RequireHttps = false }
            });

            // Önce kullanıcının rolünü al
            var userRole = await _dbContext.AspNetUsers
                .Where(u => u.UserName == signInDto.Username)
                .Join(_dbContext.AspNetUserRoles,
                    user => user.Id,
                    userRole => userRole.UserId,
                    (user, userRole) => userRole.RoleId)
                .Join(_dbContext.AspNetRoles,
                    userRoleId => userRoleId,
                    role => role.Id,
                    (userRoleId, role) => role.Name)
                .FirstOrDefaultAsync();

            // Role göre client settings al
            var clientSettings = GetClientSettingsByRole(userRole ?? "DefaultRole");

            // Doğru client bilgileriyle token isteği yap
            var passwordTokenRequest = new PasswordTokenRequest
            {
                ClientId = clientSettings.ClientId,
                ClientSecret = clientSettings.ClientSecret,
                UserName = signInDto.Username,
                Password = signInDto.Password,
                Address = discoveryEndPoint.TokenEndpoint
            };

            var token = await _httpClient.RequestPasswordTokenAsync(passwordTokenRequest);

            if (token.IsError)
            {
                // Hata işleme
                return false;
            }

            var userInfoRequest = new UserInfoRequest
            {
                Token = token.AccessToken,
                Address = discoveryEndPoint.UserInfoEndpoint
            };

            var userValues = await _httpClient.GetUserInfoAsync(userInfoRequest);
            var userId = userValues.Claims.FirstOrDefault(c => c.Type == "sub")?.Value; // Kullanıcı ID'sini al


            var authenticationProperties = new AuthenticationProperties();
            authenticationProperties.StoreTokens(new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = token.AccessToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = token.RefreshToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.ExpiresIn,
                    Value = DateTime.Now.AddSeconds(token.ExpiresIn).ToString()
                }
            });

            // Claims oluşturma ve giriş yapma
            var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, userValues.Claims.FirstOrDefault(c => c.Type == "name")?.Value),
    new Claim(ClaimTypes.Role, userRole ?? "DefaultRole") // userRole string olduğu için doğrudan kullanıyoruz
};


            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            authenticationProperties.IsPersistent = false;

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authenticationProperties);

            return true;
        }

        private string GetUserRole()
        {
            // _httpContextAccessor.HttpContext.User kullanarak rolü alın
            return _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        }

        private (string ClientId, string ClientSecret) GetClientSettingsByRole(string role)
        {
            // Rolüne göre uygun istemci bilgilerini döndür
            return role switch
            {
                "Admin" => (
                    _configuration["ClientSettings:AdminClient:ClientId"],
                    _configuration["ClientSettings:AdminClient:ClientSecret"]
                ),
                "Manager" => (
                    _configuration["ClientSettings:CrmManagerClient:ClientId"],
                    _configuration["ClientSettings:CrmManagerClient:ClientSecret"]
                ),
                "User" => (
                    _configuration["ClientSettings:CrmUserClient:ClientId"],
                    _configuration["ClientSettings:CrmUserClient:ClientSecret"]
                ),
                _ => (
                    _configuration["ClientSettings:CrmUserClient:ClientId"],
                    _configuration["ClientSettings:CrmUserClient:ClientSecret"]
                )
            };
        }
    }
}