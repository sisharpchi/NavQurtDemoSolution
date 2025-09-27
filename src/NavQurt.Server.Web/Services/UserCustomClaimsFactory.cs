using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Core.Persistence;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace NavQurt.Server.Web.Services
{
    public class UserCustomClaimsFactory : UserClaimsPrincipalFactory<AppUser, AppRole>
    {
        private readonly IMainRepository _mainRepository;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public UserCustomClaimsFactory(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IOptions<IdentityOptions> options,
            IMainRepository mainRepository)
            : base(userManager, roleManager, options)
        {
            _mainRepository = mainRepository;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
        {
            var claimsIdentity = await base.GenerateClaimsAsync(user);

            await AddAppRoleClaimsAsync(claimsIdentity, user);
            //var companyAndOrganization = await FindUserCompanies(user);
            //if (companyAndOrganization == null)
            //{
                return claimsIdentity;
            //}

            //claimsIdentity.AddClaim(new Claim(CustomClaimTypes.Company, companyAndOrganization.CompanyToken));
            //claimsIdentity.AddClaim(new Claim(CustomClaimTypes.OrganizationCode, companyAndOrganization.OrganizationCode));

            //return claimsIdentity;
        }

        private async Task AddAppRoleClaimsAsync(ClaimsIdentity claimsIdentity, AppUser user)
        {
            var appRoles = await _userManager.GetRolesAsync(user);

            foreach (var roleName in appRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);

                if (role == null)
                    continue;

                var claims = await _roleManager.GetClaimsAsync(role);
                claimsIdentity.AddClaims(claims);
            }
        }
    }
}
