using System;

namespace IdentityServer.Dtos
{
    public class GetUserWithRole
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Role { get; set; }    
    }
}
