// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
using System.Linq;
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
    public class AccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSender _emailSender;
        private readonly ILogger<AccountsController> _logger;
        private readonly SmsSender _smsSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountsController(ApplicationDbContext context, EmailSender emailSender,
            ILogger<AccountsController> logger, SmsSender smsSender, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _smsSender = smsSender;
            _userManager = userManager;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<AccountViewModel>> GetAccount([FromRoute] Guid id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var nameClaim = await _context.UserClaims
                .Where(x => x.ClaimType == "name")
                .FirstOrDefaultAsync(x => x.UserId == id);
            if (nameClaim == null)
            {
                nameClaim = new IdentityUserClaim<Guid>
                {
                    UserId = user.Id,
                    ClaimType = "name",
                    ClaimValue = ""
                };

                await _context.AddAsync(nameClaim);
                await _context.SaveChangesAsync();
            }

            var retViewModel = new AccountViewModel
            {
                Id = user.Id,
                Name = nameClaim.ClaimValue,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return Ok(retViewModel);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AccountViewModel>> PutAccount([FromRoute] Guid id,
            [FromBody] AccountViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (viewModel == null || id != viewModel.Id)
            {
                return BadRequest();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (viewModel.Email != user.Email)
            {
                user.UserName = viewModel.Email;
                user.NormalizedUserName = viewModel.Email.ToUpper();
                user.Email = viewModel.Email;
                user.NormalizedEmail = viewModel.Email.ToUpper();
                user.EmailConfirmed = false;

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                if (!string.IsNullOrEmpty(token))
                {
                    const string subject = "Confirm your Chabloom account email address";
                    await _emailSender.SendEmailAsync(user.Email, subject, token);

                    _logger.LogInformation($"User {user.Id} email confirmation link sent");
                }
            }

            if (viewModel.PhoneNumber != user.PhoneNumber)
            {
                user.PhoneNumber = viewModel.PhoneNumber;
                user.PhoneNumberConfirmed = false;

                var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
                if (!string.IsNullOrEmpty(token))
                {
                    await _smsSender.SendSmsAsync(user.PhoneNumber, token);

                    _logger.LogInformation($"User {user.Id} phone number confirmation link sent");
                }
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            var nameClaim = await _context.UserClaims
                .Where(x => x.ClaimType == "name")
                .FirstOrDefaultAsync(x => x.UserId == id);
            if (nameClaim == null)
            {
                nameClaim = new IdentityUserClaim<Guid>
                {
                    UserId = user.Id,
                    ClaimType = "name",
                    ClaimValue = viewModel.Name
                };

                await _context.AddAsync(nameClaim);
                await _context.SaveChangesAsync();
            }
            else
            {
                nameClaim.ClaimValue = viewModel.Name;

                _context.Update(nameClaim);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"User updated account {user.Id}");

            var retViewModel = new AccountViewModel
            {
                Id = user.Id,
                Name = nameClaim.ClaimValue,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return Ok(retViewModel);
        }
    }
}