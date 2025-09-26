using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NavQurt.Server.Core.Entities;

namespace NavQurt.Server.Infrastructure.Data
{
    public sealed class MainDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            b.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            b.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            b.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            b.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            b.Entity<AppUser>().ToTable("Users");

            b.Entity<OpenIdApplication>().ToTable("OpenIddictEntityFrameworkCoreApplications");
            b.Entity<OpenIdAuthorization>().ToTable("OpenIddictEntityFrameworkCoreAuthorizations");
            b.Entity<OpenIdScope>().ToTable("OpenIddictEntityFrameworkCoreScopes");
            b.Entity<OpenIdToken>().ToTable("OpenIddictEntityFrameworkCoreTokens");

            b.UseOpenIddict();
            b.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);
        }
    }
}
