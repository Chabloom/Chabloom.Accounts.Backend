// Copyright 2020 Chabloom LC. All rights reserved.

using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Payments.Account.Data;
using Payments.Account.Models;

namespace Payments.Account.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;

        public AuthenticationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _claimsPrincipalFactory = claimsPrincipalFactory;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(ModelState);
            }

            // Find the user by email address
            var user = await _userManager.FindByEmailAsync(model.Email)
                .ConfigureAwait(false);
            if (user == null)
            {
                return Unauthorized();
            }

            // Sign the user in with a password
            await _signInManager.SignInAsync(user, true)
                .ConfigureAwait(false);
            //var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false)
            //.ConfigureAwait(false);
            //if (!result.Succeeded)
            //{
            //    return Unauthorized();
            //}

            // Find the user claims principal
            var claimsPrincipal = await _claimsPrincipalFactory.CreateAsync(user)
                .ConfigureAwait(false);

            // Sign the user in with a cookie
            await HttpContext.SignInAsync(claimsPrincipal)
                .ConfigureAwait(false);

            // Return success
            return Ok();
        }

        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _signInManager.SignOutAsync()
                .ConfigureAwait(false);

            return Ok();
        }
    }
}