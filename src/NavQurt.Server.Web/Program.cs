using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Infrastructure.Data;
using NavQurt.Server.Infrastructure.Seed;
using NavQurt.Server.Web.Extensions;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace NavQurt.Server.Web
{
    public class Program
    {
        public static async Task Main(string[] args) // Change to async Task Main
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddAppDbContext(builder.Configuration);
            builder.Services.AddAppIdentity();
            builder.Services.AddAppOpenIddict();
            builder.Services.AddAppAuthentication();
            builder.Services.AddAppSwagger();

            var app = builder.Build();

            // Middleware
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAppSwagger(app.Environment);

            app.MapControllers();

            await IdentitySeeder.SeedAsync(app.Services);

            app.Run();
        }
    }
}
