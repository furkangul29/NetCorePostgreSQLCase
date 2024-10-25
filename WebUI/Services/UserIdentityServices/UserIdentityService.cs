
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using WebUI.DTO.IdentityDtos.UserDtos;
using WebUI.Services.TokenServices;

namespace WebUI.Services.UserIdentityServices
{
    public class UserIdentityService : IUserIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserIdentityService> _logger;
        private readonly ITokenService _tokenService;

        public UserIdentityService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<UserIdentityService> logger, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _tokenService = tokenService;
        }

        public async Task<List<UserWithRolesDto>> GetAllUserListAsync()
        {
            // Kullanıcının token'ını al
            var token = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API'ye istek gönder
            var responseMessage = await _httpClient.GetAsync("http://localhost:3001/api/users/GetAllUserList");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<UserWithRolesDto>>(jsonData);
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
        public async Task<ResultUserDto> GetUserByIdAsync(string id)
        {
            // TokenService üzerinden token ile client oluştur
            using var client = _tokenService.CreateClientWithToken();

            // Kullanıcı bilgisi getirme isteği
            var response = await client.GetAsync($"http://localhost:3001/api/Users/{id}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ResultUserDto>(content);
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            // TokenService üzerinden token ile client oluştur
            using var client = _tokenService.CreateClientWithToken();

            // Rol listesini getirme isteği
            var response = await client.GetAsync("http://localhost:3001/api/GetAllRoles");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<RoleDto>>(content);
        }

        public async Task<bool> UpdateUserRoleAsync(UpdateUserRoleDto updateUserRoleDto)
        {
            // TokenService üzerinden token ile client oluştur
            using var client = _tokenService.CreateClientWithToken();

            // Kullanıcı rolü güncelleme isteği
            var content = new StringContent(
                JsonConvert.SerializeObject(updateUserRoleDto),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("http://localhost:3001/api/Users/UpdateUserRole", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            // TokenService üzerinden token ile client oluştur
            using var client = _tokenService.CreateClientWithToken();

            // Kullanıcı silme isteği
            var response = await client.DeleteAsync($"http://localhost:3001/api/Users/{id}");
            return response.IsSuccessStatusCode;
        }


    }
}

