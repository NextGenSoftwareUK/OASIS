# HyperDrive Liquidity Pools - Smart Contract Architecture

## Overview

To make HyperDrive Liquidity Pools fully functional, we need a sophisticated multi-chain smart contract architecture that enables:
1. Unified liquidity pools across 10+ chains
2. Cross-chain state synchronization
3. Fee aggregation from all chains
4. Atomic add/remove liquidity operations
5. Real-time consensus and conflict resolution

---

## Core Smart Contract Architecture

### 1. **HyperDrivePool Contract** (Per Chain)

Each chain needs its own `HyperDrivePool` contract that manages the local pool state while staying synchronized with all other chains.

#### **EVM Chains (Ethereum, Polygon, Base, Arbitrum, Optimism, BNB, Avalanche, Fantom)**

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

/**
 * @title HyperDrivePool
 * @dev Unified AMM pool that syncs state across all chains via HyperDrive
 */
contract HyperDrivePool is ReentrancyGuard, Ownable {
    // ============ State Variables ============
    
    IERC20 public token0;
    IERC20 public token1;
    
    uint256 public reserve0;
    uint256 public reserve1;
    uint256 public totalLPTokens;
    
    mapping(address => uint256) public lpTokenBalances;
    mapping(address => mapping(uint256 => uint256)) public chainSpecificFees; // user => chainId => fees
    
    // HyperDrive sync
    address public hyperDriveOracle; // Trusted oracle for cross-chain messages
    mapping(uint256 => PoolState) public otherChainStates; // chainId => state
    uint256 public lastSyncTimestamp;
    
    struct PoolState {
        uint256 reserve0;
        uint256 reserve1;
        uint256 totalLPTokens;
        uint256 timestamp;
        bytes32 stateHash;
    }
    
    // Events for HyperDrive to listen to
    event LiquidityAdded(address indexed provider, uint256 amount0, uint256 amount1, uint256 lpTokens);
    event LiquidityRemoved(address indexed provider, uint256 lpTokens, uint256 amount0, uint256 amount1);
    event Swap(address indexed trader, address tokenIn, address tokenOut, uint256 amountIn, uint256 amountOut);
    event CrossChainSync(uint256 indexed chainId, bytes32 stateHash);
    event FeesClaimed(address indexed user, uint256 amount);
    
    // ============ Constructor ============
    
    constructor(address _token0, address _token1, address _hyperDriveOracle) {
        token0 = IERC20(_token0);
        token1 = IERC20(_token1);
        hyperDriveOracle = _hyperDriveOracle;
    }
    
    // ============ Core AMM Functions ============
    
    /**
     * @dev Add liquidity to the pool (local + cross-chain)
     * @param amount0 Amount of token0 to add
     * @param amount1 Amount of token1 to add
     * @return lpTokens Amount of LP tokens minted
     */
    function addLiquidity(
        uint256 amount0,
        uint256 amount1
    ) external nonReentrant returns (uint256 lpTokens) {
        require(amount0 > 0 && amount1 > 0, "Amounts must be > 0");
        
        // Transfer tokens from user
        token0.transferFrom(msg.sender, address(this), amount0);
        token1.transferFrom(msg.sender, address(this), amount1);
        
        // Calculate LP tokens to mint
        if (totalLPTokens == 0) {
            // First liquidity provider
            lpTokens = sqrt(amount0 * amount1);
        } else {
            // Subsequent providers (proportional to existing ratio)
            lpTokens = min(
                (amount0 * totalLPTokens) / reserve0,
                (amount1 * totalLPTokens) / reserve1
            );
        }
        
        require(lpTokens > 0, "Insufficient liquidity minted");
        
        // Update state
        reserve0 += amount0;
        reserve1 += amount1;
        totalLPTokens += lpTokens;
        lpTokenBalances[msg.sender] += lpTokens;
        
        // Emit event for HyperDrive to sync with other chains
        emit LiquidityAdded(msg.sender, amount0, amount1, lpTokens);
        
        return lpTokens;
    }
    
    /**
     * @dev Remove liquidity from the pool
     * @param lpTokens Amount of LP tokens to burn
     * @return amount0 Amount of token0 returned
     * @return amount1 Amount of token1 returned
     */
    function removeLiquidity(
        uint256 lpTokens
    ) external nonReentrant returns (uint256 amount0, uint256 amount1) {
        require(lpTokens > 0, "LP tokens must be > 0");
        require(lpTokenBalances[msg.sender] >= lpTokens, "Insufficient LP balance");
        
        // Calculate tokens to return (proportional)
        amount0 = (lpTokens * reserve0) / totalLPTokens;
        amount1 = (lpTokens * reserve1) / totalLPTokens;
        
        require(amount0 > 0 && amount1 > 0, "Insufficient liquidity burned");
        
        // Update state
        reserve0 -= amount0;
        reserve1 -= amount1;
        totalLPTokens -= lpTokens;
        lpTokenBalances[msg.sender] -= lpTokens;
        
        // Transfer tokens to user
        token0.transfer(msg.sender, amount0);
        token1.transfer(msg.sender, amount1);
        
        // Claim aggregated fees from all chains
        uint256 aggregatedFees = _claimCrossChainFees(msg.sender);
        
        // Emit event for HyperDrive
        emit LiquidityRemoved(msg.sender, lpTokens, amount0, amount1);
        
        return (amount0, amount1);
    }
    
    /**
     * @dev Swap tokens using constant product formula (x * y = k)
     * @param tokenIn Address of token to swap in
     * @param amountIn Amount of tokenIn
     * @param minAmountOut Minimum amount of tokenOut (slippage protection)
     * @return amountOut Amount of tokenOut received
     */
    function swap(
        address tokenIn,
        uint256 amountIn,
        uint256 minAmountOut
    ) external nonReentrant returns (uint256 amountOut) {
        require(amountIn > 0, "Amount must be > 0");
        require(tokenIn == address(token0) || tokenIn == address(token1), "Invalid token");
        
        bool isToken0 = tokenIn == address(token0);
        IERC20 inputToken = isToken0 ? token0 : token1;
        IERC20 outputToken = isToken0 ? token1 : token0;
        
        uint256 reserveIn = isToken0 ? reserve0 : reserve1;
        uint256 reserveOut = isToken0 ? reserve1 : reserve0;
        
        // Transfer input tokens
        inputToken.transferFrom(msg.sender, address(this), amountIn);
        
        // Calculate output amount (with 0.3% fee)
        uint256 amountInWithFee = amountIn * 997; // 0.3% fee (1000 - 3)
        amountOut = (amountInWithFee * reserveOut) / ((reserveIn * 1000) + amountInWithFee);
        
        require(amountOut >= minAmountOut, "Slippage exceeded");
        require(amountOut < reserveOut, "Insufficient liquidity");
        
        // Update reserves
        if (isToken0) {
            reserve0 += amountIn;
            reserve1 -= amountOut;
        } else {
            reserve1 += amountIn;
            reserve0 -= amountOut;
        }
        
        // Transfer output tokens
        outputToken.transfer(msg.sender, amountOut);
        
        // Distribute fees to LPs (0.3% stays in pool, increases K)
        uint256 fee = (amountIn * 3) / 1000;
        _distributeFees(fee, isToken0);
        
        // Emit event for HyperDrive to sync
        emit Swap(msg.sender, tokenIn, address(outputToken), amountIn, amountOut);
        
        return amountOut;
    }
    
    // ============ HyperDrive Sync Functions ============
    
    /**
     * @dev Called by HyperDrive Oracle to update pool state from another chain
     * @param chainId Chain ID of the source chain
     * @param state Pool state from that chain
     * @param signature Cryptographic proof from HyperDrive
     */
    function syncFromChain(
        uint256 chainId,
        PoolState memory state,
        bytes memory signature
    ) external {
        require(msg.sender == hyperDriveOracle, "Only oracle");
        require(_verifySignature(state, signature), "Invalid signature");
        require(state.timestamp > otherChainStates[chainId].timestamp, "Outdated state");
        
        // Store state from other chain
        otherChainStates[chainId] = state;
        lastSyncTimestamp = block.timestamp;
        
        emit CrossChainSync(chainId, state.stateHash);
    }
    
    /**
     * @dev Get current pool state hash for HyperDrive consensus
     */
    function getStateHash() public view returns (bytes32) {
        return keccak256(abi.encodePacked(
            reserve0,
            reserve1,
            totalLPTokens,
            block.timestamp
        ));
    }
    
    /**
     * @dev Record fees earned from another chain (called by oracle)
     * @param user Address of the LP
     * @param chainId Source chain
     * @param amount Fee amount
     */
    function recordCrossChainFee(
        address user,
        uint256 chainId,
        uint256 amount
    ) external {
        require(msg.sender == hyperDriveOracle, "Only oracle");
        chainSpecificFees[user][chainId] += amount;
    }
    
    /**
     * @dev Claim all fees from all chains (called during liquidity removal)
     */
    function _claimCrossChainFees(address user) internal returns (uint256 totalFees) {
        // This would aggregate fees from all chain IDs
        // In practice, HyperDrive oracle would provide the total
        uint256[] memory chainIds = _getActiveChainIds();
        
        for (uint256 i = 0; i < chainIds.length; i++) {
            uint256 chainId = chainIds[i];
            uint256 fees = chainSpecificFees[user][chainId];
            if (fees > 0) {
                totalFees += fees;
                chainSpecificFees[user][chainId] = 0;
            }
        }
        
        if (totalFees > 0) {
            // Transfer fees (assuming fees are in token0 for simplicity)
            token0.transfer(user, totalFees);
            emit FeesClaimed(user, totalFees);
        }
        
        return totalFees;
    }
    
    // ============ Internal Helper Functions ============
    
    function _distributeFees(uint256 fee, bool isToken0) internal {
        // Fee stays in pool, increasing K (automatic compounding)
        // Pro-rata distribution to all LPs happens via increased reserve ratio
    }
    
    function _verifySignature(
        PoolState memory state,
        bytes memory signature
    ) internal view returns (bool) {
        // Verify that HyperDrive Oracle signed this state update
        bytes32 messageHash = keccak256(abi.encode(state));
        // Implementation would use ECDSA recovery
        return true; // Placeholder
    }
    
    function _getActiveChainIds() internal pure returns (uint256[] memory) {
        uint256[] memory chainIds = new uint256[](10);
        chainIds[0] = 1;     // Ethereum
        chainIds[1] = 137;   // Polygon
        chainIds[2] = 8453;  // Base
        chainIds[3] = 42161; // Arbitrum
        chainIds[4] = 10;    // Optimism
        chainIds[5] = 56;    // BNB
        chainIds[6] = 43114; // Avalanche
        chainIds[7] = 250;   // Fantom
        chainIds[8] = 1151111081099710; // Solana (custom)
        chainIds[9] = 1010;  // Radix (custom)
        return chainIds;
    }
    
    function sqrt(uint256 y) internal pure returns (uint256 z) {
        if (y > 3) {
            z = y;
            uint256 x = y / 2 + 1;
            while (x < z) {
                z = x;
                x = (y / x + x) / 2;
            }
        } else if (y != 0) {
            z = 1;
        }
    }
    
    function min(uint256 a, uint256 b) internal pure returns (uint256) {
        return a < b ? a : b;
    }
}
```

---

### 2. **Solana Program** (Rust)

Solana needs a native Rust program for the same functionality:

```rust
use anchor_lang::prelude::*;
use anchor_spl::token::{self, Token, TokenAccount, Transfer};

