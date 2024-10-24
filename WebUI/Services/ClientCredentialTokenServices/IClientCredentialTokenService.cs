namespace WebUI.Services.ClientCredentialTokenServices
{
    public interface IClientCredentialTokenService
    {
        Task<string> GetToken();
    }
}
