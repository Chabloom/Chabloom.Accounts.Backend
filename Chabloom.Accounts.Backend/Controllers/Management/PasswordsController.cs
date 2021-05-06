// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Threading.Tasks;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.Services;
using Chabloom.Accounts.Backend.ViewModels.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chabloom.Accounts.Backend.Controllers.Management
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PasswordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSender _emailSender;
        private readonly ILogger<PasswordsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordsController(ApplicationDbContext context, EmailSender emailSender,
            ILogger<PasswordsController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost("Change")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == viewModel.Id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, viewModel.CurrentPassword, viewModel.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User updated account {user.Id} password");
                return NoContent();
            }

            _logger.LogInformation($"User {user.Id} password update request failed");
            return BadRequest();
        }

        [AllowAnonymous]
        [HttpPost("Reset")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == viewModel.Id);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            if (!string.IsNullOrEmpty(token))
            {
                const string subject = "Reset your Chabloom account password";
                await _emailSender.SendEmailAsync(user.Email, subject, token);

                _logger.LogInformation($"User {user.Id} password reset link sent");
                return NoContent();
            }

            _logger.LogInformation($"User {user.Id} password reset link creation failed");
            return BadRequest();
        }

        [AllowAnonymous]
        [HttpPost("ConfirmReset")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConfirmResetPassword([FromBody] PasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == viewModel.Id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ResetPasswordAsync(user, viewModel.Token, viewModel.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User reset account {user.Id} password");
                return NoContent();
            }

            _logger.LogInformation($"User {user.Id} password reset request failed");
            return BadRequest();
        }
    }
}