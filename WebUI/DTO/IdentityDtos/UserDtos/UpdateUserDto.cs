using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebUI.DTO.IdentityDtos.UserDtos
{
    public class UpdateUserDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string CurrentRoleName { get; set; }
        public string NewRoleId { get; set; }
        public List<SelectListItem> AvailableRoles { get; set; }
    }
}
