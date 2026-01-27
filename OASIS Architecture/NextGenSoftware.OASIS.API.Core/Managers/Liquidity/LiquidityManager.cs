using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Liquidity
{
    /// <summary>
    /// Manages Web4 Liquidity Pool state and operations (add/remove/swap).
    /// Phase 1a: Solana-only; state persisted in Pool State Holon via HolonManager.
    /// Chain lock/release is stubbed until Solana/Base providers are wired.
    /// </summary>
    public class LiquidityManager : OASISManager
    {
        private static LiquidityManager _instance;

        public static LiquidityManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LiquidityManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public LiquidityManager(IOASISStorageProvider storageProvider, OASISDNA oasisDna = null)
            : base(storageProvider, oasisDna) { }

        private const string MetaDataKeyPoolState = "PoolState";
        private const string MetaDataKeyLiquidityPoolType = "LiquidityPoolType";
        private const string LiquidityPoolTypeValue = "Web4LiquidityPool";

        /// <summary>Get current pool state (reserves by chain, LP supply, etc.).</summary>
        public async Task<PoolStateResult> GetPoolStateAsync(Guid poolId, CancellationToken cancellationToken = default)
        {
            var result = new PoolStateResult { PoolId = poolId };
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var holonResult = await HolonManager.Instance.LoadHolonAsync(poolId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.Success = false;
                    result.Message = "Pool not found.";
                    return result;
                }

                var state = PoolStateFromHolon(holonResult.Result);
                if (state == null)
                {
                    result.Success = false;
                    result.Message = "Invalid pool state.";
                    return result;
                }

                result.Success = true;
                result.Token0 = state.Token0;
                result.Token1 = state.Token1;
                result.TotalLpSupply = state.TotalLpSupply ?? "0";
                result.ReservesByChain = state.ReservesByChain ?? new Dictionary<string, ReserveAmounts>();
                result.Message = "OK";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>Create a new pool (admin). Phase 1a: single chain "solana".</summary>
        public async Task<CreatePoolResult> CreatePoolAsync(CreatePoolRequest request, CancellationToken cancellationToken = default)
        {
            var result = new CreatePoolResult();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(request?.Token0) || string.IsNullOrWhiteSpace(request?.Token1))
                {
                    result.Success = false;
                    result.Message = "Token0 and Token1 are required.";
                    return result;
                }

                var poolId = request.PoolId != Guid.Empty ? request.PoolId : Guid.NewGuid();
                var state = new PoolStateData
                {
                    PoolId = poolId,
                    Token0 = request.Token0.Trim(),
                    Token1 = request.Token1.Trim(),
                    TotalLpSupply = "0",
                    ReservesByChain = new Dictionary<string, ReserveAmounts>(),
                    LpSharesByAvatar = new Dictionary<string, string>(),
                    Version = 1
                };
                if (!string.IsNullOrWhiteSpace(request.InitialChainId))
                {
                    state.ReservesByChain[request.InitialChainId.Trim().ToLowerInvariant()] = new ReserveAmounts { Token0 = "0", Token1 = "0" };
                }
                else
                {
                    state.ReservesByChain["solana"] = new ReserveAmounts { Token0 = "0", Token1 = "0" };
                }

                var holon = PoolStateToHolon(state);
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.Success = false;
                    result.Message = saveResult.Message ?? "Failed to save pool.";
                    return result;
                }

                result.Success = true;
                result.PoolId = poolId;
                result.Message = "Pool created.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>Add liquidity on the given chain. Phase 1a: chainId = "solana".</summary>
        public async Task<AddLiquidityResult> AddLiquidityAsync(AddLiquidityRequest request, CancellationToken cancellationToken = default)
        {
            var result = new AddLiquidityResult();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (request == null || request.PoolId == Guid.Empty || request.AvatarId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(request.ChainId))
                {
                    result.Success = false;
                    result.Message = "PoolId, AvatarId and ChainId are required.";
                    return result;
                }

                var amount0 = ParseAmount(request.Token0Amount);
                var amount1 = ParseAmount(request.Token1Amount);
                if (amount0 <= 0 && amount1 <= 0)
                {
                    result.Success = false;
                    result.Message = "At least one of Token0Amount or Token1Amount must be positive.";
                    return result;
                }

                var holonResult = await HolonManager.Instance.LoadHolonAsync(request.PoolId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.Success = false;
                    result.Message = "Pool not found.";
                    return result;
                }

                var state = PoolStateFromHolon(holonResult.Result);
                if (state == null)
                {
                    result.Success = false;
                    result.Message = "Invalid pool state.";
                    return result;
                }

                var chainId = request.ChainId.Trim().ToLowerInvariant();
                if (!state.ReservesByChain.ContainsKey(chainId))
                    state.ReservesByChain[chainId] = new ReserveAmounts { Token0 = "0", Token1 = "0" };

                var r = state.ReservesByChain[chainId];
                var res0 = ParseAmount(r.Token0);
                var res1 = ParseAmount(r.Token1);
                string lpMinted;

                if (ParseAmount(state.TotalLpSupply) == 0)
                {
                    // First deposit: LP = sqrt(amount0 * amount1)
                    if (amount0 <= 0 || amount1 <= 0)
                    {
                        result.Success = false;
                        result.Message = "First deposit requires both token amounts.";
                        return result;
                    }
                    var lp = Math.Sqrt((double)amount0 * (double)amount1);
                    lpMinted = lp.ToString("0.###############", CultureInfo.InvariantCulture);
                    r.Token0 = (res0 + amount0).ToString("0.###############", CultureInfo.InvariantCulture);
                    r.Token1 = (res1 + amount1).ToString("0.###############", CultureInfo.InvariantCulture);
                    state.TotalLpSupply = lpMinted;
                }
                else
                {
                    if (res0 <= 0 && res1 <= 0)
                    {
                        result.Success = false;
                        result.Message = "Pool has no reserves on this chain yet.";
                        return result;
                    }
                    var totalLp = ParseAmount(state.TotalLpSupply);
                    decimal lpFrom0 = res0 > 0 ? (amount0 * totalLp) / res0 : decimal.Zero;
                    decimal lpFrom1 = res1 > 0 ? (amount1 * totalLp) / res1 : decimal.Zero;
                    var lpToMint = (lpFrom0 > 0 && lpFrom1 > 0) ? Math.Min(lpFrom0, lpFrom1) : (lpFrom0 > 0 ? lpFrom0 : lpFrom1);
                    if (lpToMint <= 0)
                    {
                        result.Success = false;
                        result.Message = "Insufficient amounts for proportional add.";
                        return result;
                    }
                    lpMinted = lpToMint.ToString("0.###############", CultureInfo.InvariantCulture);
                    r.Token0 = (res0 + amount0).ToString("0.###############", CultureInfo.InvariantCulture);
                    r.Token1 = (res1 + amount1).ToString("0.###############", CultureInfo.InvariantCulture);
                    state.TotalLpSupply = (totalLp + lpToMint).ToString("0.###############", CultureInfo.InvariantCulture);
                }

                var avatarKey = request.AvatarId.ToString();
                var currentLp = ParseAmount(state.LpSharesByAvatar.TryGetValue(avatarKey, out var v) ? v : "0");
                state.LpSharesByAvatar[avatarKey] = (currentLp + ParseAmount(lpMinted)).ToString("0.###############", CultureInfo.InvariantCulture);

                var holon = PoolStateToHolon(state);
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.Success = false;
                    result.Message = saveResult.Message ?? "Failed to save pool.";
                    return result;
                }

                result.Success = true;
                result.LpTokensMinted = lpMinted;
                result.Message = "Liquidity added.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>Remove liquidity; user chooses payout chain (solana | base).</summary>
        public async Task<RemoveLiquidityResult> RemoveLiquidityAsync(RemoveLiquidityRequest request, CancellationToken cancellationToken = default)
        {
            var result = new RemoveLiquidityResult();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (request == null || request.PoolId == Guid.Empty || request.AvatarId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(request.LpAmount) || string.IsNullOrWhiteSpace(request.PreferredChainId))
                {
                    result.Success = false;
                    result.Message = "PoolId, AvatarId, LpAmount and PreferredChainId are required.";
                    return result;
                }

                var lpAmount = ParseAmount(request.LpAmount);
                if (lpAmount <= 0)
                {
                    result.Success = false;
                    result.Message = "LpAmount must be positive.";
                    return result;
                }

                var holonResult = await HolonManager.Instance.LoadHolonAsync(request.PoolId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.Success = false;
                    result.Message = "Pool not found.";
                    return result;
                }

                var state = PoolStateFromHolon(holonResult.Result);
                if (state == null)
                {
                    result.Success = false;
                    result.Message = "Invalid pool state.";
                    return result;
                }

                var avatarKey = request.AvatarId.ToString();
                if (!state.LpSharesByAvatar.TryGetValue(avatarKey, out var shareStr))
                    shareStr = "0";
                var userLp = ParseAmount(shareStr);
                if (userLp < lpAmount)
                {
                    result.Success = false;
                    result.Message = "Insufficient LP balance.";
                    return result;
                }

                var chainId = request.PreferredChainId.Trim().ToLowerInvariant();
                if (!state.ReservesByChain.TryGetValue(chainId, out var r))
                {
                    result.Success = false;
                    result.Message = "Chain not supported for this pool.";
                    return result;
                }

                var totalLp = ParseAmount(state.TotalLpSupply);
                if (totalLp <= 0)
                {
                    result.Success = false;
                    result.Message = "Pool has no LP supply.";
                    return result;
                }

                var ratio = lpAmount / totalLp;
                var res0 = ParseAmount(r.Token0);
                var res1 = ParseAmount(r.Token1);
                var out0 = (res0 * ratio).ToString("0.###############", CultureInfo.InvariantCulture);
                var out1 = (res1 * ratio).ToString("0.###############", CultureInfo.InvariantCulture);

                r.Token0 = (res0 - res0 * ratio).ToString("0.###############", CultureInfo.InvariantCulture);
                r.Token1 = (res1 - res1 * ratio).ToString("0.###############", CultureInfo.InvariantCulture);
                state.TotalLpSupply = (totalLp - lpAmount).ToString("0.###############", CultureInfo.InvariantCulture);
                state.LpSharesByAvatar[avatarKey] = (userLp - lpAmount).ToString("0.###############", CultureInfo.InvariantCulture);
                if (ParseAmount(state.LpSharesByAvatar[avatarKey]) == 0)
                    state.LpSharesByAvatar.Remove(avatarKey);

                var holon = PoolStateToHolon(state);
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.Success = false;
                    result.Message = saveResult.Message ?? "Failed to save pool.";
                    return result;
                }

                result.Success = true;
                result.Token0Amount = out0;
                result.Token1Amount = out1;
                result.Message = "Liquidity removed.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>Swap on the given chain. Same-chain only in Phase 1.</summary>
        public async Task<SwapResult> SwapAsync(SwapRequest request, CancellationToken cancellationToken = default)
        {
            var result = new SwapResult();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (request == null || request.PoolId == Guid.Empty || request.AvatarId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(request.ChainId) || string.IsNullOrWhiteSpace(request.TokenIn) ||
                    string.IsNullOrWhiteSpace(request.TokenOut) || string.IsNullOrWhiteSpace(request.AmountIn))
                {
                    result.Success = false;
                    result.Message = "PoolId, AvatarId, ChainId, TokenIn, TokenOut and AmountIn are required.";
                    return result;
                }

                var amountIn = ParseAmount(request.AmountIn);
                if (amountIn <= 0)
                {
                    result.Success = false;
                    result.Message = "AmountIn must be positive.";
                    return result;
                }

                var holonResult = await HolonManager.Instance.LoadHolonAsync(request.PoolId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.Success = false;
                    result.Message = "Pool not found.";
                    return result;
                }

                var state = PoolStateFromHolon(holonResult.Result);
                if (state == null)
                {
                    result.Success = false;
                    result.Message = "Invalid pool state.";
                    return result;
                }

                var chainId = request.ChainId.Trim().ToLowerInvariant();
                if (!state.ReservesByChain.TryGetValue(chainId, out var r))
                {
                    result.Success = false;
                    result.Message = "Chain not supported for this pool.";
                    return result;
                }

                var tokenIn = request.TokenIn.Trim().ToLowerInvariant();
                var tokenOut = request.TokenOut.Trim().ToLowerInvariant();
                string resInStr, resOutStr;
                if (tokenIn == "token0" || (state.Token0 != null && state.Token0.ToLowerInvariant() == tokenIn))
                {
                    resInStr = r.Token0;
                    resOutStr = r.Token1;
                }
                else if (tokenIn == "token1" || (state.Token1 != null && state.Token1.ToLowerInvariant() == tokenIn))
                {
                    resInStr = r.Token1;
                    resOutStr = r.Token0;
                }
                else
                {
                    result.Success = false;
                    result.Message = "TokenIn must be token0 or token1.";
                    return result;
                }

                var reserveIn = ParseAmount(resInStr);
                var reserveOut = ParseAmount(resOutStr);
                if (reserveIn <= 0 || reserveOut <= 0)
                {
                    result.Success = false;
                    result.Message = "Insufficient reserves for swap.";
                    return result;
                }

                // Constant-product: amountOut = (amountIn * reserveOut) / (reserveIn + amountIn)
                var amountOut = (amountIn * reserveOut) / (reserveIn + amountIn);
                var minOut = ParseAmount(request.MinAmountOut ?? "0");
                if (amountOut < minOut)
                {
                    result.Success = false;
                    result.Message = "Slippage: amountOut below MinAmountOut.";
                    return result;
                }

                if (tokenIn == "token0" || (state.Token0 != null && state.Token0.ToLowerInvariant() == tokenIn))
                {
                    r.Token0 = (reserveIn + amountIn).ToString("0.###############", CultureInfo.InvariantCulture);
                    r.Token1 = (reserveOut - amountOut).ToString("0.###############", CultureInfo.InvariantCulture);
                }
                else
                {
                    r.Token0 = (reserveOut - amountOut).ToString("0.###############", CultureInfo.InvariantCulture);
                    r.Token1 = (reserveIn + amountIn).ToString("0.###############", CultureInfo.InvariantCulture);
                }

                var holon = PoolStateToHolon(state);
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.Success = false;
                    result.Message = saveResult.Message ?? "Failed to save pool.";
                    return result;
                }

                result.Success = true;
                result.AmountOut = amountOut.ToString("0.###############", CultureInfo.InvariantCulture);
                result.Message = "Swap completed.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        private static decimal ParseAmount(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return decimal.Zero;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : decimal.Zero;
        }

        private static PoolStateData PoolStateFromHolon(IHolon holon)
        {
            if (holon?.MetaData == null || !holon.MetaData.TryGetValue(MetaDataKeyPoolState, out var obj) || obj == null)
                return null;
            try
            {
                var json = obj is string str ? str : JsonConvert.SerializeObject(obj);
                return JsonConvert.DeserializeObject<PoolStateData>(json);
            }
            catch
            {
                return null;
            }
        }

        private static IHolon PoolStateToHolon(PoolStateData state)
        {
            if (state == null) return null;
            var holon = new Holon(state.PoolId)
            {
                HolonType = HolonType.LiquidityPool,
                Name = $"LiquidityPool_{state.Token0}_{state.Token1}",
                Description = $"Web4 Liquidity Pool {state.Token0}/{state.Token1}",
                MetaData = new Dictionary<string, object>
                {
                    [MetaDataKeyLiquidityPoolType] = LiquidityPoolTypeValue,
                    [MetaDataKeyPoolState] = JsonConvert.SerializeObject(state)
                }
            };
            return holon;
        }
    }

    #region DTOs and state

    /// <summary>Canonical pool state (matches Web4 schema).</summary>
    public class PoolStateData
    {
        public Guid PoolId { get; set; }
        public string Token0 { get; set; }
        public string Token1 { get; set; }
        public string TotalLpSupply { get; set; }
        public Dictionary<string, ReserveAmounts> ReservesByChain { get; set; }
        public Dictionary<string, string> LpSharesByAvatar { get; set; }
        public int Version { get; set; }
    }

    public class ReserveAmounts
    {
        public string Token0 { get; set; }
        public string Token1 { get; set; }
    }

    public class CreatePoolRequest
    {
        public Guid PoolId { get; set; }
        public string Token0 { get; set; }
        public string Token1 { get; set; }
        public string InitialChainId { get; set; }
    }

    public class CreatePoolResult
    {
        public bool Success { get; set; }
        public Guid PoolId { get; set; }
        public string Message { get; set; }
    }

    public class AddLiquidityRequest
    {
        public Guid PoolId { get; set; }
        public Guid AvatarId { get; set; }
        public string ChainId { get; set; }
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
        public string PreferredChainId { get; set; }
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
        public string ChainId { get; set; }
        public string TokenIn { get; set; }
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

    #endregion
}
