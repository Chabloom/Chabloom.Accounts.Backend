// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Security.Claims;
using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.Services;
using Chabloom.Accounts.Backend.ViewModels;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        private readonly EmailSender _emailSender;
        private readonly ILogger<RegisterController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterController(EmailSender emailSender, ILogger<RegisterController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _emailSender = emailSender;
            _logger = logger;
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
            var result = await _userManager.CreateAsync(user, viewModel.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            _logger.LogInformation($"User with email {viewModel.Email} registered");

            // Add the user name claim
            await _userManager.AddClaimAsync(user, new Claim(JwtClaimTypes.Name, viewModel.Name));

            _logger.LogInformation($"User with email {viewModel.Email} added name claim {viewModel.Name}");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            if (!string.IsNullOrEmpty(token))
            {
                const string subject = "Confirm your Chabloom account";
                await _emailSender.SendEmailAsync(user.Email, subject, token);

                _logger.LogInformation($"User {user.Id} email confirmation link sent");
            }

            // Return success status code
            return Ok();
        }
    }
}