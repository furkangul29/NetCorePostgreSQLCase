using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using WebUI.DTO.IdentityDtos.LoginDtos;
using WebUI.Interfaces;
using DataAccessLayer.Context;
using WebUI.Models;

namespace WebUI.Services.Concrete
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly CRMContext _dbContext;

        public IdentityService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration, CRMContext dbContext)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<bool> GetRefreshToken()
        {
            var discoveryEndPoint = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _configuration["IdentityServer:IdentityServerUrl"],
                Policy = new DiscoveryPolicy { RequireHttps = false }
            });

            if (discoveryEndPoint.IsError)
            {
                throw new Exception($"Discovery endpoint error: {discoveryEndPoint.Error}");
            }

            var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var userRole = GetUserRole();
            var clientSettings = GetClientSettingsByRole(userRole ?? "DefaultRole");

            var refreshTokenRequest = new RefreshTokenRequest
            {
                ClientId = clientSettings.ClientId,
                ClientSecret = clientSettings.ClientSecret,
                RefreshToken = refreshToken,
                Address = discoveryEndPoint.TokenEndpoint
            };

            var token = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest);

            if (token.IsError)
            {
                throw new Exception($"Refresh token error: {token.Error}");
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token.AccessToken);
            var scopes = jwtToken.Claims.Where(c => c.Type == "scope").Select(c => c.Value).ToList();

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

            var newClaims = result.Principal.Claims.Where(c => c.Type != "scope").ToList();
            foreach (var scope in scopes)
            {
                newClaims.Add(new Claim("scope", scope));
            }

            var claimsIdentity = new ClaimsIdentity(newClaims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, properties);

            return true;
        }

        private async Task<string> GetUserScopesFromDatabase(string username)
        {
            var userScopes = await _dbContext.AspNetUsers
                .Where(u => u.UserName == username)
                .Join(_dbContext.AspNetUserClaims,
                    user => user.Id,
                    claim => claim.UserId,
                    (user, claim) => new { user, claim })
                .Where(x => x.claim.ClaimType == "scope")
                .Select(x => x.claim.ClaimValue)
                .ToListAsync();

            // Standart OpenID Connect scope'larını ekle
            var standardScopes = new[]
            {
                "openid",
                "profile",
                "email",
                "api1"  // IdentityServerConstants.LocalApi.ScopeName
            };

            // Tüm scope'ları birleştir
            var allScopes = standardScopes.Concat(userScopes);
            return string.Join(" ", allScopes);
        }

        public async Task<bool> SignIn(SignInDto signInDto)
        {
            try
            {
                var discoveryEndPoint = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
                {
                    Address = _configuration["IdentityServer:IdentityServerUrl"],
                    Policy = new DiscoveryPolicy { RequireHttps = false }
                });

                if (discoveryEndPoint.IsError)
                {
                    throw new Exception($"Discovery endpoint error: {discoveryEndPoint.Error}");
                }

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

                // Veritabanından kullanıcının scope'larını al
                var scopes = await GetUserScopesFromDatabase(signInDto.Username);

                var passwordTokenRequest = new PasswordTokenRequest
                {
                    ClientId = clientSettings.ClientId,
                    ClientSecret = clientSettings.ClientSecret,
                    UserName = signInDto.Username,
                    Password = signInDto.Password,
                    Address = discoveryEndPoint.TokenEndpoint,
                    Scope = scopes
                };

                var token = await _httpClient.RequestPasswordTokenAsync(passwordTokenRequest);

                if (token.IsError)
                {
                    throw new Exception($"Token request error: {token.Error}");
                }

                var userInfoRequest = new UserInfoRequest
                {
                    Token = token.AccessToken,
                    Address = discoveryEndPoint.UserInfoEndpoint
                };

                var userValues = await _httpClient.GetUserInfoAsync(userInfoRequest);

                if (userValues.IsError)
                {
                    throw new Exception($"User info request error: {userValues.Error}");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token.AccessToken);
                var tokenScopes = jwtToken.Claims.Where(c => c.Type == "scope").Select(c => c.Value).ToList();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userValues.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? ""),
                    new Claim(ClaimTypes.Name, userValues.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? ""),
                    new Claim(ClaimTypes.Email, userValues.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? ""),
                    new Claim(ClaimTypes.Role, userRole ?? "DefaultRole")
                };

                foreach (var scope in tokenScopes)
                {
                    claims.Add(new Claim("scope", scope));
                }

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

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                authenticationProperties.IsPersistent = false;

                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authenticationProperties);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private string GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
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