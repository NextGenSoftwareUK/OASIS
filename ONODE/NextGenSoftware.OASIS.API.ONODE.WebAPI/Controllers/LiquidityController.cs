using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers.Liquidity;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>
/// Controller for Web4 Liquidity Pool operations.
/// Phase 1a: Solana-only pools; add/remove/swap update pool state holon via LiquidityManager.
/// </summary>
[ApiController]
[Route("api/v1/liquidity")]
public class LiquidityController : OASISControllerBase
{
    private LiquidityManager _liquidityManager;
    private readonly ILogger<LiquidityController> _logger;

    private LiquidityManager LiquidityManager =>
        _liquidityManager ??= LiquidityManager.Instance;

    public LiquidityController(ILogger<LiquidityController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Create a new liquidity pool (admin). Phase 1a: initial chain = solana.</summary>
    [HttpPost("pools")]
    [ProducesResponseType(typeof(CreatePoolResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePool(
        [FromBody] CreatePoolRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating liquidity pool: {Token0}/{Token1}", request?.Token0, request?.Token1);
            var result = await LiquidityManager.CreatePoolAsync(request ?? new CreatePoolRequest(), cancellationToken);
            if (!result.Success)
            {
                _logger.LogWarning("Create pool failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message, isError = true });
            }
            _logger.LogInformation("Pool created: {PoolId}", result.PoolId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreatePool");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>Get pool state (reserves by chain, TVL, LP supply).</summary>
    [HttpGet("pools/{poolId:guid}")]
    [ProducesResponseType(typeof(PoolStateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPoolState(
        [FromRoute] Guid poolId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await LiquidityManager.GetPoolStateAsync(poolId, cancellationToken);
            if (!result.Success)
            {
                _logger.LogWarning("Get pool state failed: {Message}", result.Message);
                return NotFound(new { error = result.Message, isError = true });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetPoolState");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>Add liquidity. Phase 1a: chainId = solana.</summary>
    [HttpPost("pools/{poolId:guid}/add")]
    [ProducesResponseType(typeof(AddLiquidityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddLiquidity(
        [FromRoute] Guid poolId,
        [FromBody] AddLiquidityRequest body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (body == null) body = new AddLiquidityRequest();
            body.PoolId = poolId;
            _logger.LogInformation("Add liquidity to pool {PoolId}, chain {ChainId}", poolId, body.ChainId);
            var result = await LiquidityManager.AddLiquidityAsync(body, cancellationToken);
            if (!result.Success)
            {
                _logger.LogWarning("Add liquidity failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message, isError = true });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in AddLiquidity");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>Remove liquidity; preferredChainId = solana | base.</summary>
    [HttpPost("pools/{poolId:guid}/remove")]
    [ProducesResponseType(typeof(RemoveLiquidityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveLiquidity(
        [FromRoute] Guid poolId,
        [FromBody] RemoveLiquidityRequest body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (body == null) body = new RemoveLiquidityRequest();
            body.PoolId = poolId;
            _logger.LogInformation("Remove liquidity from pool {PoolId}, lpAmount {LpAmount}", poolId, body.LpAmount);
            var result = await LiquidityManager.RemoveLiquidityAsync(body, cancellationToken);
            if (!result.Success)
            {
                _logger.LogWarning("Remove liquidity failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message, isError = true });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in RemoveLiquidity");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>Swap on the given chain. Same-chain only in Phase 1.</summary>
    [HttpPost("pools/{poolId:guid}/swap")]
    [ProducesResponseType(typeof(SwapResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Swap(
        [FromRoute] Guid poolId,
        [FromBody] SwapRequest body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (body == null) body = new SwapRequest();
            body.PoolId = poolId;
            _logger.LogInformation("Swap on pool {PoolId}, chain {ChainId}, {TokenIn} -> {TokenOut}", poolId, body.ChainId, body.TokenIn, body.TokenOut);
            var result = await LiquidityManager.SwapAsync(body, cancellationToken);
            if (!result.Success)
            {
                _logger.LogWarning("Swap failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message, isError = true });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Swap");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }
}
