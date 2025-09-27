using NavQurt.Server.Core.Entities;
using OpenIddict.Abstractions;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NavQurt.Server.Web.HostedServices
{
    public class OpenIddictSeederService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OpenIddictSeederService> _logger;

        public OpenIddictSeederService(IServiceProvider serviceProvider, ILogger<OpenIddictSeederService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            await SeedWebClientAsync(applicationManager, stoppingToken);

            _logger.LogInformation("✅ OpenIddict clients seeded successfully.");
        }

        private async Task SeedWebClientAsync(IOpenIddictApplicationManager applicationManager, CancellationToken ct)
        {
            const string clientId = "web-client";

            if (await applicationManager.FindByClientIdAsync(clientId, ct) is not null)
            {
                _logger.LogInformation("🔁 Client '{ClientId}' already exists.", clientId);
                return;
            }
            var permissions = new[]
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.ResponseTypes.Token,
                Permissions.Prefixes.GrantType + "switch_company"
            };
            var descriptor = new OpenIdApplication
            {
                ClientId = clientId,
                DisplayName = "NavQurt",
                Permissions = JsonSerializer.Serialize(permissions)

            };

            object? client = await applicationManager.FindByClientIdAsync(descriptor.ClientId, ct);
            if (client == null)
            {
                await applicationManager.CreateAsync(descriptor, ct);
                _logger.LogInformation("✅ Created client: {ClientId}", clientId);
            }
            else
            {
                // await applicationManager.UpdateAsync(client, descriptor, ct);
                // _logger.LogInformation("✅ Updated client: {ClientId}", clientId);
            }
        }
    }
}
