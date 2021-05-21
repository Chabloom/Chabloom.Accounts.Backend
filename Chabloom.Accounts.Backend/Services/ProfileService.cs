// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;

namespace Chabloom.Accounts.Backend.Services
{
    public class ProfileService : IProfileService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager,
            IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            var userPrincipal = await _userClaimsPrincipalFactory.CreateAsync(user);

            var claims = userPrincipal.Claims
                .ToList();
            claims = claims
                .Where(x => context.RequestedClaimTypes.Contains(x.Type))
                .ToList();

            if (!_userManager.SupportsUserRole)
            {
                return;
            }

            var roleNames = await _userManager.GetRolesAsync(user);
            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, roleName));
                if (!_roleManager.SupportsRoleClaims)
                {
                    continue;
                }

                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    claims.AddRange(await _roleManager.GetClaimsAsync(role));
                }
            }
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
    }
}