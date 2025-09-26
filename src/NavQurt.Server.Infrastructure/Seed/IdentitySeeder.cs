using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NavQurt.Server.Core.Entities;
using OpenIddict.Abstractions;
using System.Linq;

namespace NavQurt.Server.Infrastructure.Seed
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var services = scope.ServiceProvider;
            var roleMgr = services.GetRequiredService<RoleManager<AppRole>>();
            var userMgr = services.GetRequiredService<UserManager<AppUser>>();
            var appMgr = services.GetRequiredService<IOpenIddictApplicationManager>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<IdentitySeeder>>();

            // Roles
            foreach (var role in new[] { "SuperAdmin", "Admin", "Customer" })
            {
                if (!await roleMgr.RoleExistsAsync(role))
                {
                    await roleMgr.CreateAsync(new AppRole { Name = role });
                }
            }

            // Admin user (dev only)
            var adminUserName = configuration["Seed:AdminUserName"] ?? "SuperAdmin";
            var adminEmail = configuration["Seed:AdminEmail"] ?? "superadmin@qurt.local";
            var adminPassword = configuration["Seed:AdminPassword"];

            var admin = await userMgr.FindByNameAsync(adminUserName) ?? await userMgr.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    logger.LogWarning("Seed:AdminPassword is not configured; skipping administrator account creation.");
                }
                else
                {
                    admin = new AppUser
                    {
                        UserName = adminUserName,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        PhoneNumber = configuration["Seed:AdminPhone"] ?? "+998900000000",
                        PhoneNumberConfirmed = true,
                        FirstName = configuration["Seed:AdminFirstName"] ?? "System",
                        LastName = configuration["Seed:AdminLastName"] ?? "SuperAdmin",
                        IsActive = true
                    };

                    var createResult = await userMgr.CreateAsync(admin, adminPassword);
                    if (!createResult.Succeeded)
                    {
                        logger.LogError("Failed to create administrator account: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        admin = null;
                    }
                }
            }

            if (admin is not null)
            {
                if (!await userMgr.IsInRoleAsync(admin, "SuperAdmin"))
                {
                    await userMgr.AddToRoleAsync(admin, "SuperAdmin");
                }
            }

            // Optional: register OpenIddict client
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
                        "scp:api",
                    }
                });
            }
        }
    }
}