declare_id!("HyperDrive11111111111111111111111111111111");

#[program]
pub mod hyperdrive_pool {
    use super::*;

    /// Initialize a new unified pool
    pub fn initialize_pool(
        ctx: Context<InitializePool>,
        token0_mint: Pubkey,
        token1_mint: Pubkey,
    ) -> Result<()> {
        let pool = &mut ctx.accounts.pool;
        pool.token0_mint = token0_mint;
        pool.token1_mint = token1_mint;
        pool.reserve0 = 0;
        pool.reserve1 = 0;
        pool.total_lp_tokens = 0;
        pool.hyperdrive_authority = ctx.accounts.authority.key();
        pool.last_sync_timestamp = Clock::get()?.unix_timestamp;
        Ok(())
    }

    /// Add liquidity to the pool
    pub fn add_liquidity(
        ctx: Context<AddLiquidity>,
        amount0: u64,
        amount1: u64,
    ) -> Result<()> {
        let pool = &mut ctx.accounts.pool;
        
        // Calculate LP tokens
        let lp_tokens = if pool.total_lp_tokens == 0 {
            // First LP
            ((amount0 as u128 * amount1 as u128) as f64).sqrt() as u64
        } else {
            // Proportional
            std::cmp::min(
                (amount0 * pool.total_lp_tokens) / pool.reserve0,
                (amount1 * pool.total_lp_tokens) / pool.reserve1,
            )
        };
        
        require!(lp_tokens > 0, ErrorCode::InsufficientLiquidity);
        
        // Transfer tokens from user
        token::transfer(
            CpiContext::new(
                ctx.accounts.token_program.to_account_info(),
                Transfer {
                    from: ctx.accounts.user_token0.to_account_info(),
                    to: ctx.accounts.pool_token0.to_account_info(),
                    authority: ctx.accounts.user.to_account_info(),
                },
            ),
            amount0,
        )?;
        
        token::transfer(
            CpiContext::new(
                ctx.accounts.token_program.to_account_info(),
                Transfer {
                    from: ctx.accounts.user_token1.to_account_info(),
                    to: ctx.accounts.pool_token1.to_account_info(),
                    authority: ctx.accounts.user.to_account_info(),
                },
            ),
            amount1,
        )?;
        
        // Update state
        pool.reserve0 += amount0;
        pool.reserve1 += amount1;
        pool.total_lp_tokens += lp_tokens;
        
        // Update user LP balance
        let user_position = &mut ctx.accounts.user_position;
        user_position.lp_tokens += lp_tokens;
        user_position.owner = ctx.accounts.user.key();
        
        // Emit event for HyperDrive
        emit!(LiquidityAddedEvent {
            user: ctx.accounts.user.key(),
            amount0,
            amount1,
            lp_tokens,
        });
        
        Ok(())
    }

