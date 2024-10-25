using System.Collections.Generic;
using System;

namespace IdentityServer.Dtos
{
    public class UserWithRolesDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } // Rollerin listesi
    }
}
