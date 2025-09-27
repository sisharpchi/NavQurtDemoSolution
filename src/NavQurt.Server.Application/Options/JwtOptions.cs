using System;

namespace NavQurt.Server.Application.Options;

/// <summary>
///     Configuration settings used for issuing JSON Web Tokens and refresh tokens.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    ///     Symmetric signing key used for JWT issuance.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     The issuer claim to embed in generated access tokens.
    /// </summary>
    public string? Issuer { get; set; } = string.Empty;

    /// <summary>
    ///     The intended audience of generated access tokens.
    /// </summary>
    public string? Audience { get; set; } = string.Empty;

    /// <summary>
    ///     Lifetime of issued access tokens. Defaults to 15 minutes.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    ///     Lifetime of issued refresh tokens. Defaults to 7 days.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
