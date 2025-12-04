using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Repositories
{
    /// <summary>
    /// Repository for storing and retrieving stablecoin positions using OASIS Holon storage
    /// </summary>
    public class StablecoinRepository : IStablecoinRepository
    {
        private const string HOLON_TYPE_NAME = "StablecoinPosition";
        private const string POSITION_ID_METADATA_KEY = "positionId";

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
                // Load holon by metadata (positionId)
                var holonsResult = await HolonManager.Instance.LoadHolonsByMetaDataAsync(
                    POSITION_ID_METADATA_KEY,
                    positionId.ToString(),
                    HolonType.Default,
                    false, false, 0, true, false, 0, HolonType.All, 0);

                if (holonsResult.IsError || holonsResult.Result == null || !holonsResult.Result.Any())
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                var holon = holonsResult.Result.FirstOrDefault(h => h is StablecoinPositionHolon);
                if (holon == null)
                {
                    result.IsError = true;
                    result.Message = "Position holon not found";
                    return result;
                }

                result.Result = ConvertHolonToPosition((StablecoinPositionHolon)holon);
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error loading position: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets all positions for an avatar
        /// </summary>
        public async Task<OASISResult<List<StablecoinPosition>>> GetPositionsByAvatarAsync(
            Guid avatarId, 
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<List<StablecoinPosition>>();

            try
            {
                // Load holons by metadata (avatarId)
                var holonsResult = await HolonManager.Instance.LoadHolonsByMetaDataAsync(
                    "avatarId",
                    avatarId.ToString(),
                    HolonType.Default,
                    false, false, 0, true, false, 0, HolonType.All, 0);

                if (holonsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = holonsResult.Message;
                    return result;
                }

                var positions = (holonsResult.Result ?? new List<IHolon>())
                    .OfType<StablecoinPositionHolon>()
                    .Where(h => h.AvatarId == avatarId)
                    .Select(ConvertHolonToPosition)
                    .ToList();

                result.Result = positions;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error loading positions by avatar: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets all positions in the system
        /// </summary>
        public async Task<OASISResult<List<StablecoinPosition>>> GetAllPositionsAsync(
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<List<StablecoinPosition>>();

            try
            {
                // Load all holons of type StablecoinPosition
                var holonsResult = await HolonManager.Instance.LoadHolonsForParentAsync(
                    Guid.Empty, // Root parent
                    HolonType.Default);

                if (holonsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = holonsResult.Message;
                    return result;
                }

                var positions = (holonsResult.Result ?? new List<IHolon>())
                    .OfType<StablecoinPositionHolon>()
                    .Where(h => h.MetaData != null && h.MetaData.ContainsKey("holonType") && 
                                h.MetaData["holonType"] == HOLON_TYPE_NAME)
                    .Select(ConvertHolonToPosition)
                    .ToList();

                result.Result = positions;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error loading all positions: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Saves a position
        /// </summary>
        public async Task<OASISResult<bool>> SavePositionAsync(
            StablecoinPosition position, 
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<bool>();

            try
            {
                var holon = ConvertPositionToHolon(position);

                // Set metadata for querying
                holon.MetaData[POSITION_ID_METADATA_KEY] = position.PositionId.ToString();
                holon.MetaData["avatarId"] = position.AvatarId.ToString();
                holon.MetaData["holonType"] = HOLON_TYPE_NAME;

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);

                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = saveResult.Message;
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error saving position: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Deletes a position
        /// </summary>
        public async Task<OASISResult<bool>> DeletePositionAsync(
            Guid positionId, 
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<bool>();

            try
            {
                // Load the position first
                var positionResult = await GetPositionAsync(positionId, cancellationToken);
                if (positionResult.IsError || positionResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Position not found";
                    return result;
                }

                // Load the holon
                var holonsResult = await HolonManager.Instance.LoadHolonsByMetaDataAsync(
                    POSITION_ID_METADATA_KEY,
                    positionId.ToString(),
                    HolonType.Default,
                    false, false, 0, true, false, 0, HolonType.All, 0);

                if (holonsResult.IsError || holonsResult.Result == null || !holonsResult.Result.Any())
                {
                    result.IsError = true;
                    result.Message = "Position holon not found";
                    return result;
                }

                var holon = holonsResult.Result.FirstOrDefault(h => h is StablecoinPositionHolon);
                if (holon == null)
                {
                    result.IsError = true;
                    result.Message = "Position holon not found";
                    return result;
                }

                // Delete the holon - DeleteHolonAsync requires id and avatarId
                var deleteResult = await HolonManager.Instance.DeleteHolonAsync(holon.Id, positionResult.Result.AvatarId);

                if (deleteResult.IsError)
                {
                    result.IsError = true;
                    result.Message = deleteResult.Message;
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error deleting position: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Converts a StablecoinPositionHolon to a StablecoinPosition DTO
        /// </summary>
        private static StablecoinPosition ConvertHolonToPosition(StablecoinPositionHolon holon)
        {
            return new StablecoinPosition
            {
                PositionId = holon.PositionId,
                AvatarId = holon.AvatarId,
                CollateralAmount = holon.CollateralAmount,
                DebtAmount = holon.DebtAmount,
                CollateralRatio = holon.CollateralRatio,
                Health = Enum.TryParse<PositionHealth>(holon.Health, out var health) 
                    ? health 
                    : PositionHealth.Safe,
                CreatedAt = holon.CreatedDate,
                LastUpdated = holon.ModifiedDate,
                ViewingKeyHash = holon.ViewingKeyHash,
                ZcashAddress = holon.ZcashAddress,
                AztecAddress = holon.AztecAddress
            };
        }

        /// <summary>
        /// Converts a StablecoinPosition DTO to a StablecoinPositionHolon
        /// </summary>
        private static StablecoinPositionHolon ConvertPositionToHolon(StablecoinPosition position)
        {
            // Create new holon (we'll update it if it exists when saving)
            var holon = new StablecoinPositionHolon
            {
                Id = Guid.NewGuid(),
                CreatedDate = position.CreatedAt,
                ModifiedDate = position.LastUpdated,
                Name = $"Stablecoin Position {position.PositionId}",
                Description = $"ZEC collateral: {position.CollateralAmount}, zUSD debt: {position.DebtAmount}"
            };

            // Update properties
            holon.PositionId = position.PositionId;
            holon.AvatarId = position.AvatarId;
            holon.CollateralAmount = position.CollateralAmount;
            holon.DebtAmount = position.DebtAmount;
            holon.CollateralRatio = position.CollateralRatio;
            holon.Health = position.Health.ToString();
            holon.ViewingKeyHash = position.ViewingKeyHash;
            holon.ZcashAddress = position.ZcashAddress;
            holon.AztecAddress = position.AztecAddress;
            holon.CreatedByAvatarId = position.AvatarId;
            holon.ModifiedByAvatarId = position.AvatarId;

            return holon;
        }
    }
}

