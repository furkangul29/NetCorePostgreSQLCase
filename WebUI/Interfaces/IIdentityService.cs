using WebUI.DTO.IdentityDtos.LoginDtos;



namespace WebUI.Interfaces
{
    public interface IIdentityService
    {
        Task<bool> SignIn(SignInDto signInDto);
        Task<bool> GetRefreshToken();
    }
}