    /// Swap tokens
    pub fn swap(
        ctx: Context<Swap>,
        amount_in: u64,
        min_amount_out: u64,
        is_token0: bool,
    ) -> Result<()> {
        let pool = &mut ctx.accounts.pool;
        
        let (reserve_in, reserve_out) = if is_token0 {
            (pool.reserve0, pool.reserve1)
        } else {
            (pool.reserve1, pool.reserve0)
        };
        
        // Calculate output with 0.3% fee
        let amount_in_with_fee = (amount_in as u128 * 997) / 1000;
        let amount_out = (amount_in_with_fee * reserve_out as u128) 
            / (reserve_in as u128 * 1000 + amount_in_with_fee);
        let amount_out = amount_out as u64;
        
        require!(amount_out >= min_amount_out, ErrorCode::SlippageExceeded);
        require!(amount_out < reserve_out, ErrorCode::InsufficientLiquidity);
        
        // Transfer tokens
        if is_token0 {
            // Transfer token0 from user
            token::transfer(
                CpiContext::new(
                    ctx.accounts.token_program.to_account_info(),
                    Transfer {
                        from: ctx.accounts.user_token_in.to_account_info(),
                        to: ctx.accounts.pool_token0.to_account_info(),
                        authority: ctx.accounts.user.to_account_info(),
                    },
                ),
                amount_in,
            )?;
            
            // Transfer token1 to user
            token::transfer(
                CpiContext::new_with_signer(
                    ctx.accounts.token_program.to_account_info(),
                    Transfer {
                        from: ctx.accounts.pool_token1.to_account_info(),
                        to: ctx.accounts.user_token_out.to_account_info(),
                        authority: pool.to_account_info(),
                    },
                    &[&[b"pool", &[ctx.bumps.pool]]],
                ),
                amount_out,
            )?;
            
            pool.reserve0 += amount_in;
            pool.reserve1 -= amount_out;
        } else {
            // Similar logic for token1 -> token0
        }
        
        // Emit event for HyperDrive
        emit!(SwapEvent {
            user: ctx.accounts.user.key(),
            amount_in,
            amount_out,
            is_token0,
        });
        
        Ok(())
    }

