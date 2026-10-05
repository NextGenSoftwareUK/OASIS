using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Services;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Controllers
{
    /// <summary>
    /// REST API controller for oracle operations
    /// </summary>
    [ApiController]
    [Route("api/v1/oracle")]
    public class OracleController : ControllerBase
    {
        private readonly OracleService _oracleService;
        
        public OracleController()
        {
            _oracleService = new OracleService();
        }
        
        /// <summary>
        /// Get current ZEC price
        /// </summary>
        [HttpGet("zec-price")]
        public async Task<IActionResult> GetZECPrice()
        {
            try
            {
                var result = await _oracleService.GetZECPriceAsync();
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(new
                {
                    price = result.Result,
                    timestamp = DateTime.UtcNow,
                    currency = "USD"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<decimal>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Get price history
        /// </summary>
        [HttpGet("price-history")]
        [Authorize] // Requires authentication for history
        public async Task<IActionResult> GetPriceHistory([FromQuery] int hours = 24)
        {
            try
            {
                // TODO: Load price history from oracle holon
                var history = new
                {
                    prices = new object[0],
                    hours = hours
                };
                
                return Ok(new OASISResult<object>
                {
                    Result = history,
                    IsError = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<object>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
    }
}

