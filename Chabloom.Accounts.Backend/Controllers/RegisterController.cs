// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Security.Claims;
using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.ViewModels;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Chabloom.Accounts.Backend.Controllers
{
    /// <summary>
    ///     This controller is responsible for registering application users
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RegisterController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        ///     Register an application user
        /// </summary>
        /// <param name="viewModel">The view model</param>
        /// <returns>A 200 status code on success, else a failure status code</returns>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> PostRegister([FromBody] RegisterViewModel viewModel)
        {
            // Validate the model passed to the endpoint
            if (!ModelState.IsValid || viewModel == null)
            {
                return BadRequest(ModelState);
            }

            // Initialize the user object
            var user = new ApplicationUser
            {
                UserName = viewModel.Email,
                Email = viewModel.Email,
                PhoneNumber = viewModel.Phone
            };

            // Create the user
            var result = await _userManager.CreateAsync(user, viewModel.Password)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Add the user name claim
            await _userManager.AddClaimAsync(user, new Claim(JwtClaimTypes.Name, viewModel.Name))
                .ConfigureAwait(false);

            // Return success status code
            return Ok();
        }
    }
}