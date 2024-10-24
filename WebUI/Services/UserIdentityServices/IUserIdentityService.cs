using WebUI.DTO.IdentityDtos.UserDtos;

namespace WebUI.Services.UserIdentityServices
{
    public interface IUserIdentityService
    {
        Task<List<ResultUserDto>> GetAllUserListAsync();
    }
}
