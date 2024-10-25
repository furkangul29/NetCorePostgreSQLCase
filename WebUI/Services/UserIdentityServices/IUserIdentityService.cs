using WebUI.DTO.IdentityDtos.UserDtos;

namespace WebUI.Services.UserIdentityServices
{
    public interface IUserIdentityService
    {
        Task<List<UserWithRolesDto>> GetAllUserListAsync();
        Task<ResultUserDto> GetUserByIdAsync(string id);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<bool> UpdateUserRoleAsync(UpdateUserRoleDto updateUserRoleDto);
        Task<bool> DeleteUserAsync(string id);
    }
}
