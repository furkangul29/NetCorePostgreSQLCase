namespace WebUI.DTO.IdentityDtos.UserDtos
{
    public class UserHeaderDto
    {
        public string UserId { get; set; }
        public string NormalizedUserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfileImage { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public string  UpdatedAt { get; set; }
    }
}
