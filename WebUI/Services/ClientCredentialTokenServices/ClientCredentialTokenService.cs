using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;

namespace WebUI.Services.ClientCredentialTokenServices
{
    public class ClientCredentialTokenService : IClientCredentialTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IClientAccessTokenCache _clientAccessTokenCache;
        private readonly IConfiguration _configuration; // IConfiguration nesnesini ekledik

        public ClientCredentialTokenService(
            HttpClient httpClient,
            IClientAccessTokenCache clientAccessTokenCache,
            IConfiguration configuration) // IConfiguration nesnesini enjekte ettik
        {
            _httpClient = httpClient;
            _clientAccessTokenCache = clientAccessTokenCache;
            _configuration = configuration; // IConfiguration'ı saklıyoruz
        }

        public async Task<string> GetToken()
        {
            var token1 = await _clientAccessTokenCache.GetAsync("crmtoken", new ClientAccessTokenParameters(), CancellationToken.None);
            if (token1 != null)
            {
                return token1.AccessToken;
            }

            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _configuration["CrmUserClient:ClientId"], // Buradan alıyoruz
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            if (discoveryDocument.IsError)
            {
                throw new Exception("Discovery document error: " + discoveryDocument.Error);
            }

            var clientCredentialTokenRequest = new ClientCredentialsTokenRequest
            {
                ClientId = _configuration["CrmUserClient:ClientId"], // Buradan alıyoruz
                ClientSecret = _configuration["CrmUserClient:ClientSecret"], // Buradan alıyoruz
                Address = discoveryDocument.TokenEndpoint
            };

            var token2 = await _httpClient.RequestClientCredentialsTokenAsync(clientCredentialTokenRequest);
            if (token2.IsError)
            {
                throw new Exception("Token request error: " + token2.Error);
            }

            await _clientAccessTokenCache.SetAsync("crmtoken", token2.AccessToken, token2.ExpiresIn, new ClientAccessTokenParameters(), CancellationToken.None);
            return token2.AccessToken;
        }
    }
}
