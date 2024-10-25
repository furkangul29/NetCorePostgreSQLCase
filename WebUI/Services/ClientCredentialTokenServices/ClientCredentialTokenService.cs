using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebUI.DTO.IdentityDtos.LoginDtos;

namespace WebUI.Services.ClientCredentialTokenServices
{
    public class ClientCredentialTokenService : IClientCredentialTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IClientAccessTokenCache _clientAccessTokenCache;
        private readonly IConfiguration _configuration;

        public ClientCredentialTokenService(
            HttpClient httpClient,
            IClientAccessTokenCache clientAccessTokenCache,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _clientAccessTokenCache = clientAccessTokenCache;
            _configuration = configuration;
        }

        public async Task<string> GetToken(SignInDto signInDto) // SignInDto kullanıcı adı ve şifreyi içermelidir
        {
            var cachedToken = await _clientAccessTokenCache.GetAsync("admintoken", new ClientAccessTokenParameters(), CancellationToken.None);
            if (cachedToken != null)
            {
                return cachedToken.AccessToken;
            }

            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _configuration["IdentityServer:IdentityServerUrl"],
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            if (discoveryDocument.IsError)
            {
                throw new Exception("Discovery document error: " + discoveryDocument.Error);
            }

            // Password akışını kullanarak token almak için düzenleme
            var passwordTokenRequest = new PasswordTokenRequest
            {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = _configuration["ClientSettings:AdminClient:ClientId"],
                ClientSecret = _configuration["ClientSettings:AdminClient:ClientSecret"],
                UserName = signInDto.Username,
                Password = signInDto.Password,
                Scope = "users.write" // Burada gerekli scope'u belirtin
            };

            var tokenResponse = await _httpClient.RequestPasswordTokenAsync(passwordTokenRequest);
            if (tokenResponse.IsError)
            {
                throw new Exception("Token request error: " + tokenResponse.Error);
            }

            await _clientAccessTokenCache.SetAsync("admintoken", tokenResponse.AccessToken, tokenResponse.ExpiresIn, new ClientAccessTokenParameters(), CancellationToken.None);
            return tokenResponse.AccessToken;
        }

    }
}
