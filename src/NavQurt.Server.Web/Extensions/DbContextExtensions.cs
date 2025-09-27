using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NavQurt.Server.Infrastructure.Data;

namespace NavQurt.Server.Web.Extensions
{
    public static class DbContextExtensions
    {
        public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("MainDatabase"));
            });

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("AppDatabase"))
            );

            return services;
        }
    }
}
