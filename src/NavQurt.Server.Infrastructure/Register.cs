using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Core.Persistence;
using NavQurt.Server.Infrastructure.Data;
using NavQurt.Server.Infrastructure.Persistence;

namespace NavQurt.Server.Infrastructure
{
    public static class Register
    {
        private static void AddMainDatabase(
            IServiceCollection services,
            IConfiguration configuration,
            string connectionSection,
            Action<DbContextOptionsBuilder>? optionsAction = null)
        {
            services.AddDbContext<MainDbContext>(options =>
            {
                optionsAction?.Invoke(options);
            });
        }

        private static void AddIdentity(IServiceCollection services, Action<IdentityBuilder>? configureIdentity = null)
        {
            var identityBuilder = services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
                options.User.AllowedUserNameCharacters =
                    "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789_.@+";
                options.User.RequireUniqueEmail = false;

            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<MainDbContext>();

            configureIdentity?.Invoke(identityBuilder);
        }

        public static IServiceCollection AddInfrastructureLayer(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IdentityBuilder>? configureIdentity = null,
            Action<DbContextOptionsBuilder>? optionsAction = null)
        {
            AddIdentity(services, configureIdentity);

            services.AddScoped<IMainRepository, MainRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<UnitOfWork<MainDbContext>>();
            services.AddScoped<UnitOfWork<AppDbContext>>();

            return services;
        }
    }
}
