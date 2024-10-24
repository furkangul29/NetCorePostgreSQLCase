using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using WebUI.Models;
using WebUI.Services.UserServices;

namespace WebUI.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserDetailViewModel> GetUserInfo()
        {
            var accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Access token not found");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("http://localhost:3001/api/users/getuser");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {responseContent}"); // API yanıtını logla

            if (response.IsSuccessStatusCode)
            {
                var userDetail = await response.Content.ReadFromJsonAsync<UserDetailViewModel>();
                if (userDetail == null)
                {
                    throw new Exception("Received null response from API");
                }
                return userDetail;
            }

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Details: {error}"); // Hata detaylarını konsola yazdır
            throw new Exception($"Failed to get user info. Status: {response.StatusCode}, Error: {error}");
        }



    }
}