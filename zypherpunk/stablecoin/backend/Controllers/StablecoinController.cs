using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Holons;
using NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Managers;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Controllers
{
    /// <summary>
    /// REST API controller for stablecoin operations
    /// </summary>
    [ApiController]
    [Route("api/v1/stablecoin")]
    [Authorize] // Requires JWT authentication
    public class StablecoinController : ControllerBase
    {
        private readonly StablecoinManager _stablecoinManager;
        private readonly RiskManager _riskManager;
        private readonly YieldManager _yieldManager;
        
        public StablecoinController()
        {
            _stablecoinManager = new StablecoinManager();
            _riskManager = new RiskManager(new Services.OracleService());
            _yieldManager = new YieldManager();
        }
        
        /// <summary>
        /// Mint stablecoin with ZEC collateral
        /// </summary>
        [HttpPost("mint")]
        public async Task<IActionResult> MintStablecoin([FromBody] MintStablecoinRequest request)
        {
            try
            {
                // Get avatar ID from JWT token
                var avatarId = GetAvatarIdFromToken();
                if (string.IsNullOrEmpty(avatarId))
                {
                    return Unauthorized(new OASISResult<StablecoinPositionHolon>
                    {
                        IsError = true,
                        Message = "Avatar ID not found in token"
                    });
                }
                
                var result = await _stablecoinManager.MintStablecoinAsync(
                    avatarId,
                    request.ZecAmount,
                    request.StablecoinAmount,
                    request.AztecAddress,
                    request.ZcashAddress,
                    request.GenerateViewingKey
                );
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<StablecoinPositionHolon>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Redeem stablecoin for ZEC
        /// </summary>
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemStablecoin([FromBody] RedeemStablecoinRequest request)
        {
            try
            {
                var result = await _stablecoinManager.RedeemStablecoinAsync(
                    request.PositionId,
                    request.StablecoinAmount,
                    request.ZcashAddress
                );
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<string>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Get position by ID
        /// </summary>
        [HttpGet("position/{positionId}")]
        public async Task<IActionResult> GetPosition(string positionId)
        {
            try
            {
                var result = await _stablecoinManager.GetPositionAsync(positionId);
                
                if (result.IsError)
                {
                    return NotFound(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<StablecoinPositionHolon>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Get position health
        /// </summary>
        [HttpGet("position/{positionId}/health")]
        public async Task<IActionResult> GetPositionHealth(string positionId)
        {
            try
            {
                var result = await _riskManager.CheckPositionHealthAsync(positionId);
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<PositionHealth>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Get all positions for current user
        /// </summary>
        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions()
        {
            try
            {
                var avatarId = GetAvatarIdFromToken();
                if (string.IsNullOrEmpty(avatarId))
                {
                    return Unauthorized();
                }
                
                var result = await _stablecoinManager.GetPositionsByAvatarAsync(avatarId);
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<List<StablecoinPositionHolon>>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Liquidate a position
        /// </summary>
        [HttpPost("liquidate/{positionId}")]
        public async Task<IActionResult> LiquidatePosition(string positionId)
        {
            try
            {
                var result = await _riskManager.LiquidatePositionAsync(positionId);
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OASISResult<string>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
        }
        
        /// <summary>
        /// Generate yield for a position
        /// </summary>
        [HttpPost("yield/{positionId}")]
        public async Task<IActionResult> GenerateYield(string positionId)
        {
            try
            {
                var result = await _yieldManager.GenerateYieldAsync(positionId);
                
                if (result.IsError)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
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
        /// Get system information
        /// </summary>
        [HttpGet("system")]
        public async Task<IActionResult> GetSystemInfo()
        {
            try
            {
                // TODO: Load system holon and return info
                var systemInfo = new
                {
                    TotalSupply = 0m,
                    TotalCollateral = 0m,
                    CollateralRatio = 150m,
                    LiquidationThreshold = 120m,
                    CurrentAPY = 5.0m
                };
                
                return Ok(new OASISResult<object>
                {
                    Result = systemInfo,
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
        
        /// <summary>
        /// Extract avatar ID from JWT token
        /// </summary>
        private string GetAvatarIdFromToken()
        {
            // TODO: Extract from JWT token claims
            // For now, return a placeholder
            return User?.FindFirst("avatarId")?.Value ?? User?.FindFirst("sub")?.Value;
        }
    }
    
    /// <summary>
    /// Request model for minting stablecoin
    /// </summary>
    public class MintStablecoinRequest
    {
        public decimal ZecAmount { get; set; }
        public decimal StablecoinAmount { get; set; }
        public string AztecAddress { get; set; }
        public string ZcashAddress { get; set; }
        public bool GenerateViewingKey { get; set; } = true;
    }
    
    /// <summary>
    /// Request model for redeeming stablecoin
    /// </summary>
    public class RedeemStablecoinRequest
    {
        public string PositionId { get; set; }
        public decimal StablecoinAmount { get; set; }
        public string ZcashAddress { get; set; }
    }
}

