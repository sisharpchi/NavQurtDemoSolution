using Microsoft.AspNetCore.Identity;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Infrastructure.Data;

namespace NavQurt.Server.Web.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddAppIdentity(this IServiceCollection services)
        {
            services.AddIdentity<AppUser, AppRole>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequiredLength = 6;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<MainDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}
