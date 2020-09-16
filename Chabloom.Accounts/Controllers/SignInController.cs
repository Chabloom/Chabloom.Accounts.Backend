// Copyright 2020 Chabloom LC. All rights reserved.

using System.Threading.Tasks;
using Chabloom.Accounts.Data;
using Chabloom.Accounts.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Chabloom.Accounts.Controllers
{
    /// <summary>
    ///     This controller is responsible for signing in application users
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SignInController : ControllerBase
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;


        public SignInController(IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     Log a user into the application
        /// </summary>
        /// <param name="viewModel">The view model</param>
        /// <returns>A 200 status code on success, else a failure status code</returns>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> PostSignIn([FromBody] SignInViewModel viewModel)
        {
            // Validate the model passed to the endpoint
            if (!ModelState.IsValid || viewModel == null)
            {
                return BadRequest(ModelState);
            }

            // Find the user by email address
            var user = await _userManager.FindByEmailAsync(viewModel.Email)
                .ConfigureAwait(false);
            if (user == null)
            {
                return Unauthorized();
            }

            // Sign the user in with a password
            var result = await _signInManager.CheckPasswordSignInAsync(user, viewModel.Password, false)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            // Sign the user in to the application
            await _signInManager.SignInAsync(user, viewModel.Remember)
                .ConfigureAwait(false);

            // Find the user claims principal
            var claimsPrincipal = await _claimsPrincipalFactory.CreateAsync(user)
                .ConfigureAwait(false);

            // Sign the user in with a cookie
            await HttpContext.SignInAsync(claimsPrincipal)
                .ConfigureAwait(false);

            // Return success status code
            return Ok();
        }
    }
}