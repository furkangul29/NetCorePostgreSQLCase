using WebUI.DTO.IdentityDtos.UserDtos;

namespace WebUI.UserIdentityServices
{
    public interface IUserIdentityService
    {
        Task<List<ResultUserDto>> GetAllUserListAsync();
    }
}
