﻿// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Security.Claims;
using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.Services;
using Chabloom.Accounts.Backend.ViewModels.Auth;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chabloom.Accounts.Backend.Controllers.Auth
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;
        private readonly ApplicationDbContext _context;
        private readonly EmailSender _emailSender;
        private readonly IIdentityServerInteractionService _interactionService;
        private readonly ILogger<AuthController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly SmsSender _smsSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(
            IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory,
            ApplicationDbContext context,
            EmailSender emailSender,
            IIdentityServerInteractionService interactionService,
            ILogger<AuthController> logger,
            SignInManager<ApplicationUser> signInManager,
            SmsSender smsSender,
            UserManager<ApplicationUser> userManager)
        {
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _context = context;
            _emailSender = emailSender;
            _interactionService = interactionService;
            _logger = logger;
            _signInManager = signInManager;
            _smsSender = smsSender;
            _userManager = userManager;
        }

        [HttpPost("SignIn")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AccountSignInAsync([FromBody] SignInViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            // Find the user specified in the login request
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.NormalizedUserName == viewModel.Username.ToUpper());
            if (user == null)
            {
                _logger.LogWarning($"User {viewModel.Username} not found");
                return NotFound();
            }

            // Application sign in
            var result = await _signInManager.PasswordSignInAsync(user, viewModel.Password, viewModel.Persist,
                user.AccessFailedCount > 3);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"User {viewModel.Username} password incorrect");
                return Unauthorized();
            }

            // Get the user claims principal
            var claimsPrincipal = await _claimsPrincipalFactory.CreateAsync(user);

            // Cookie sign in
            await HttpContext.SignInAsync(claimsPrincipal);

            _logger.LogInformation($"User {user.Id} sign in success");

            return NoContent();
        }

        [HttpPost("SignOut/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AccountSignOutAsync([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            // Get the logout context
            var context = await _interactionService.GetLogoutContextAsync(id);

            // Cookie sign out
            await HttpContext.SignOutAsync();

            // Application sign out
            await _signInManager.SignOutAsync();

            _logger.LogInformation($"Sign out {id} success");

            return Ok(context.PostLogoutRedirectUri);
        }

        [HttpPost("Register")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AccountRegisterAsync([FromBody] RegisterViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            // Initialize the user specified in the register request
            var user = new ApplicationUser
            {
                UserName = viewModel.Username,
                Email = viewModel.Email,
                PhoneNumber = viewModel.PhoneNumber
            };

            // Create the user specified in the register request
            var result = await _userManager.CreateAsync(user, viewModel.Password);
            if (!result.Succeeded)
            {
                return BadRequest();
            }

            // Add the user claims
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, viewModel.Name));

            // Send email confirmation link
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            if (!string.IsNullOrEmpty(emailToken))
            {
                const string subject = "Confirm your Chabloom account";
                await _emailSender.SendEmailAsync(user.Email, subject, emailToken);
            }

            // Send sms confirmation link
            var smsToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, viewModel.PhoneNumber);
            if (!string.IsNullOrEmpty(smsToken))
            {
                await _smsSender.SendSmsAsync(user.PhoneNumber, smsToken);
            }

            _logger.LogInformation($"User {user.Id} registration success");

            return NoContent();
        }

        [HttpGet("Error/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AccountErrorAsync([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            // Get the error context
            var context = await _interactionService.GetErrorContextAsync(id);

            // Populate the ret view model
            var retViewModel = new ErrorViewModel
            {
                Error = context.Error,
                ErrorDescription = context.ErrorDescription,
                RedirectUri = context.RedirectUri
            };

            return Ok(retViewModel);
        }
    }
}