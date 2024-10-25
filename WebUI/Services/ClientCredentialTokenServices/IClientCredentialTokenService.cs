using WebUI.DTO.IdentityDtos.LoginDtos;

namespace WebUI.Services.ClientCredentialTokenServices
{
    public interface IClientCredentialTokenService
    {
        Task<string> GetToken(SignInDto signInDto);
    }
}
