using OpenIddict.EntityFrameworkCore.Models;

namespace NavQurt.Server.Core.Entities
{
    public class OpenIdAuthorization : OpenIddictEntityFrameworkCoreAuthorization<long, OpenIdApplication, OpenIdToken>
    {
    }
}
