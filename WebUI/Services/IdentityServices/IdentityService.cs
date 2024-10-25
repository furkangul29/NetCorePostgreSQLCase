using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using WebUI.DTO.IdentityDtos.LoginDtos;
using WebUI.Services.TokenServices;
using DataAccessLayer.Context;
using System.Net.Http;
using WebUI.Services.IdentityServices;

namespace WebUI.Services.Concrete
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly CRMContext _dbContext;
        private readonly ITokenService _tokenService; // TokenService ekleyin

        public IdentityService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, CRMContext dbContext, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _dbContext = dbContext;
            _tokenService = tokenService; // TokenService'i başlatın
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
            var userRole = GetUserRole();

            if (string.IsNullOrEmpty(userRole))
            {
                userRole = "DefaultRole";
            }

            var clientSettings = GetClientSettingsByRole(userRole);

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

        public async Task<string> SignIn(SignInDto signInDto)
        {
            var discoveryEndPoint = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _configuration["IdentityServer:IdentityServerUrl"],
                Policy = new DiscoveryPolicy { RequireHttps = false }
            });

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

            var clientSettings = GetClientSettingsByRole(userRole ?? "DefaultRole");

            var passwordTokenRequest = new PasswordTokenRequest
            {
                ClientId = clientSettings.ClientId,
                ClientSecret = clientSettings.ClientSecret,
                UserName = signInDto.Username,
                Password = signInDto.Password,
                Address = discoveryEndPoint.TokenEndpoint
            };

            var tokenResponse = await _httpClient.RequestPasswordTokenAsync(passwordTokenRequest);

            if (tokenResponse.IsError)
            {
                return null;
            }

            _tokenService.StoreToken(tokenResponse.AccessToken); // Token'ı TokenService ile saklayın

            var userInfoRequest = new UserInfoRequest
            {
                Token = tokenResponse.AccessToken,
                Address = discoveryEndPoint.UserInfoEndpoint
            };

            var userInfoResponse = await _httpClient.GetUserInfoAsync(userInfoRequest);
            var userId = userInfoResponse.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };

            authenticationProperties.StoreTokens(new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = tokenResponse.AccessToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = tokenResponse.RefreshToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.ExpiresIn,
                    Value = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn).ToString()
                }
            });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userInfoResponse.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? signInDto.Username),
                new Claim(ClaimTypes.Role, userRole ?? "DefaultRole")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authenticationProperties);

            return tokenResponse.AccessToken;
        }

        private string GetUserRole()
        {
            return _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        }

        private (string ClientId, string ClientSecret) GetClientSettingsByRole(string role)
        {
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
