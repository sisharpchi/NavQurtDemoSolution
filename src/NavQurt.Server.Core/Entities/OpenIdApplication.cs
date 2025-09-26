using OpenIddict.EntityFrameworkCore.Models;

namespace NavQurt.Server.Core.Entities
{
    public class OpenIdApplication : OpenIddictEntityFrameworkCoreApplication<long, OpenIdAuthorization, OpenIdToken>
    {
    }
}
