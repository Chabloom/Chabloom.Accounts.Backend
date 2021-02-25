// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Chabloom.Accounts.Backend.Controllers
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
        private readonly ILogger<SignInController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SignInController(IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory,
            ILogger<SignInController> logger, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
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

            if (_httpContextAccessor.HttpContext != null)
            {
                foreach (var (key, value) in _httpContextAccessor.HttpContext.Request.Headers)
                {
                    _logger.LogInformation($"{key}: {value}");
                }
            }

            // Find the user by email address
            var user = await _userManager.FindByEmailAsync(viewModel.Email)
                .ConfigureAwait(false);
            if (user == null)
            {
                _logger.LogWarning($"Could not find user with email {viewModel.Email}");
                return Unauthorized();
            }

            // Sign the user in with a password
            var result = await _signInManager.CheckPasswordSignInAsync(user, viewModel.Password, false)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    $"Password sign in failed. Not allowed: {result.IsNotAllowed}. Locked out: {result.IsLockedOut}");
                return Unauthorized();
            }

            // Sign the user in to the application
            await _signInManager.SignInAsync(user, viewModel.Remember)
                .ConfigureAwait(false);

            _logger.LogInformation($"User {viewModel.Email} signed in to application");

            // Find the user claims principal
            var claimsPrincipal = await _claimsPrincipalFactory.CreateAsync(user)
                .ConfigureAwait(false);

            // Sign the user in with a cookie
            await HttpContext.SignInAsync(claimsPrincipal)
                .ConfigureAwait(false);

            _logger.LogInformation($"User {viewModel.Email} auth cookie set");

            // Return success status code
            return Ok();
        }
    }
}