using WebUI.DTO.IdentityDtos.LoginDtos;



namespace WebUI.Services.IdentityServices
{
    public interface IIdentityService
    {
        Task<string> SignIn(SignInDto signInDto);
        Task<bool> GetRefreshToken();
    }
}
