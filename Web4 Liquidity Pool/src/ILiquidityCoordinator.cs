// Web4 Liquidity Pool — Liquidity Coordinator contract
// Target chains: Solana, Base. Implement in OASIS Core or ONODE.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Web4LiquidityPool
{
    /// <summary>
    /// Coordinates add/remove/swap and pool state for Web4 Liquidity Pools.
    /// Updates Pool State Holon via HolonManager; triggers value movement via bridge/providers (Solana, Base).
    /// </summary>
    public interface ILiquidityCoordinator
    {
        /// <summary>Get current pool state (reserves by chain, LP supply, etc.).</summary>
        Task<PoolStateResult> GetPoolStateAsync(Guid poolId, CancellationToken cancellationToken = default);

        /// <summary>Add liquidity on the given chain. Phase 1a: chainId = "solana"; Phase 1b: "base" as well.</summary>
        Task<AddLiquidityResult> AddLiquidityAsync(AddLiquidityRequest request, CancellationToken cancellationToken = default);

        /// <summary>Remove liquidity; user chooses payout chain (solana | base).</summary>
        Task<RemoveLiquidityResult> RemoveLiquidityAsync(RemoveLiquidityRequest request, CancellationToken cancellationToken = default);

        /// <summary>Swap on the given chain. Same-chain only in Phase 1; cross-chain in Phase 2.</summary>
        Task<SwapResult> SwapAsync(SwapRequest request, CancellationToken cancellationToken = default);
    }

    public class AddLiquidityRequest
    {
        public Guid PoolId { get; set; }
        public Guid AvatarId { get; set; }
        public string ChainId { get; set; }  // "solana" | "base"
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
    }

    public class AddLiquidityResult
    {
        public bool Success { get; set; }
        public string LpTokensMinted { get; set; }
        public string Message { get; set; }
    }

    public class RemoveLiquidityRequest
    {
        public Guid PoolId { get; set; }
        public Guid AvatarId { get; set; }
        public string LpAmount { get; set; }
        public string PreferredChainId { get; set; }  // "solana" | "base"
    }

    public class RemoveLiquidityResult
    {
        public bool Success { get; set; }
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
        public string Message { get; set; }
    }

    public class SwapRequest
    {
        public Guid PoolId { get; set; }
        public Guid AvatarId { get; set; }
        public string ChainId { get; set; }  // "solana" | "base"
        public string TokenIn { get; set; }   // token0 | token1
        public string AmountIn { get; set; }
        public string TokenOut { get; set; }
        public string MinAmountOut { get; set; }
    }

    public class SwapResult
    {
        public bool Success { get; set; }
        public string AmountOut { get; set; }
        public string Message { get; set; }
    }

    public class PoolStateResult
    {
        public bool Success { get; set; }
        public Guid PoolId { get; set; }
        public string Token0 { get; set; }
        public string Token1 { get; set; }
        public string TotalLpSupply { get; set; }
        public Dictionary<string, ReserveAmounts> ReservesByChain { get; set; }
        public string Message { get; set; }
    }

    public class ReserveAmounts
    {
        public string Token0 { get; set; }
        public string Token1 { get; set; }
    }
}
