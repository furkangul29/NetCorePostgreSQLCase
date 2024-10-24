using WebUI.Models;

namespace WebUI.Services.UserServices
{
    public interface IUserService
    {
        Task<UserDetailViewModel> GetUserInfo();
    }
}
