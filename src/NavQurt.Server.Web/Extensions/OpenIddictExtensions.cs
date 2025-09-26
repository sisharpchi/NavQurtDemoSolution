using OpenIddict.Abstractions;

namespace NavQurt.Server.Web.Extensions
{
    public static class OpenIddictExtensions
    {
        public static IServiceCollection AddAppOpenIddict(this IServiceCollection services)
        {
            services.AddOpenIddict()
                .AddServer(o =>
                {
                    o.SetTokenEndpointUris("/connect/token");

                    o.AllowPasswordFlow().AcceptAnonymousClients();
                    o.AllowRefreshTokenFlow();

                    o.AddDevelopmentEncryptionCertificate()
                     .AddDevelopmentSigningCertificate();

                    o.UseAspNetCore()
                     .EnableTokenEndpointPassthrough();

                    o.SetAccessTokenLifetime(TimeSpan.FromMinutes(60));
                    o.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                    o.RegisterScopes(OpenIddictConstants.Scopes.OpenId,
                                     OpenIddictConstants.Scopes.Profile,
                                     OpenIddictConstants.Scopes.OfflineAccess,
                                     "api");
                })
                .AddValidation(o =>
                {
                    o.UseLocalServer();
                    o.UseAspNetCore();
                });

            return services;
        }
    }
}
