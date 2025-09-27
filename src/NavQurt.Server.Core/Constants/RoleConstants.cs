using System.Collections.Generic;

namespace NavQurt.Server.Core.Constants;

/// <summary>
///     Provides strongly-typed names for well-known application roles.
/// </summary>
public static class RoleConstants
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    ///     Returns the canonical list of roles that must exist in the system.
    /// </summary>
    public static IReadOnlyList<string> DefaultRoles { get; } = new[] { User, Admin, SuperAdmin };
}
