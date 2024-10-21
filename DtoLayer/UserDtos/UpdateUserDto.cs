using System;

namespace DtoLayer.UserDtos
{
    public class UpdateUserDto
    {
        //public int Id { get; set; } 
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
