
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebUI.DTO.IdentityDtos.UserDtos;

namespace WebUI.Services.UserIdentityServices
{
    public class UserIdentityService : IUserIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserIdentityService> _logger;
        public UserIdentityService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<UserIdentityService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public async Task<List<ResultUserDto>> GetAllUserListAsync()
        {
            // Kullanıcının token'ını al
            var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API'ye istek gönder
            var responseMessage = await _httpClient.GetAsync("http://localhost:3001/api/users/GetAllUserList");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultUserDto>>(jsonData);
                return values;
            }
            else
            {
                // Hata durumunda loglama yap
                var errorContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogError("Failed to retrieve user list. Status Code: {StatusCode}, Content: {ErrorContent}", responseMessage.StatusCode, errorContent);
                throw new UnauthorizedAccessException("Unable to access user list. Check your authentication.");
            }

        }
    }
}
