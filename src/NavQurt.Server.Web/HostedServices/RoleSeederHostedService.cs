using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NavQurt.Server.Core.Constants;
using NavQurt.Server.Core.Entities;

namespace NavQurt.Server.Web.HostedServices;

/// <summary>
///     Ensures that the default application roles exist when the application starts.
/// </summary>
public sealed class RoleSeederHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoleSeederHostedService> _logger;

    public RoleSeederHostedService(IServiceProvider serviceProvider, ILogger<RoleSeederHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        foreach (var roleName in RoleConstants.DefaultRoles)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogDebug("Role {RoleName} already exists.", roleName);
                continue;
            }

            var role = new AppRole
            {
                Name = roleName,
                NormalizedName = roleManager.NormalizeKey(roleName)
            };

            var result = await roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Created application role {RoleName}.", roleName);
            }
            else
            {
                _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
