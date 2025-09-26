using Microsoft.AspNetCore.Identity;

namespace NavQurt.Server.Core.Entities
{
    public class AppRole : IdentityRole, IEntity<string>{ }
}
