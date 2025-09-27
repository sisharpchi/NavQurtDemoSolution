using Microsoft.AspNetCore.Identity;

namespace NavQurt.Server.Core.Entities
{
    public class AppUser : IdentityUser, IEntity<string>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; } = true;
        public string FullName => LastName + " " + FirstName;
        public string? Code { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
