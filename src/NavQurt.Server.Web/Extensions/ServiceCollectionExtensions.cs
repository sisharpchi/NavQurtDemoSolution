using Microsoft.OpenApi.Models;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Infrastructure.Data;
using OpenIddict.Abstractions;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;

namespace NavQurt.Server.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {

            services.AddSwaggerGen(c =>
            {
                // Swagger-документ для контроллеров в Area("Web")
                //c.SwaggerDoc("web", new OpenApiInfo { Title = "Web API", Version = "v1" });

                // Swagger-документ для контроллеров вне area (например, SyncController и прочие)
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "General API", Version = "v1" });

                // Включать контроллеры в нужный документ на основе их area
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    apiDesc.ActionDescriptor.RouteValues.TryGetValue("area", out var area);

                    // doc "web" показывает только контроллеры из Area("Web")
                    if (docName.Equals("web", StringComparison.OrdinalIgnoreCase))
                        return string.Equals(area, "Web", StringComparison.OrdinalIgnoreCase);

                    // doc "v1" — всё без area
                    if (docName.Equals("v1", StringComparison.OrdinalIgnoreCase))
                        return string.IsNullOrEmpty(area);

                    return false;
                });


                // XML комментарии (опционально, если есть .xml-файл документации)
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                //c.SwaggerEndpoint("/swagger/web/swagger.json", "Web API v1");
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "General API v1");

                c.DocumentTitle = "NavQurt API Documentation";
                c.DocExpansion(DocExpansion.List);
                c.RoutePrefix = "swagger";
            });

            return app;
        }

        public static IServiceCollection RegisterOpenIddict(this IServiceCollection services)
        {
            services.AddOpenIddict()
                      // Register the OpenIddict core components.
                      .AddCore(options =>
                      {
                          // Configure OpenIddict to use the Entity Framework Core stores and models.
                          // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                          options.UseEntityFrameworkCore()
                                 .UseDbContext<MainDbContext>()
                                 .ReplaceDefaultEntities<OpenIdApplication, OpenIdAuthorization, OpenIdScope, OpenIdToken, long>();

                          // Enable Quartz.NET integration.
                          //  options.UseQuartz();
                      })
                      // Register the OpenIddict server components.
                      .AddServer(options =>
                      {
                          options.RegisterScopes("read", "write", OpenIddictConstants.Scopes.OfflineAccess);
                          // Enable the token endpoint.
                          options.SetTokenEndpointUris("security/oauth/token");

                          // Enable the client credentials flow.
                          options.AllowClientCredentialsFlow()
                                 .AllowPasswordFlow()
                                    .AllowRefreshTokenFlow();

                          options.AllowPasswordFlow()
                                    .AllowRefreshTokenFlow();

                          options.AllowCustomFlow("switch_company").AllowRefreshTokenFlow();

                          // Register the signing and encryption credentials.
                          options.AddDevelopmentEncryptionCertificate()
                                 .AddDevelopmentSigningCertificate();

                          // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                          options.UseAspNetCore()
                                  .DisableTransportSecurityRequirement()
                                 .EnableTokenEndpointPassthrough();

                          options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));
                          options.SetAccessTokenLifetime(TimeSpan.FromHours(24));
                      })
                      // Register the OpenIddict validation components.
                      .AddValidation(options =>
                      {
                          // Import the configuration from the local OpenIddict server instance.
                          options.UseLocalServer();

                          // Register the ASP.NET Core host.
                          options.UseAspNetCore();
                      });
            return services;
        }
    }
}
