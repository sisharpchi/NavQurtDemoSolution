using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NavQurt.Server.Core.Entities;
using OpenIddict.Abstractions;

namespace NavQurt.Server.Infrastructure.Seed
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var appMgr = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            // Rollar
            foreach (var r in new[] { "SuperAdmin", "Admin", "Customer" })
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new AppRole { Name = r });

            // Admin user (dev uchun)
            var admin = await userMgr.FindByNameAsync("admin");
            if (admin is null)
            {
                admin = new AppUser
                {
                    UserName = "SuperAdmin",
                    Email = "superadmin@qurt.local",
                    EmailConfirmed = true,
                    PhoneNumber = "+998900000000",
                    PhoneNumberConfirmed = true,
                    FirstName = "System",
                    LastName = "SuperAdmin",
                    IsActive = true
                };
                await userMgr.CreateAsync(admin, "SuperAdmin@123"); // dev parol
                await userMgr.AddToRoleAsync(admin, "SuperAdmin");
            }

            // (Ixtiyoriy) Agar client ro‘yxatdan o‘tkazmoqchi bo‘lsangiz:
            if (await appMgr.FindByClientIdAsync("qurt-super-admin") is null)
            {
                await appMgr.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "qurt-super-admin",
                    DisplayName = "Qurt Super Admin Panel",
                    Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    "scp:api"
                }
                });
            }
        }
    }
}
