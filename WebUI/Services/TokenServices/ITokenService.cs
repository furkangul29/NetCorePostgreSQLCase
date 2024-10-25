namespace WebUI.Services.TokenServices
{
    public interface ITokenService
    {
        string GetToken();
        void StoreToken(string token);
        HttpClient CreateClientWithToken();
    }
}
