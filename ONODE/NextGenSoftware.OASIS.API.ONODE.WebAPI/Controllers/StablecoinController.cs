using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>
/// Controller for Zcash-backed stablecoin (zUSD) operations.
/// Handles minting, redeeming, position management, and liquidation.
/// </summary>
[ApiController]
[Route("api/v1/stablecoin")]
[Authorize]
public class StablecoinController : OASISControllerBase
{
    private readonly StablecoinManager _stablecoinManager;
    private readonly ILogger<StablecoinController> _logger;

    public StablecoinController(
        StablecoinManager stablecoinManager,
        ILogger<StablecoinController> logger)
    {
        _stablecoinManager = stablecoinManager ?? throw new ArgumentNullException(nameof(stablecoinManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Mints stablecoin (zUSD) by locking ZEC collateral
    /// </summary>
    /// <param name="request">Mint request with ZEC amount, zUSD amount, and addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created position</returns>
    [HttpPost("mint")]
    [ProducesResponseType(typeof(StablecoinPosition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Mint(
        [FromBody] MintStablecoinRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            request.AvatarId = AvatarId;

            _logger.LogInformation("Minting stablecoin: {ZecAmount} ZEC → {StablecoinAmount} zUSD for avatar {AvatarId}",
                request.ZecAmount, request.StablecoinAmount, AvatarId);

            var result = await _stablecoinManager.MintStablecoinAsync(request, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Stablecoin mint failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Stablecoin minted successfully: Position {PositionId}", result.Result.PositionId);
            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Mint");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Redeems stablecoin (zUSD) to unlock ZEC collateral
    /// </summary>
    /// <param name="request">Redeem request with position ID and zUSD amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Redemption result message</returns>
    [HttpPost("redeem")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Redeem(
        [FromBody] RedeemStablecoinRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            request.AvatarId = AvatarId;

            _logger.LogInformation("Redeeming stablecoin: {StablecoinAmount} zUSD from position {PositionId} for avatar {AvatarId}",
                request.StablecoinAmount, request.PositionId, AvatarId);

            var result = await _stablecoinManager.RedeemStablecoinAsync(request, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Stablecoin redeem failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Stablecoin redeemed successfully: {Message}", result.Result);
            return Ok(new { message = result.Result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Redeem");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a position by ID
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Position details</returns>
    [HttpGet("position/{positionId:guid}")]
    [ProducesResponseType(typeof(StablecoinPosition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPosition(
        [FromRoute] Guid positionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            _logger.LogInformation("Getting position {PositionId} for avatar {AvatarId}", positionId, AvatarId);

            var result = await _stablecoinManager.GetPositionAsync(positionId, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Position not found: {PositionId}", positionId);
                return NotFound(new { error = result.Message });
            }

            // Verify ownership
            if (result.Result.AvatarId != AvatarId)
            {
                return Forbid("Position does not belong to this avatar");
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetPosition");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets position health status
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Position health information</returns>
    [HttpGet("position/{positionId:guid}/health")]
    [ProducesResponseType(typeof(PositionHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPositionHealth(
        [FromRoute] Guid positionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            _logger.LogInformation("Getting position health for {PositionId}", positionId);

            var result = await _stablecoinManager.GetPositionHealthAsync(positionId, cancellationToken);

            if (result.IsError)
            {
                return NotFound(new { error = result.Message });
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetPositionHealth");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all positions for the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of positions</returns>
    [HttpGet("positions")]
    [ProducesResponseType(typeof(StablecoinPosition[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPositions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            _logger.LogInformation("Getting positions for avatar {AvatarId}", AvatarId);

            var result = await _stablecoinManager.GetPositionsAsync(AvatarId, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Failed to get positions: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            return Ok(result.Result ?? new List<StablecoinPosition>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetPositions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets system-wide status and metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System status</returns>
    [HttpGet("system")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SystemStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system status");

            var result = await _stablecoinManager.GetSystemStatusAsync(cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Failed to get system status: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetSystemStatus");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Liquidates an undercollateralized position
    /// </summary>
    /// <param name="positionId">Position ID to liquidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Liquidation result message</returns>
    [HttpPost("liquidate/{positionId:guid}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Liquidate(
        [FromRoute] Guid positionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            _logger.LogInformation("Liquidating position {PositionId} for avatar {AvatarId}", positionId, AvatarId);

            var result = await _stablecoinManager.LiquidatePositionAsync(positionId, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Liquidation failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Position liquidated successfully: {Message}", result.Result);
            return Ok(new { message = result.Result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Liquidate");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generates yield for a position
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Yield amount generated</returns>
    [HttpPost("yield/{positionId:guid}")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateYield(
        [FromRoute] Guid positionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (AvatarId == Guid.Empty)
            {
                return Unauthorized(new { error = "Avatar not authenticated" });
            }

            _logger.LogInformation("Generating yield for position {PositionId}", positionId);

            var result = await _stablecoinManager.GenerateYieldAsync(positionId, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Yield generation failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Yield generated: {YieldAmount} ZEC", result.Result);
            return Ok(new { yieldAmount = result.Result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GenerateYield");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

