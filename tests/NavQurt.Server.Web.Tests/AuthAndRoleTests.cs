using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Core.Constants;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Web.Tests.Infrastructure;
using Xunit;

namespace NavQurt.Server.Web.Tests;

public class AuthAndRoleTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AuthAndRoleTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RoleSeederCreatesDefaultRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        foreach (var roleName in RoleConstants.DefaultRoles)
        {
            (await roleManager.RoleExistsAsync(roleName)).Should().BeTrue($"Role '{roleName}' should exist after seeding.");
        }
    }

    [Fact]
    public async Task SignUpAssignsDefaultUserRole()
    {
        var client = _factory.CreateClient();
        var userName = $"user_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("api/v1/auth/sign-up", new SignUpRequest(userName, "P@ssw0rd!", "Test", "User", $"{userName}@mail.test", null));
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByNameAsync(userName);
        user.Should().NotBeNull();
        var roles = await userManager.GetRolesAsync(user!);
        roles.Should().Contain(RoleConstants.User);
    }

    [Fact]
    public async Task LoginReturnsTokensAndRefreshRotatesToken()
    {
        var client = _factory.CreateClient();
        var userName = $"login_{Guid.NewGuid():N}";
        var password = "P@ssw0rd!";
        await client.PostAsJsonAsync("api/v1/auth/sign-up", new SignUpRequest(userName, password, null, null, null, null));

        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(userName, password, false));
        loginResponse.EnsureSuccessStatusCode();

        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginDto.Should().NotBeNull();
        loginDto!.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginDto.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refreshResponse = await client.PostAsJsonAsync("api/v1/auth/refresh", new RefreshTokenRequest(loginDto.Tokens.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        refreshed.Should().NotBeNull();
        refreshed!.Tokens.RefreshToken.Should().NotBe(loginDto.Tokens.RefreshToken);
        refreshed.Tokens.AccessToken.Should().NotBe(loginDto.Tokens.AccessToken);
    }

    [Fact]
    public async Task SuperAdminCanManageRolesAndAssignToUsers()
    {
        var client = _factory.CreateClient();
        var targetUserName = $"target_{Guid.NewGuid():N}";
        var password = "P@ssw0rd!";
        await client.PostAsJsonAsync("api/v1/auth/sign-up", new SignUpRequest(targetUserName, password, null, null, $"{targetUserName}@mail.test", null));

        string superAdminPassword = "SuperP@ss1!";
        string superAdminUserName = $"super_{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var superAdmin = new AppUser { UserName = superAdminUserName, Email = $"{superAdminUserName}@mail.test", IsActive = true };
            (await userManager.CreateAsync(superAdmin, superAdminPassword)).Succeeded.Should().BeTrue();
            (await userManager.AddToRoleAsync(superAdmin, RoleConstants.SuperAdmin)).Succeeded.Should().BeTrue();
        }

        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(superAdminUserName, superAdminPassword, false));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginDto.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.Tokens.AccessToken);

        var createRoleResponse = await client.PostAsJsonAsync("api/v1/roles", new CreateRoleRequest("Support"));
        createRoleResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var createdRole = await createRoleResponse.Content.ReadFromJsonAsync<RoleDto>();
        createdRole.Should().NotBeNull();

        using var scope2 = _factory.Services.CreateScope();
        var userManagerScope = scope2.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var targetUser = await userManagerScope.FindByNameAsync(targetUserName);
        targetUser.Should().NotBeNull();

        var assignResponse = await client.PostAsJsonAsync("api/v1/roles/assign", new RoleAssignmentRequest(targetUser!.Id, new[] { createdRole!.Name }));
        assignResponse.EnsureSuccessStatusCode();

        var roles = await userManagerScope.GetRolesAsync(targetUser);
        roles.Should().Contain("Support");
    }

    [Fact]
    public async Task AdminCannotCreateElevatedRoles()
    {
        var client = _factory.CreateClient();
        var adminUserName = $"admin_{Guid.NewGuid():N}";
        var password = "P@ssw0rd!";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var adminUser = new AppUser { UserName = adminUserName, Email = $"{adminUserName}@mail.test", IsActive = true };
            (await userManager.CreateAsync(adminUser, password)).Succeeded.Should().BeTrue();
            (await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin)).Succeeded.Should().BeTrue();
        }

        var loginResponse = await client.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(adminUserName, password, false));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginDto.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.Tokens.AccessToken);

        var response = await client.PostAsJsonAsync("api/v1/roles", new CreateRoleRequest(RoleConstants.SuperAdmin));
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }
}
