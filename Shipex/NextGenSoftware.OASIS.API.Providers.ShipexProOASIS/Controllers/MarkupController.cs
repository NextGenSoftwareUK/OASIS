using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers
{
    /// <summary>
    /// Controller for managing markup configurations.
    /// Provides CRUD operations for markup settings.
    /// </summary>
    [ApiController]
    [Route("api/shipexpro/markups")]
    public class MarkupController : ControllerBase
    {
        private readonly MarkupConfigurationService _markupService;

        public MarkupController(MarkupConfigurationService markupService)
        {
            _markupService = markupService ?? throw new ArgumentNullException(nameof(markupService));
        }

        /// <summary>
        /// Get all markups, optionally filtered by merchant ID.
        /// </summary>
        /// <param name="merchantId">Optional merchant ID to filter by</param>
        /// <returns>List of markup configurations</returns>
        [HttpGet]
        public async Task<ActionResult<OASISResult<List<MarkupConfiguration>>>> GetMarkups([FromQuery] Guid? merchantId = null)
        {
            var result = await _markupService.GetMarkupsAsync(merchantId);
            
            if (result.IsError)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Get a markup configuration by ID.
        /// </summary>
        /// <param name="markupId">Markup identifier</param>
        /// <returns>Markup configuration</returns>
        [HttpGet("{markupId}")]
        public async Task<ActionResult<OASISResult<MarkupConfiguration>>> GetMarkup(Guid markupId)
        {
            var result = await _markupService.GetMarkupAsync(markupId);
            
            if (result.IsError)
            {
                if (result.Message?.Contains("not found") == true)
                    return NotFound(result);
                return BadRequest(result);
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Create a new markup configuration.
        /// </summary>
        /// <param name="markup">Markup configuration to create</param>
        /// <returns>Created markup configuration</returns>
        [HttpPost]
        public async Task<ActionResult<OASISResult<MarkupConfiguration>>> CreateMarkup([FromBody] MarkupConfiguration markup)
        {
            if (markup == null)
                return BadRequest(new OASISResult<MarkupConfiguration> { IsError = true, Message = "Markup configuration is required" });

            var result = await _markupService.CreateMarkupAsync(markup);
            
            if (result.IsError)
                return BadRequest(result);
            
            return CreatedAtAction(nameof(GetMarkup), new { markupId = result.Result.MarkupId }, result);
        }

        /// <summary>
        /// Update an existing markup configuration.
        /// </summary>
        /// <param name="markupId">Markup identifier</param>
        /// <param name="markup">Updated markup configuration</param>
        /// <returns>Updated markup configuration</returns>
        [HttpPut("{markupId}")]
        public async Task<ActionResult<OASISResult<MarkupConfiguration>>> UpdateMarkup(Guid markupId, [FromBody] MarkupConfiguration markup)
        {
            if (markup == null)
                return BadRequest(new OASISResult<MarkupConfiguration> { IsError = true, Message = "Markup configuration is required" });

            var result = await _markupService.UpdateMarkupAsync(markupId, markup);
            
            if (result.IsError)
            {
                if (result.Message?.Contains("not found") == true)
                    return NotFound(result);
                return BadRequest(result);
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Delete a markup configuration.
        /// </summary>
        /// <param name="markupId">Markup identifier</param>
        /// <returns>Success result</returns>
        [HttpDelete("{markupId}")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteMarkup(Guid markupId)
        {
            var result = await _markupService.DeleteMarkupAsync(markupId);
            
            if (result.IsError)
            {
                if (result.Message?.Contains("not found") == true)
                    return NotFound(result);
                return BadRequest(result);
            }
            
            return Ok(result);
        }
    }
}




