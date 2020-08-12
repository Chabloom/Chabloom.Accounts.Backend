// Copyright 2020 Chabloom LC. All rights reserved.

using System.Threading.Tasks;
using Chabloom.Accounts.Data;
using Chabloom.Accounts.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Chabloom.Accounts.Controllers
{
    /// <summary>
    ///     This controller is responsible for managing authentication of application users
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthenticationController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _claimsPrincipalFactory = claimsPrincipalFactory;
        }

        /// <summary>
        ///     Log a user into the application
        /// </summary>
        /// <param name="model">The login view model</param>
        /// <returns>A 204 status code on success, else a failure status code</returns>
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // Validate the model passed to the endpoint
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
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            // Find the user claims principal
            var claimsPrincipal = await _claimsPrincipalFactory.CreateAsync(user)
                .ConfigureAwait(false);

            // Sign the user in with a cookie
            await HttpContext.SignInAsync(claimsPrincipal)
                .ConfigureAwait(false);

            // Return success status code
            return NoContent();
        }

        /// <summary>
        ///     Sign a user out of the application
        /// </summary>
        /// <returns>A 204 response code</returns>
        [HttpPost("Logout")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Logout()
        {
            // Sign the user out
            await HttpContext.SignOutAsync()
                .ConfigureAwait(false);

            // Return success status code
            return NoContent();
        }
    }
}