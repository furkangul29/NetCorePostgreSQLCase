namespace WebUI.Services.RegistrationTokenServices
{
    public interface IRegistrationTokenService
    {
        Task<string> GenerateRegistrationToken(int roleId);
        bool ValidateRegistrationToken(string token, out int roleId, out string accessToken);
    }
}
