using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

public class UserService : IUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<UserService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDetail> GetUserInfo()
    {
        try
        {
            var accessToken = await _httpContextAccessor.HttpContext?.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)
                ?? throw new UnauthorizedException("Access token not found");

            using var httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var identityServerUrl = _configuration["IdentityServer:IdentityServerUrl"]
                ?? throw new ConfigurationException("IdentityServer URL not configured");

            var response = await httpClient.GetAsync($"{identityServerUrl}/api/users/getuser");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get user info. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, error);
                throw new ApiException($"Failed to get user info. Status: {response.StatusCode}", error);
            }

            var userDetail = await response.Content.ReadFromJsonAsync<UserDetail>();
            return userDetail ?? throw new ApiException("Received null response from API");
        }
        catch (Exception ex) when (ex is not UnauthorizedException
                                    && ex is not ConfigurationException
                                    && ex is not ApiException)
        {
            _logger.LogError(ex, "Unexpected error while getting user info");
            throw new ApiException("An unexpected error occurred while getting user information", ex);
        }
    }
}

// Custom exceptions
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
}

public class ApiException : Exception
{
    public string? ApiError { get; }

    public ApiException(string message, string? apiError = null) : base(message)
    {
        ApiError = apiError;
    }

    public ApiException(string message, Exception innerException) : base(message, innerException) { }
}

// Interface
public interface IUserService
{
    Task<UserDetail> GetUserInfo();
}

// User detail model (adjust properties as needed)
public class UserDetail
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Add other properties as needed
}