    /// Sync state from another chain (called by HyperDrive oracle)
    pub fn sync_from_chain(
        ctx: Context<SyncFromChain>,
        chain_id: u64,
        reserve0: u64,
        reserve1: u64,
        total_lp_tokens: u64,
        timestamp: i64,
    ) -> Result<()> {
        require!(
            ctx.accounts.authority.key() == ctx.accounts.pool.hyperdrive_authority,
            ErrorCode::Unauthorized
        );
        
        let pool = &mut ctx.accounts.pool;
        pool.last_sync_timestamp = timestamp;
        
        // Store other chain state for consensus
        // (In practice, would use a PDA to store per-chain state)
        
        emit!(CrossChainSyncEvent {
            chain_id,
            reserve0,
            reserve1,
            total_lp_tokens,
            timestamp,
        });
        
        Ok(())
    }
}

// ============ Account Structures ============

#[account]
pub struct Pool {
    pub token0_mint: Pubkey,
    pub token1_mint: Pubkey,
    pub reserve0: u64,
    pub reserve1: u64,
    pub total_lp_tokens: u64,
    pub hyperdrive_authority: Pubkey,
    pub last_sync_timestamp: i64,
}

#[account]
pub struct UserPosition {
    pub owner: Pubkey,
    pub lp_tokens: u64,
    pub fees_earned: u64,
}

// ============ Context Structs ============

#[derive(Accounts)]
pub struct InitializePool<'info> {
    #[account(init, payer = authority, space = 8 + 200)]
    pub pool: Account<'info, Pool>,
    #[account(mut)]
    pub authority: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct AddLiquidity<'info> {
    #[account(mut)]
    pub pool: Account<'info, Pool>,
    #[account(mut)]
    pub user: Signer<'info>,
    #[account(mut)]
    pub user_token0: Account<'info, TokenAccount>,
    #[account(mut)]
    pub user_token1: Account<'info, TokenAccount>,
    #[account(mut)]
    pub pool_token0: Account<'info, TokenAccount>,
    #[account(mut)]
    pub pool_token1: Account<'info, TokenAccount>,
    #[account(init_if_needed, payer = user, space = 8 + 100, seeds = [b"position", user.key().as_ref()], bump)]
    pub user_position: Account<'info, UserPosition>,
    pub token_program: Program<'info, Token>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct Swap<'info> {
    #[account(mut, seeds = [b"pool"], bump)]
    pub pool: Account<'info, Pool>,
    #[account(mut)]
    pub user: Signer<'info>,
    #[account(mut)]
    pub user_token_in: Account<'info, TokenAccount>,
    #[account(mut)]
    pub user_token_out: Account<'info, TokenAccount>,
    #[account(mut)]
    pub pool_token0: Account<'info, TokenAccount>,
    #[account(mut)]
    pub pool_token1: Account<'info, TokenAccount>,
    pub token_program: Program<'info, Token>,
}

#[derive(Accounts)]
pub struct SyncFromChain<'info> {
    #[account(mut)]
    pub pool: Account<'info, Pool>,
    pub authority: Signer<'info>,
}

// ============ Events ============

#[event]
pub struct LiquidityAddedEvent {
    pub user: Pubkey,
    pub amount0: u64,
    pub amount1: u64,
    pub lp_tokens: u64,
}

#[event]
pub struct SwapEvent {
    pub user: Pubkey,
    pub amount_in: u64,
    pub amount_out: u64,
    pub is_token0: bool,
}

#[event]
pub struct CrossChainSyncEvent {
    pub chain_id: u64,
    pub reserve0: u64,
    pub reserve1: u64,
    pub total_lp_tokens: u64,
    pub timestamp: i64,
}

// ============ Error Codes ============

#[error_code]
pub enum ErrorCode {
    #[msg("Insufficient liquidity")]
    InsufficientLiquidity,
    #[msg("Slippage tolerance exceeded")]
    SlippageExceeded,
    #[msg("Unauthorized")]
    Unauthorized,
}
```

---

## 3. **HyperDrive Orchestration Layer** (Backend)

The .NET backend needs to orchestrate cross-chain synchronization:

```csharp
namespace OASIS.API.Core.Managers.HyperDrive
{
    /// <summary>
    /// Manages unified liquidity pools across all chains
    /// </summary>
    public class HyperDriveLiquidityManager
    {
        private readonly Dictionary<ProviderType, IOASISProvider> _providers;
        private readonly ILogger<HyperDriveLiquidityManager> _logger;
        private readonly IEventAggregator _eventAggregator;
        
        // Pool state cache
        private readonly ConcurrentDictionary<string, UnifiedPoolState> _poolStates;
        
        public HyperDriveLiquidityManager(
            IEnumerable<IOASISProvider> providers,
            ILogger<HyperDriveLiquidityManager> logger,
            IEventAggregator eventAggregator)
        {
            _providers = providers.ToDictionary(p => p.ProviderType);
            _logger = logger;
            _eventAggregator = eventAggregator;
            _poolStates = new ConcurrentDictionary<string, UnifiedPoolState>();
            
            // Subscribe to blockchain events
            SubscribeToPoolEvents();
        }
        
