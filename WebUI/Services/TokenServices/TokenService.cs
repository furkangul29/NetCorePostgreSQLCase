using System.Net.Http.Headers;

namespace WebUI.Services.TokenServices
{
 
    public class TokenService : ITokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public TokenService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        public string GetToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("Token");
        }

        public void StoreToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("Token", token);
        }

        public HttpClient CreateClientWithToken()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
    }
}
