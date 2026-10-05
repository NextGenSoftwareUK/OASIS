using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin
{
    /// <summary>
    /// Manager for Zcash-backed stablecoin (zUSD) operations
    /// Handles minting, redeeming, position management, and liquidation
    /// </summary>
    public class StablecoinManager
    {
        private readonly IStablecoinRepository _repository;
        private readonly IZecPriceOracle _priceOracle;
        private readonly IZcashCollateralService _zcashService;
        private readonly IAztecStablecoinService _aztecService;
        private readonly IViewingKeyService _viewingKeyService;

        // System parameters
        private const decimal MINIMUM_COLLATERAL_RATIO = 150m; // 150%
        private const decimal LIQUIDATION_THRESHOLD = 110m; // 110%
        private const decimal WARNING_THRESHOLD = 120m; // 120%
        private const decimal DANGER_THRESHOLD = 150m; // 150%
        private const decimal DEFAULT_APY = 5.0m; // 5% APY

        public StablecoinManager(
            IStablecoinRepository repository,
            IZecPriceOracle priceOracle,
            IZcashCollateralService zcashService,
            IAztecStablecoinService aztecService,
            IViewingKeyService viewingKeyService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _priceOracle = priceOracle ?? throw new ArgumentNullException(nameof(priceOracle));
            _zcashService = zcashService ?? throw new ArgumentNullException(nameof(zcashService));
            _aztecService = aztecService ?? throw new ArgumentNullException(nameof(aztecService));
            _viewingKeyService = viewingKeyService ?? throw new ArgumentNullException(nameof(viewingKeyService));
        }

        /// <summary>
        /// Mints stablecoin (zUSD) by locking ZEC collateral
        /// </summary>
        public async Task<OASISResult<StablecoinPosition>> MintStablecoinAsync(
            MintStablecoinRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<StablecoinPosition>();

            try
            {
                // Validate request
                if (request.ZecAmount <= 0)
                {
                    result.IsError = true;
                    result.Message = "ZEC amount must be greater than zero";
                    return result;
                }

                if (request.StablecoinAmount <= 0)
                {
                    result.IsError = true;
                    result.Message = "Stablecoin amount must be greater than zero";
                    return result;
                }

                // Get current ZEC price
                var priceResult = await _priceOracle.GetZecPriceAsync(cancellationToken);
                if (priceResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to get ZEC price: {priceResult.Message}";
                    return result;
                }

                decimal zecPrice = priceResult.Result;

                // Calculate collateral ratio
                decimal collateralValue = request.ZecAmount * zecPrice;
                decimal collateralRatio = (collateralValue / request.StablecoinAmount) * 100m;

                // Validate minimum collateral ratio
                if (collateralRatio < MINIMUM_COLLATERAL_RATIO)
                {
                    result.IsError = true;
                    result.Message = $"Collateral ratio ({collateralRatio:F2}%) is below minimum ({MINIMUM_COLLATERAL_RATIO}%)";
                    return result;
                }

                // Lock ZEC collateral
                var lockResult = await _zcashService.LockCollateralAsync(
                    request.ZcashAddress,
                    request.ZecAmount,
                    cancellationToken);

                if (lockResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to lock ZEC collateral: {lockResult.Message}";
                    return result;
                }

                // Mint zUSD on Aztec
                var mintResult = await _aztecService.MintStablecoinAsync(
                    request.AztecAddress,
                    request.StablecoinAmount,
                    cancellationToken);

                if (mintResult.IsError)
                {
                    // Rollback: Unlock ZEC if minting fails
                    await _zcashService.UnlockCollateralAsync(
                        request.ZcashAddress,
                        request.ZecAmount,
                        cancellationToken);

                    result.IsError = true;
                    result.Message = $"Failed to mint stablecoin: {mintResult.Message}";
                    return result;
                }

                // Generate viewing key if requested
                string? viewingKeyHash = null;
                if (request.GenerateViewingKey)
                {
                    var viewingKeyResult = await _viewingKeyService.GenerateViewingKeyAsync(
                        request.AvatarId,
                        cancellationToken);
                    if (!viewingKeyResult.IsError)
                    {
                        viewingKeyHash = viewingKeyResult.Result;
                    }
                }

                // Create position
                var position = new StablecoinPosition
                {
                    PositionId = Guid.NewGuid(),
                    AvatarId = request.AvatarId,
                    CollateralAmount = request.ZecAmount,
                    DebtAmount = request.StablecoinAmount,
                    CollateralRatio = collateralRatio,
                    Health = CalculateHealth(collateralRatio),
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    ViewingKeyHash = viewingKeyHash,
                    ZcashAddress = request.ZcashAddress,
                    AztecAddress = request.AztecAddress
                };

                // Save position
                var saveResult = await _repository.SavePositionAsync(position, cancellationToken);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save position: {saveResult.Message}";
                    return result;
                }

                result.Result = position;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error minting stablecoin: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Redeems stablecoin (zUSD) to unlock ZEC collateral
        /// </summary>
        public async Task<OASISResult<string>> RedeemStablecoinAsync(
            RedeemStablecoinRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<string>();

            try
            {
                // Load position
                var positionResult = await _repository.GetPositionAsync(request.PositionId, cancellationToken);
                if (positionResult.IsError || positionResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                var position = positionResult.Result;

                // Validate ownership
                if (position.AvatarId != request.AvatarId)
                {
                    result.IsError = true;
                    result.Message = "Unauthorized: Position does not belong to this avatar";
                    return result;
                }

                // Validate amount
                if (request.StablecoinAmount <= 0 || request.StablecoinAmount > position.DebtAmount)
                {
                    result.IsError = true;
                    result.Message = "Invalid redeem amount";
                    return result;
                }

                // Get current ZEC price
                var priceResult = await _priceOracle.GetZecPriceAsync(cancellationToken);
                if (priceResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to get ZEC price: {priceResult.Message}";
                    return result;
                }

                decimal zecPrice = priceResult.Result;

                // Calculate ZEC to unlock (proportional to debt reduction)
                decimal zecToUnlock = (request.StablecoinAmount / position.DebtAmount) * position.CollateralAmount;

                // Burn zUSD on Aztec
                var burnResult = await _aztecService.BurnStablecoinAsync(
                    position.AztecAddress,
                    request.StablecoinAmount,
                    cancellationToken);

                if (burnResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to burn stablecoin: {burnResult.Message}";
                    return result;
                }

                // Unlock ZEC collateral
                var unlockResult = await _zcashService.UnlockCollateralAsync(
                    request.ZcashAddress,
                    zecToUnlock,
                    cancellationToken);

                if (unlockResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to unlock ZEC: {unlockResult.Message}";
                    return result;
                }

                // Update position
                position.DebtAmount -= request.StablecoinAmount;
                position.CollateralAmount -= zecToUnlock;

                if (position.DebtAmount <= 0)
                {
                    // Position fully redeemed, delete it
                    await _repository.DeletePositionAsync(request.PositionId, cancellationToken);
                    result.Result = "Position fully redeemed and closed";
                }
                else
                {
                    // Recalculate collateral ratio
                    decimal collateralValue = position.CollateralAmount * zecPrice;
                    position.CollateralRatio = (collateralValue / position.DebtAmount) * 100m;
                    position.Health = CalculateHealth(position.CollateralRatio);
                    position.LastUpdated = DateTime.UtcNow;

                    await _repository.SavePositionAsync(position, cancellationToken);
                    result.Result = "Stablecoin redeemed successfully";
                }

                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error redeeming stablecoin: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets a position by ID
        /// </summary>
        public async Task<OASISResult<StablecoinPosition>> GetPositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<StablecoinPosition>();

            try
            {
                var positionResult = await _repository.GetPositionAsync(positionId, cancellationToken);
                if (positionResult.IsError || positionResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                // Update health based on current price
                var position = positionResult.Result;
                await UpdatePositionHealthAsync(position, cancellationToken);

                result.Result = position;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error getting position: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets position health status
        /// </summary>
        public async Task<OASISResult<PositionHealthResponse>> GetPositionHealthAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<PositionHealthResponse>();

            try
            {
                var positionResult = await GetPositionAsync(positionId, cancellationToken);
                if (positionResult.IsError || positionResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                var position = positionResult.Result;
                var priceResult = await _priceOracle.GetZecPriceAsync(cancellationToken);
                decimal zecPrice = priceResult.IsError ? 0 : priceResult.Result;

                result.Result = new PositionHealthResponse
                {
                    Health = position.Health.ToString().ToLower(),
                    Ratio = position.CollateralRatio,
                    CollateralAmount = position.CollateralAmount,
                    DebtAmount = position.DebtAmount,
                    ZecPrice = zecPrice
                };

                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error getting position health: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets all positions for an avatar
        /// </summary>
        public async Task<OASISResult<List<StablecoinPosition>>> GetPositionsAsync(
            Guid avatarId,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<List<StablecoinPosition>>();

            try
            {
                var positionsResult = await _repository.GetPositionsByAvatarAsync(avatarId, cancellationToken);
                if (positionsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = positionsResult.Message;
                    return result;
                }

                // Update health for all positions
                var positions = positionsResult.Result ?? new List<StablecoinPosition>();
                foreach (var position in positions)
                {
                    await UpdatePositionHealthAsync(position, cancellationToken);
                }

                result.Result = positions;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error getting positions: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets system-wide status
        /// </summary>
        public async Task<OASISResult<SystemStatus>> GetSystemStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<SystemStatus>();

            try
            {
                // Get all positions
                var allPositionsResult = await _repository.GetAllPositionsAsync(cancellationToken);
                if (allPositionsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = allPositionsResult.Message;
                    return result;
                }

                var positions = allPositionsResult.Result ?? new List<StablecoinPosition>();

                // Get current ZEC price
                var priceResult = await _priceOracle.GetZecPriceAsync(cancellationToken);
                decimal zecPrice = priceResult.IsError ? 0 : priceResult.Result;

                // Calculate totals
                decimal totalSupply = positions.Sum(p => p.DebtAmount);
                decimal totalCollateral = positions.Sum(p => p.CollateralAmount);
                decimal collateralValue = totalCollateral * zecPrice;
                decimal collateralRatio = totalSupply > 0 ? (collateralValue / totalSupply) * 100m : 0;

                int liquidatedCount = positions.Count(p => p.Health == PositionHealth.Liquidated);
                int activeCount = positions.Count - liquidatedCount;

                result.Result = new SystemStatus
                {
                    TotalSupply = totalSupply,
                    TotalCollateral = totalCollateral,
                    CollateralRatio = collateralRatio,
                    LiquidationThreshold = LIQUIDATION_THRESHOLD,
                    CurrentAPY = DEFAULT_APY,
                    ZecPrice = zecPrice,
                    MinimumCollateralRatio = MINIMUM_COLLATERAL_RATIO,
                    ActivePositions = activeCount,
                    LiquidatedPositions = liquidatedCount
                };

                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error getting system status: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Liquidates an undercollateralized position
        /// </summary>
        public async Task<OASISResult<string>> LiquidatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<string>();

            try
            {
                var positionResult = await GetPositionAsync(positionId, cancellationToken);
                if (positionResult.IsError || positionResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                var position = positionResult.Result;

                // Check if position is liquidatable
                if (position.Health != PositionHealth.Danger && position.Health != PositionHealth.Liquidated)
                {
                    result.IsError = true;
                    result.Message = "Position is not liquidatable (collateral ratio above threshold)";
                    return result;
                }

                // Liquidate: Burn all zUSD, unlock remaining ZEC (with penalty)
                decimal liquidationPenalty = 0.1m; // 10% penalty
                decimal zecToUnlock = position.CollateralAmount * (1 - liquidationPenalty);

                // Burn all remaining zUSD
                await _aztecService.BurnStablecoinAsync(
                    position.AztecAddress,
                    position.DebtAmount,
                    cancellationToken);

                // Unlock ZEC (with penalty)
                await _zcashService.UnlockCollateralAsync(
                    position.ZcashAddress,
                    zecToUnlock,
                    cancellationToken);

                // Mark as liquidated
                position.Health = PositionHealth.Liquidated;
                position.DebtAmount = 0;
                position.CollateralAmount = 0;
                position.LastUpdated = DateTime.UtcNow;

                await _repository.SavePositionAsync(position, cancellationToken);

                result.Result = $"Position liquidated. {zecToUnlock:F8} ZEC unlocked (10% penalty applied)";
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error liquidating position: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Generates yield for a position
        /// </summary>
        public async Task<OASISResult<decimal>> GenerateYieldAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<decimal>();

            try
            {
                var positionResult = await GetPositionAsync(positionId, cancellationToken);
                if (positionResult.IsError || positionResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                var position = positionResult.Result;

                // Calculate yield based on APY and time since last update
                var timeSinceUpdate = DateTime.UtcNow - position.LastUpdated;
                var daysSinceUpdate = (decimal)timeSinceUpdate.TotalDays;
                var annualYield = position.CollateralAmount * (DEFAULT_APY / 100m);
                var yieldAmount = annualYield * (daysSinceUpdate / 365m);

                // Add yield to collateral
                position.CollateralAmount += yieldAmount;
                position.LastUpdated = DateTime.UtcNow;

                await UpdatePositionHealthAsync(position, cancellationToken);
                await _repository.SavePositionAsync(position, cancellationToken);

                result.Result = yieldAmount;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error generating yield: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Calculates health status based on collateral ratio
        /// </summary>
        private PositionHealth CalculateHealth(decimal collateralRatio)
        {
            if (collateralRatio < LIQUIDATION_THRESHOLD)
                return PositionHealth.Liquidated;
            if (collateralRatio < WARNING_THRESHOLD)
                return PositionHealth.Danger;
            if (collateralRatio < DANGER_THRESHOLD)
                return PositionHealth.Warning;
            return PositionHealth.Safe;
        }

        /// <summary>
        /// Updates position health based on current ZEC price
        /// </summary>
        private async Task UpdatePositionHealthAsync(
            StablecoinPosition position,
            CancellationToken cancellationToken)
        {
            try
            {
                var priceResult = await _priceOracle.GetZecPriceAsync(cancellationToken);
                if (!priceResult.IsError)
                {
                    decimal zecPrice = priceResult.Result;
                    decimal collateralValue = position.CollateralAmount * zecPrice;
                    if (position.DebtAmount > 0)
                    {
                        position.CollateralRatio = (collateralValue / position.DebtAmount) * 100m;
                        position.Health = CalculateHealth(position.CollateralRatio);
                    }
                }
            }
            catch
            {
                // Ignore errors in health update
            }
        }
    }
}

