// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.Threading.Tasks;
using Chabloom.Accounts.Backend.ViewModels;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chabloom.Accounts.Backend.Controllers
{
    /// <summary>
    ///     This controller is responsible for managing errors
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ErrorController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interactionService;

        public ErrorController(IIdentityServerInteractionService interactionService)
        {
            _interactionService = interactionService;
        }

        /// <summary>
        ///     Get an application error
        /// </summary>
        /// <param name="viewModel">The view model</param>
        /// <returns>A 200 response code</returns>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ErrorViewModel>> GetError([FromBody] ErrorViewModel viewModel)
        {
            // Validate the model passed to the endpoint
            if (!ModelState.IsValid || viewModel == null)
            {
                return BadRequest(ModelState);
            }

            // Get the logout context
            var context = await _interactionService.GetErrorContextAsync(viewModel.Id)
                .ConfigureAwait(false);

            viewModel.Error = context.Error;
            viewModel.ErrorDescription = context.ErrorDescription;
            viewModel.RedirectUri = context.RedirectUri;

            // Return success status code
            return Ok(viewModel);
        }
    }
}