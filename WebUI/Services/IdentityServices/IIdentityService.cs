using WebUI.DTO.IdentityDtos.LoginDtos;



namespace WebUI.Services.IdentityServices
{
    public interface IIdentityService
    {
        Task<bool> SignIn(SignInDto signInDto);
        Task<bool> GetRefreshToken();
    }
}
