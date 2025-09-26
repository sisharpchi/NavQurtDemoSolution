using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NavQurt.Server.Core.Entities;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NavQurt.Server.Web.Controllers
{
    [Route("/security/oauth")]
    [ApiController]
    public class OAuthAuthorizationController : ControllerBase
    {
        private readonly ILogger<OAuthAuthorizationController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public OAuthAuthorizationController(
            ILogger<OAuthAuthorizationController> logger,
            UserManager<AppUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost("token"), IgnoreAntiforgeryToken, Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new ArgumentException("OpenIddict request is missing.");

            // Password grant flow
            if (request.IsPasswordGrantType())
            {
                var user = await _userManager.FindByNameAsync(request.Username!);
                if (user == null)
                {
                    return Unauthorized(new OpenIddictResponse
                    {
                        Error = Errors.InvalidGrant,
                        ErrorDescription = "Неверный логин или пароль."
                    });
                }

                var passwordOk = await _userManager.CheckPasswordAsync(user, request.Password!);
                if (!passwordOk)
                {
                    return Unauthorized(new OpenIddictResponse
                    {
                        Error = Errors.InvalidGrant,
                        ErrorDescription = "Неверный логин или пароль."
                    });
                }

                var identity = await CreatePrincipalAsync(user, request);

                return SignIn(new ClaimsPrincipal(identity),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Refresh token flow
            if (request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = result.Principal;

                if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
                {
                    return Unauthorized(new OpenIddictResponse
                    {
                        Error = Errors.InvalidGrant,
                        ErrorDescription = "The refresh token is invalid."
                    });
                }

                var userId = principal.GetClaim(Claims.Subject);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                var identity = await CreatePrincipalAsync(user, request);

                // Keep original scopes
                identity.SetScopes(principal.GetScopes());
                identity.SetDestinations(GetDestinations);

                return SignIn(new ClaimsPrincipal(identity),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Unsupported grant type
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }

        private async Task<ClaimsIdentity> CreatePrincipalAsync(AppUser user, OpenIddictRequest openIddictRequest)
        {
            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name, Claims.Role);

            identity.SetClaim(Claims.Subject, user.Id.ToString());
            identity.SetClaim(ClaimTypes.NameIdentifier, user.Id.ToString());
            identity.SetClaim(Claims.GivenName, user.FirstName ?? "");
            identity.SetClaim(Claims.FamilyName, user.LastName ?? "");
            identity.SetClaim(OpenIddictConstants.Claims.PhoneNumber, user.PhoneNumber ?? "");

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.SetClaim(Claims.Role, role);
            }

            identity.SetScopes(openIddictRequest.GetScopes());

            identity.SetDestinations(GetDestinations);

            return identity;
        }

        private static IEnumerable<string> GetDestinations(Claim claim)
        {
            return claim.Type switch
            {
                Claims.Name
                    or Claims.Subject
                    or Claims.Role
                    or Claims.GivenName
                    or Claims.FamilyName
                    or OpenIddictConstants.Claims.PhoneNumber
                    => new[] { Destinations.AccessToken, Destinations.IdentityToken },

                _ => new[] { Destinations.AccessToken }
            };
        }
    }
}