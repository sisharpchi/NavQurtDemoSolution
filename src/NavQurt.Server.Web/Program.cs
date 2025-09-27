using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NavQurt.Server.Application.Interfaces;
using NavQurt.Server.Application.Options;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Infrastructure;
using NavQurt.Server.Web.Authorization;
using NavQurt.Server.Web.Conventions;
using NavQurt.Server.Web.Extensions;
using NavQurt.Server.Web.HostedServices;
using NavQurt.Server.Web.Mapper;
using NavQurt.Server.Web.ParameterTransformers;
using NavQurt.Server.Web.Services;
using Serilog;
using System.Text;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
try
{
    var builder = WebApplication.CreateBuilder(args);

    var configuration = builder.Configuration
                               .SetBasePath(System.AppContext.BaseDirectory)
                               .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                               .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                               .AddEnvironmentVariables()
                               .Build();

    Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .CreateLogger();

    builder.Host.UseSerilog();

    builder.WebHost.UseConfiguration(builder.Configuration);

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

    builder.Services.AddControllers(options =>
    {
        //options.InputFormatters.Insert(0, MyJPIF.GetJsonPatchInputFormatter());
        options.AllowEmptyInputInBodyModelBinding = false;
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        options.Conventions.Add(new WebAreaSlugifyConvention(new SlugifyParameterTransformer()));
    });

    builder.Services.AddAuthorization(options => AuthorizationPolicies.RegisterPolicies(options));

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
        options.SuppressInferBindingSourcesForParameters = false;

        //options.InvalidModelStateResponseFactory = context =>
        //{
        //    return new BadRequestObjectResult(context.ModelState);
        //};

    });

    builder.Services.AddAppDbContext(configuration);

    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddInfrastructureLayer(builder.Configuration, builder =>
    {
        builder.AddClaimsPrincipalFactory<UserCustomClaimsFactory>();

    }, mainDbContextOptions =>
    {

        mainDbContextOptions.UseOpenIddict<OpenIdApplication, OpenIdAuthorization, OpenIdScope, OpenIdToken, long>();
    });

    builder.Services.AddHostedService<RoleSeederHostedService>();

    var jwtConfiguration = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
    if (string.IsNullOrWhiteSpace(jwtConfiguration.Key))
    {
        throw new InvalidOperationException("Jwt:Key must be provided in configuration.");
    }

    builder.Services.ConfigureApplicationCookie(options =>
    {
        // Cookie settings
        options.Cookie.HttpOnly = false;
        options.ExpireTimeSpan = TimeSpan.FromHours(36);
        options.Cookie.MaxAge = options.ExpireTimeSpan;
        options.SlidingExpiration = true;
    });
    builder.Services.AddDataProtection();

    builder.Services.RegisterOpenIddict();

    builder.Services.AddMemoryCache();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession();

    var config = TypeAdapterConfig.GlobalSettings;
    MappingConfig.RegisterMappings(config);

    // MappingConfig.RegisterMappings(config);
    //   Mapster  DI

    builder.Services.AddSingleton(config);
    builder.Services.AddScoped<IMapper, ServiceMapper>();

    builder.Services.AddHttpClient("Synchronization", client =>
    {
        client.BaseAddress = new Uri("https://web.navqurt.uz/api/v1/");
    });

    //builder.Services.AddAliposSmsService(configuration);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("corsapp",
        policy =>
        {
            policy
            .SetIsOriginAllowed(_ => true)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        });
    });

    builder.Services.AddSignalR();

    var app = builder.Build();
    var env2 = app.Environment;

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwaggerDocumentation();

        //app.Use(async (context, next) =>
        //{
        //    var path = context.Request.Path.Value;

        //    if (!string.IsNullOrEmpty(path) && path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var physicalPath = Path.Combine(env2.WebRootPath, path.TrimStart('/'));

        //        if (!File.Exists(physicalPath))
        //        {
        //            //     
        //            context.Request.Path = "/img/default/no-image.png";
        //        }
        //    }

        //    await next();
        //});
    }

    app.UseStaticFiles();

    app.UseRouting();
    app.UseCors("corsapp");


    app.UseDeveloperExceptionPage();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();
    app.MapControllers();
    //app.MapHub<SiteHub>("/siteHub");
    app.UseWebSockets();

    //  SentrySdk.CaptureMessage("Hello Sentry");
    app.Run();
}
catch (Exception ex)
{

    Console.WriteLine(ex.ToString());
}

public partial class Program
{
}
