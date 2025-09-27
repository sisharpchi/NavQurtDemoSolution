using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Infrastructure.Data;
using Xunit;

namespace NavQurt.Server.Web.Tests.Infrastructure;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            var mainDescriptors = services.Where(s => s.ServiceType == typeof(DbContextOptions<MainDbContext>)).ToList();
            foreach (var descriptor in mainDescriptors)
            {
                services.Remove(descriptor);
            }

            var appDescriptors = services.Where(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var descriptor in appDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict<OpenIdApplication, OpenIdAuthorization, OpenIdScope, OpenIdToken, long>();
            });

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("AppDbContextTests"));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var mainContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await mainContext.Database.EnsureDeletedAsync();
        await mainContext.Database.EnsureCreatedAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
