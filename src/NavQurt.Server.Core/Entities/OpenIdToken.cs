using OpenIddict.EntityFrameworkCore.Models;

namespace NavQurt.Server.Core.Entities
{
    public class OpenIdToken : OpenIddictEntityFrameworkCoreToken<long, OpenIdApplication, OpenIdAuthorization>
    {
    }
}
