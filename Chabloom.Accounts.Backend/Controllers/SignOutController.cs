// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.ViewModels;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Chabloom.Accounts.Backend.Controllers
{
    /// <summary>
    ///     This controller is responsible for signing out application users
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SignOutController : Controller
    {
        private readonly IIdentityServerInteractionService _interactionService;
        private readonly ILogger<SignOutController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SignOutController(SignInManager<ApplicationUser> signInManager, ILogger<SignOutController> logger,
            IIdentityServerInteractionService interactionService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _interactionService = interactionService;
        }

        /// <summary>
        ///     Sign a user out of the application
        /// </summary>
        /// <param name="viewModel">The view model</param>
        /// <returns>A 200 response code</returns>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<SignOutViewModel>> PostSignOut([FromBody] SignOutViewModel viewModel)
        {
            // Validate the model passed to the endpoint
            if (!ModelState.IsValid || viewModel == null)
            {
                return BadRequest(ModelState);
            }

            // Get the logout context
            var context = await _interactionService.GetLogoutContextAsync(viewModel.Id)
                .ConfigureAwait(false);

            // Sign the user out with a cookie
            await HttpContext.SignOutAsync()
                .ConfigureAwait(false);

            _logger.LogInformation($"Sign out id {viewModel.Id} auth cookie removed");

            // Sign the user out of the application
            await _signInManager.SignOutAsync()
                .ConfigureAwait(false);

            _logger.LogInformation($"Sign out id {viewModel.Id} signed out of application");

            viewModel.PostLogoutRedirectUri = context.PostLogoutRedirectUri;

            // Return success status code
            return Ok(viewModel);
        }
    }
}