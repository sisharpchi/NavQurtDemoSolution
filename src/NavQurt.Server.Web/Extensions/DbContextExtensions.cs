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
                options.UseNpgsql(configuration.GetConnectionString("Default"))
            );

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Default"))
            );

            return services;
        }
    }
}
