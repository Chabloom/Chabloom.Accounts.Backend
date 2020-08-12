// Copyright 2020 Chabloom LC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Chabloom.Accounts.Data;
using Chabloom.Accounts.Models;

namespace Chabloom.Accounts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserViewModel>> Get()
        {
            return Ok(_context.Users
                .Select(x => new UserViewModel
                {
                    Name = _context.UserClaims
                        .Where(y => y.ClaimType == "name")
                        .First(y => y.UserId == x.Id)
                        .ClaimValue,
                    Email = x.Email,
                    EmailConfirmed = x.EmailConfirmed,
                    Phone = x.PhoneNumber,
                    PhoneConfirmed = x.PhoneNumberConfirmed
                }));
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<UserViewModel>> Get(string email)
        {
            return await _context.Users
                .Where(x => x.NormalizedEmail == email.ToUpperInvariant())
                .Select(x => new UserViewModel
                {
                    Name = _context.UserClaims
                        .Where(y => y.ClaimType == "name")
                        .First(y => y.UserId == x.Id)
                        .ClaimValue,
                    Email = x.Email,
                    EmailConfirmed = x.EmailConfirmed,
                    Phone = x.PhoneNumber,
                    PhoneConfirmed = x.PhoneNumberConfirmed
                })
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string email, UserViewModel model)
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(email)
                .ConfigureAwait(false);
            if (user == null)
            {
                return NotFound();
            }

            user.UserName = model.Name;
            if (model.Email != user.Email)
            {
                user.Email = model.Email;
                user.EmailConfirmed = false;
            }

            if (model.Phone != user.PhoneNumber)
            {
                user.PhoneNumber = model.Phone;
                user.PhoneNumberConfirmed = false;
            }

            await _userManager.UpdateAsync(user)
                .ConfigureAwait(false);

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<UserViewModel>> Post(UserViewModel model)
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email)
                .ConfigureAwait(false);
            if (existingUser != null)
            {
                return Conflict();
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone
            };

            var result = await _userManager.CreateAsync(user)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            result = await _userManager.AddClaimAsync(user, new Claim("name", model.Name))
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(model);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string email)
        {
            var user = await _userManager.FindByEmailAsync(email)
                .ConfigureAwait(false);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.DeleteAsync(user)
                .ConfigureAwait(false);

            return NoContent();
        }
    }
}