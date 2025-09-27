using Microsoft.AspNetCore.Authorization;
using NavQurt.Server.Core.Constants;

namespace NavQurt.Server.Web.Authorization;

public static class AuthorizationPolicies
{
    public const string RoleManager = nameof(RoleManager);
    public const string ManageElevatedRoles = nameof(ManageElevatedRoles);

    public static void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(RoleManager, policy =>
        {
            policy.RequireRole(RoleConstants.Admin, RoleConstants.SuperAdmin);
        });

        options.AddPolicy(ManageElevatedRoles, policy =>
        {
            policy.RequireRole(RoleConstants.SuperAdmin);
        });
    }
}