        /// <summary>
        /// Add liquidity to unified pool (triggers on all chains)
        /// </summary>
        public async Task<OASISResult<LiquidityPosition>> AddLiquidityAsync(
            string poolId,
            ProviderType deploymentChain,
            string userAddress,
            decimal amount0,
            decimal amount1)
        {
            try
            {
                // 1. Add liquidity on deployment chain
                var primaryProvider = _providers[deploymentChain];
                var addLiquidityResult = await primaryProvider.BridgeService.AddLiquidityAsync(
                    poolId, userAddress, amount0, amount1);
                
                if (!addLiquidityResult.IsSuccess)
                    return OASISResult<LiquidityPosition>.Fail(addLiquidityResult.Message);
                
                var lpTokens = addLiquidityResult.Result;
                
                // 2. Sync pool state to all other chains
                var syncTasks = _providers
                    .Where(p => p.Key != deploymentChain)
                    .Select(p => SyncPoolStateAsync(poolId, p.Value, deploymentChain));
                
                await Task.WhenAll(syncTasks);
                
                // 3. Register user position across all chains
                var position = new LiquidityPosition
                {
                    PoolId = poolId,
                    UserAddress = userAddress,
                    DeploymentChain = deploymentChain,
                    LPTokens = lpTokens,
                    Amount0 = amount0,
                    Amount1 = amount1,
                    Timestamp = DateTime.UtcNow,
                    EarningFromChains = _providers.Keys.ToList()
                };
                
                await RegisterPositionAcrossChainsAsync(position);
                
                _logger.LogInformation(
                    "Added liquidity: {PoolId} on {Chain}, LP tokens: {Tokens}",
                    poolId, deploymentChain, lpTokens);
                
                return OASISResult<LiquidityPosition>.Success(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding liquidity to pool {PoolId}", poolId);
                return OASISResult<LiquidityPosition>.Fail($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle swap event from any chain and sync to all others
        /// </summary>
        public async Task HandleSwapEventAsync(SwapEvent swapEvent)
        {
            try
            {
                var poolId = swapEvent.PoolId;
                var sourceChain = swapEvent.SourceChain;
                
                // 1. Get updated pool state from source chain
                var sourceProvider = _providers[sourceChain];
                var poolState = await sourceProvider.BridgeService.GetPoolStateAsync(poolId);
                
                // 2. Calculate fee distribution
                var fee = swapEvent.AmountIn * 0.003m; // 0.3% fee
                await DistributeFeesToLPs(poolId, fee, sourceChain);
                
                // 3. Sync updated reserves to all other chains
                var syncTasks = _providers
                    .Where(p => p.Key != sourceChain)
                    .Select(p => SyncPoolStateAsync(poolId, p.Value, sourceChain, poolState));
                
                await Task.WhenAll(syncTasks);
                
                // 4. Update pool state cache
                _poolStates.AddOrUpdate(poolId, poolState, (key, old) => poolState);
                
                _logger.LogInformation(
                    "Swap on {Chain}: {AmountIn} {TokenIn} -> {AmountOut} {TokenOut}, Fee: {Fee}",
                    sourceChain, swapEvent.AmountIn, swapEvent.TokenIn,
                    swapEvent.AmountOut, swapEvent.TokenOut, fee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling swap event");
            }
        }
        
        /// <summary>
        /// Remove liquidity and aggregate fees from all chains
        /// </summary>
        public async Task<OASISResult<RemoveLiquidityResult>> RemoveLiquidityAsync(
            string poolId,
            string userAddress,
            decimal lpTokens)
        {
            try
            {
                // 1. Get user position to know deployment chain
                var position = await GetUserPositionAsync(poolId, userAddress);
                if (position == null)
                    return OASISResult<RemoveLiquidityResult>.Fail("Position not found");
                
                // 2. Aggregate fees from ALL chains
                var totalFees = await AggregateCrossChainFeesAsync(poolId, userAddress);
                
                // 3. Remove liquidity on deployment chain
                var provider = _providers[position.DeploymentChain];
                var removeResult = await provider.BridgeService.RemoveLiquidityAsync(
                    poolId, userAddress, lpTokens);
                
                if (!removeResult.IsSuccess)
                    return OASISResult<RemoveLiquidityResult>.Fail(removeResult.Message);
                
                // 4. Transfer aggregated fees from other chains
                var transferTasks = totalFees
                    .Where(f => f.ChainId != position.DeploymentChain)
                    .Select(f => TransferFeesToDeploymentChainAsync(
                        f.ChainId, position.DeploymentChain, userAddress, f.Amount));
                
                await Task.WhenAll(transferTasks);
                
                // 5. Sync removal to all chains
                await SyncLiquidityRemovalAsync(poolId, userAddress, lpTokens);
                
                var result = new RemoveLiquidityResult
                {
                    Amount0 = removeResult.Result.Amount0,
                    Amount1 = removeResult.Result.Amount1,
                    TotalFeesFromAllChains = totalFees.Sum(f => f.Amount),
                    FeesBreakdown = totalFees
                };
                
                _logger.LogInformation(
                    "Removed liquidity: {PoolId}, LP: {Tokens}, Total fees: {Fees}",
                    poolId, lpTokens, result.TotalFeesFromAllChains);
                
                return OASISResult<RemoveLiquidityResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing liquidity");
                return OASISResult<RemoveLiquidityResult>.Fail($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sync pool state from source chain to target chain
        /// </summary>
        private async Task SyncPoolStateAsync(
            string poolId,
            IOASISProvider targetProvider,
            ProviderType sourceChain,
            UnifiedPoolState? state = null)
        {
            try
            {
                // Get latest state if not provided
                if (state == null)
                {
                    var sourceProvider = _providers[sourceChain];
                    state = await sourceProvider.BridgeService.GetPoolStateAsync(poolId);
                }
                
                // Generate state hash and signature
                var stateHash = GenerateStateHash(state);
                var signature = await SignStateAsync(state);
                
                // Call syncFromChain on target chain contract
                await targetProvider.BridgeService.SyncPoolFromChainAsync(
                    poolId, (int)sourceChain, state, signature);
                
                _logger.LogDebug(
                    "Synced pool {PoolId} from {SourceChain} to {TargetChain}",
                    poolId, sourceChain, targetProvider.ProviderType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing pool state");
            }
        }
        
        /// <summary>
        /// Aggregate fees earned from all chains for a user
        /// </summary>
        private async Task<List<ChainFee>> AggregateCrossChainFeesAsync(
            string poolId,
            string userAddress)
        {
            var feeTasks = _providers.Values.Select(async provider =>
            {
                try
                {
                    var fees = await provider.BridgeService.GetUserFeesAsync(poolId, userAddress);
                    return new ChainFee
                    {
                        ChainId = provider.ProviderType,
                        ChainName = provider.ProviderName,
                        Amount = fees
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting fees from {Provider}", provider.ProviderName);
                    return new ChainFee { ChainId = provider.ProviderType, Amount = 0 };
                }
            });
            
            var fees = await Task.WhenAll(feeTasks);
            return fees.Where(f => f.Amount > 0).ToList();
        }
        
        /// <summary>
        /// Distribute fees to all LPs proportionally
        /// </summary>
        private async Task DistributeFeesToLPs(
            string poolId,
            decimal totalFee,
            ProviderType sourceChain)
        {
            try
            {
                // Get all LP positions for this pool
                var positions = await GetAllPoolPositionsAsync(poolId);
                var totalLPTokens = positions.Sum(p => p.LPTokens);
                
                // Distribute proportionally
                var distributionTasks = positions.Select(position =>
                {
                    var userFee = (position.LPTokens / totalLPTokens) * totalFee;
                    return RecordUserFeeAsync(poolId, position.UserAddress, sourceChain, userFee);
                });
                
                await Task.WhenAll(distributionTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error distributing fees");
            }
        }
        
        /// <summary>
        /// Record fee earned by user on a specific chain
        /// </summary>
        private async Task RecordUserFeeAsync(
            string poolId,
            string userAddress,
            ProviderType chain,
            decimal amount)
        {
            // Store in database or call smart contract
            // This allows user to see "You earned $X from Solana trades"
            _logger.LogDebug(
                "User {User} earned {Amount} from {Chain} in pool {Pool}",
                userAddress, amount, chain, poolId);
        }
        
        /// <summary>
        /// Subscribe to events from all chain providers
        /// </summary>
        private void SubscribeToPoolEvents()
        {
            foreach (var provider in _providers.Values)
            {
                provider.BridgeService.OnSwap += async (sender, swapEvent) =>
                    await HandleSwapEventAsync(swapEvent);
                
                provider.BridgeService.OnLiquidityAdded += async (sender, liquidityEvent) =>
                    await HandleLiquidityAddedEventAsync(liquidityEvent);
                
                provider.BridgeService.OnLiquidityRemoved += async (sender, liquidityEvent) =>
                    await HandleLiquidityRemovedEventAsync(liquidityEvent);
            }
        }
        
        // Additional helper methods...
        private string GenerateStateHash(UnifiedPoolState state) => 
            // Implementation
            string.Empty;
        
        private async Task<byte[]> SignStateAsync(UnifiedPoolState state) =>
            // Sign with HyperDrive private key
            Array.Empty<byte>();
        
        private async Task<LiquidityPosition?> GetUserPositionAsync(string poolId, string userAddress) =>
            // Get from database
            null;
        
        private async Task<List<LiquidityPosition>> GetAllPoolPositionsAsync(string poolId) =>
            // Get from database
            new List<LiquidityPosition>();
        
        private async Task RegisterPositionAcrossChainsAsync(LiquidityPosition position)
        {
            // Store in database and notify all chains
        }
        
        private async Task SyncLiquidityRemovalAsync(string poolId, string userAddress, decimal lpTokens)
        {
            // Notify all chains that liquidity was removed
        }
        
        private async Task TransferFeesToDeploymentChainAsync(
            ProviderType sourceChain,
            ProviderType targetChain,
            string userAddress,
            decimal amount)
        {
            // Bridge fees from source to target chain (if needed)
        }
        
        private async Task HandleLiquidityAddedEventAsync(LiquidityEvent evt)
        {
            // Process liquidity addition
        }
        
        private async Task HandleLiquidityRemovedEventAsync(LiquidityEvent evt)
        {
            // Process liquidity removal
        }
    }
    
    // Supporting types
    public class UnifiedPoolState
    {
        public string PoolId { get; set; }
        public decimal Reserve0 { get; set; }
        public decimal Reserve1 { get; set; }
        public decimal TotalLPTokens { get; set; }
        public DateTime Timestamp { get; set; }
        public ProviderType ChainId { get; set; }
    }
    
    public class LiquidityPosition
    {
        public string PoolId { get; set; }
        public string UserAddress { get; set; }
        public ProviderType DeploymentChain { get; set; }
        public decimal LPTokens { get; set; }
        public decimal Amount0 { get; set; }
        public decimal Amount1 { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ProviderType> EarningFromChains { get; set; }
    }
    
    public class ChainFee
    {
        public ProviderType ChainId { get; set; }
        public string ChainName { get; set; }
        public decimal Amount { get; set; }
    }
    
    public class RemoveLiquidityResult
    {
        public decimal Amount0 { get; set; }
        public decimal Amount1 { get; set; }
        public decimal TotalFeesFromAllChains { get; set; }
        public List<ChainFee> FeesBreakdown { get; set; }
    }
    
    public class SwapEvent
    {
        public string PoolId { get; set; }
        public ProviderType SourceChain { get; set; }
        public string UserAddress { get; set; }
        public string TokenIn { get; set; }
        public string TokenOut { get; set; }
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
    }
    
    public class LiquidityEvent
    {
        public string PoolId { get; set; }
        public ProviderType Chain { get; set; }
        public string UserAddress { get; set; }
        public decimal Amount0 { get; set; }
        public decimal Amount1 { get; set; }
        public decimal LPTokens { get; set; }
    }
}
```

---

## 4. **IOASISBridge Extension** (Interface Update)

Add liquidity methods to the existing bridge interface:

```csharp
public interface IOASISBridge
{
    // Existing methods...
    Task<OASISResult<decimal>> SwapAsync(string fromToken, string toToken, decimal amount);
    Task<OASISResult<decimal>> GetExchangeRateAsync(string fromToken, string toToken);
    
    // NEW: Liquidity Pool Methods
    Task<OASISResult<decimal>> AddLiquidityAsync(
        string poolId, 
        string userAddress, 
        decimal amount0, 
        decimal amount1);
    
    Task<OASISResult<(decimal Amount0, decimal Amount1)>> RemoveLiquidityAsync(
        string poolId, 
        string userAddress, 
        decimal lpTokens);
    
    Task<OASISResult<UnifiedPoolState>> GetPoolStateAsync(string poolId);
    
    Task<OASISResult<decimal>> GetUserLPBalanceAsync(string poolId, string userAddress);
    
    Task<OASISResult<decimal>> GetUserFeesAsync(string poolId, string userAddress);
    
    Task<OASISResult<bool>> SyncPoolFromChainAsync(
        string poolId, 
        int sourceChainId, 
        UnifiedPoolState state, 
        byte[] signature);
    
    // Events
    event EventHandler<SwapEvent> OnSwap;
    event EventHandler<LiquidityEvent> OnLiquidityAdded;
    event EventHandler<LiquidityEvent> OnLiquidityRemoved;
}
```

---

## 5. **Database Schema** (For Tracking)

We need a database to track positions and fees:

```sql
-- Unified Pools Table
CREATE TABLE UnifiedPools (
    PoolId VARCHAR(100) PRIMARY KEY,
    Token0Address VARCHAR(100) NOT NULL,
    Token1Address VARCHAR(100) NOT NULL,
    Token0Symbol VARCHAR(10) NOT NULL,
    Token1Symbol VARCHAR(10) NOT NULL,
    TotalReserve0 DECIMAL(38, 18) NOT NULL,
    TotalReserve1 DECIMAL(38, 18) NOT NULL,
    TotalLPTokens DECIMAL(38, 18) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    LastSyncAt DATETIME NOT NULL
);

-- Chain-Specific Pool States
CREATE TABLE ChainPoolStates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PoolId VARCHAR(100) NOT NULL,
    ChainId INT NOT NULL,
    ChainName VARCHAR(50) NOT NULL,
    Reserve0 DECIMAL(38, 18) NOT NULL,
    Reserve1 DECIMAL(38, 18) NOT NULL,
    LPTokens DECIMAL(38, 18) NOT NULL,
    Volume24h DECIMAL(38, 18) DEFAULT 0,
    Fees24h DECIMAL(38, 18) DEFAULT 0,
    LastSyncAt DATETIME NOT NULL,
    FOREIGN KEY (PoolId) REFERENCES UnifiedPools(PoolId),
    INDEX IX_PoolId_ChainId (PoolId, ChainId)
);

-- User Liquidity Positions
CREATE TABLE LiquidityPositions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PoolId VARCHAR(100) NOT NULL,
    UserAddress VARCHAR(100) NOT NULL,
    DeploymentChain INT NOT NULL,
    DeploymentChainName VARCHAR(50) NOT NULL,
    LPTokens DECIMAL(38, 18) NOT NULL,
    InitialAmount0 DECIMAL(38, 18) NOT NULL,
    InitialAmount1 DECIMAL(38, 18) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    RemovedAt DATETIME NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (PoolId) REFERENCES UnifiedPools(PoolId),
    INDEX IX_UserAddress_PoolId (UserAddress, PoolId),
    INDEX IX_PoolId_Active (PoolId, IsActive)
);

-- Cross-Chain Fees Earned
CREATE TABLE CrossChainFees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PoolId VARCHAR(100) NOT NULL,
    UserAddress VARCHAR(100) NOT NULL,
    SourceChain INT NOT NULL,
    SourceChainName VARCHAR(50) NOT NULL,
    FeeAmount DECIMAL(38, 18) NOT NULL,
    FeeToken VARCHAR(100) NOT NULL,
    EarnedAt DATETIME NOT NULL,
    ClaimedAt DATETIME NULL,
    IsClaimed BIT NOT NULL DEFAULT 0,
    TransactionHash VARCHAR(100) NULL,
    FOREIGN KEY (PoolId) REFERENCES UnifiedPools(PoolId),
    INDEX IX_UserAddress_PoolId (UserAddress, PoolId),
    INDEX IX_Claimed (IsClaimed)
);

-- Swap Events (for analytics)
CREATE TABLE SwapEvents (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PoolId VARCHAR(100) NOT NULL,
    SourceChain INT NOT NULL,
    SourceChainName VARCHAR(50) NOT NULL,
    TraderAddress VARCHAR(100) NOT NULL,
    TokenIn VARCHAR(100) NOT NULL,
    TokenOut VARCHAR(100) NOT NULL,
    AmountIn DECIMAL(38, 18) NOT NULL,
    AmountOut DECIMAL(38, 18) NOT NULL,
    FeeAmount DECIMAL(38, 18) NOT NULL,
    SwappedAt DATETIME NOT NULL,
    TransactionHash VARCHAR(100) NOT NULL,
    FOREIGN KEY (PoolId) REFERENCES UnifiedPools(PoolId),
    INDEX IX_PoolId_Time (PoolId, SwappedAt),
    INDEX IX_Chain_Time (SourceChain, SwappedAt)
);

-- Pool Analytics (Daily aggregates)
CREATE TABLE PoolAnalytics (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PoolId VARCHAR(100) NOT NULL,
    ChainId INT NOT NULL,
    AnalyticsDate DATE NOT NULL,
    TotalVolume DECIMAL(38, 18) NOT NULL,
    TotalFees DECIMAL(38, 18) NOT NULL,
    SwapCount INT NOT NULL,
    UniqueTradersCount INT NOT NULL,
    FOREIGN KEY (PoolId) REFERENCES UnifiedPools(PoolId),
    UNIQUE INDEX IX_Pool_Chain_Date (PoolId, ChainId, AnalyticsDate)
);
```

---

## 6. **Frontend Integration** (API Endpoints)

The frontend needs these API endpoints:

```typescript
// UniversalAssetBridge/frontend/src/lib/api/liquidity.ts

export interface Pool {
  id: string;
  token0: string;
  token1: string;
  token0Icon: string;
  token1Icon: string;
  tvl: number;
  volume24h: number;
  apy: number;
  chainDistribution: ChainDistribution[];
}

export interface ChainDistribution {
  chainId: number;
  chainName: string;
  tvl: number;
  volume24h: number;
  percentage: number;
}

export interface LiquidityPosition {
  poolId: string;
  lpTokens: number;
  value: number;
  deployedOn: string;
  fees24h: number;
  feesTotal: number;
  feesBreakdown: ChainFee[];
}

export interface ChainFee {
  chainId: number;
  chainName: string;
  amount: number;
}

// API Functions
export async function getAvailablePools(): Promise<Pool[]> {
  const response = await fetch(`${API_URL}/liquidity/pools`);
  return response.json();
}

export async function getPoolDetails(poolId: string): Promise<Pool> {
  const response = await fetch(`${API_URL}/liquidity/pools/${poolId}`);
  return response.json();
}

export async function getUserPositions(userAddress: string): Promise<LiquidityPosition[]> {
  const response = await fetch(`${API_URL}/liquidity/positions/${userAddress}`);
  return response.json();
}

export async function addLiquidity(
  poolId: string,
  userAddress: string,
  amount0: number,
  amount1: number,
  deploymentChain: string
): Promise<{ lpTokens: number }> {
  const response = await fetch(`${API_URL}/liquidity/add`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ poolId, userAddress, amount0, amount1, deploymentChain })
  });
  return response.json();
}

export async function removeLiquidity(
  poolId: string,
  userAddress: string,
  lpTokens: number
): Promise<{ amount0: number; amount1: number; totalFees: number }> {
  const response = await fetch(`${API_URL}/liquidity/remove`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ poolId, userAddress, lpTokens })
  });
  return response.json();
}

export async function getPoolAnalytics(
  poolId: string,
  days: number = 30
): Promise<PoolAnalytics[]> {
  const response = await fetch(`${API_URL}/liquidity/pools/${poolId}/analytics?days=${days}`);
  return response.json();
}
```

---

## Summary: What's Needed for Full Functionality

### Smart Contracts (Deploy to all 10 chains)
1. ✅ **HyperDrivePool.sol** - EVM chains (8 contracts)
2. ✅ **hyperdrive_pool.rs** - Solana (1 program)
3. 🎯 **Radix Scrypto** - Radix (1 component)

### Backend (.NET/C#)
4. ✅ **HyperDriveLiquidityManager** - Orchestration layer
5. ✅ **IOASISBridge** extension - Interface updates
6. ✅ **Per-chain bridge services** - Implement new methods

### Database
7. ✅ **SQL Schema** - Track positions, fees, analytics

### Frontend
8. ✅ **UI Components** - Already built!
9. ✅ **API Integration** - Connect to backend

### Infrastructure
10. 🎯 **Event Listeners** - Monitor all 10 chains for events
11. 🎯 **State Sync Workers** - Background jobs to sync pool states
12. 🎯 **Fee Aggregation Service** - Collect and distribute fees
13. 🎯 **Oracle Service** - Sign and verify cross-chain messages

### Testing
14. 🎯 **Unit Tests** - Test each contract
15. 🎯 **Integration Tests** - Test cross-chain sync
16. 🎯 **Security Audit** - Professional audit before mainnet

---

## Next Steps (Priority Order)

**Phase 1: Testnet Deployment**
1. Deploy HyperDrivePool contracts to all 10 testnets
2. Deploy backend orchestration layer
3. Set up database schema
4. Configure event listeners for all chains

**Phase 2: Core Functionality**
5. Implement add/remove liquidity flows
6. Implement swap functionality
7. Implement cross-chain state sync
8. Implement fee aggregation

**Phase 3: Testing & Refinement**
9. End-to-end testing on testnets
10. Stress testing (high volume, chain failures)
11. Security testing (attack scenarios)
12. Performance optimization

**Phase 4: Production**
13. Security audit
14. Mainnet deployment
15. Seed initial liquidity
16. Public launch

---

**Current Status:**
- Frontend: ✅ Complete
- Smart contracts: 📝 Specified (need deployment)
- Backend: 📝 Specified (need implementation)
- Database: 📝 Specified (need setup)

**Time Estimate:**
- Smart contract deployment: 2-3 weeks
- Backend implementation: 3-4 weeks
- Testing: 2-3 weeks
- **Total: 7-10 weeks to production-ready**

This is the complete technical architecture for fully functional HyperDrive Liquidity Pools! 🚀

