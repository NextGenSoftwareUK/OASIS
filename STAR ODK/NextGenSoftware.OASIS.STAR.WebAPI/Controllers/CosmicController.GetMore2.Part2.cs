using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Collections.Generic;
using System.Threading;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public partial class CosmicController
    {

        /// <summary>
        /// Deletes a BlackHole.
        /// </summary>
        /// <param name="blackHoleId">The BlackHole ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">BlackHole deleted successfully</response>
        /// <response code="400">Error deleting BlackHole</response>
        [HttpDelete("blackhole/{blackHoleId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteBlackHole(Guid blackHoleId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteBlackHoleAsync(blackHoleId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting BlackHole");
            }
        }



        /// <summary>
        /// Updates a Portal.
        /// </summary>
        /// <param name="portal">The Portal to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Portal.</returns>
        /// <response code="200">Portal updated successfully</response>
        /// <response code="400">Error updating Portal</response>
        [HttpPut("portal")]
        [ProducesResponseType(typeof(OASISResult<IPortal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IPortal>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePortal([FromBody] IPortal portal, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (portal == null)
                {
                    return BadRequest(new OASISResult<IPortal>
                    {
                        IsError = true,
                        Message = "Portal cannot be null. Please provide a valid Portal object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IPortal>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdatePortalAsync(portal, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IPortal>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IPortal>(ex, "updating Portal");
            }
        }

        /// <summary>
        /// Deletes a Portal.
        /// </summary>
        /// <param name="portalId">The Portal ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Portal deleted successfully</response>
        /// <response code="400">Error deleting Portal</response>
        [HttpDelete("portal/{portalId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeletePortal(Guid portalId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeletePortalAsync(portalId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Portal");
            }
        }



        /// <summary>
        /// Updates a StarGate.
        /// </summary>
        /// <param name="starGate">The StarGate to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated StarGate.</returns>
        /// <response code="200">StarGate updated successfully</response>
        /// <response code="400">Error updating StarGate</response>
        [HttpPut("stargate")]
        [ProducesResponseType(typeof(OASISResult<IStarGate>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStarGate>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStarGate([FromBody] IStarGate starGate, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (starGate == null)
                {
                    return BadRequest(new OASISResult<IStarGate>
                    {
                        IsError = true,
                        Message = "StarGate cannot be null. Please provide a valid StarGate object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStarGate>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateStarGateAsync(starGate, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStarGate>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStarGate>(ex, "updating StarGate");
            }
        }

        /// <summary>
        /// Deletes a StarGate.
        /// </summary>
        /// <param name="starGateId">The StarGate ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">StarGate deleted successfully</response>
        /// <response code="400">Error deleting StarGate</response>
        [HttpDelete("stargate/{starGateId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteStarGate(Guid starGateId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteStarGateAsync(starGateId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting StarGate");
            }
        }



        /// <summary>
        /// Updates a SpaceTimeDistortion.
        /// </summary>
        /// <param name="distortion">The SpaceTimeDistortion to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SpaceTimeDistortion.</returns>
        /// <response code="200">SpaceTimeDistortion updated successfully</response>
        /// <response code="400">Error updating SpaceTimeDistortion</response>
        [HttpPut("spacetime-distortion")]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeDistortion>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeDistortion>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSpaceTimeDistortion([FromBody] ISpaceTimeDistortion distortion, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (distortion == null)
                {
                    return BadRequest(new OASISResult<ISpaceTimeDistortion>
                    {
                        IsError = true,
                        Message = "SpaceTimeDistortion cannot be null. Please provide a valid SpaceTimeDistortion object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISpaceTimeDistortion>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateSpaceTimeDistortionAsync(distortion, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISpaceTimeDistortion>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISpaceTimeDistortion>(ex, "updating SpaceTimeDistortion");
            }
        }

        /// <summary>
        /// Deletes a SpaceTimeDistortion.
        /// </summary>
        /// <param name="distortionId">The SpaceTimeDistortion ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SpaceTimeDistortion deleted successfully</response>
        /// <response code="400">Error deleting SpaceTimeDistortion</response>
        [HttpDelete("spacetime-distortion/{distortionId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSpaceTimeDistortion(Guid distortionId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSpaceTimeDistortionAsync(distortionId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SpaceTimeDistortion");
            }
        }



        /// <summary>
        /// Updates a SpaceTimeAbnormally.
        /// </summary>
        /// <param name="abnormally">The SpaceTimeAbnormally to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SpaceTimeAbnormally.</returns>
        /// <response code="200">SpaceTimeAbnormally updated successfully</response>
        /// <response code="400">Error updating SpaceTimeAbnormally</response>
        [HttpPut("spacetime-abnormally")]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeAbnormally>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeAbnormally>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSpaceTimeAbnormally([FromBody] ISpaceTimeAbnormally abnormally, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (abnormally == null)
                {
                    return BadRequest(new OASISResult<ISpaceTimeAbnormally>
                    {
                        IsError = true,
                        Message = "SpaceTimeAbnormally cannot be null. Please provide a valid SpaceTimeAbnormally object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISpaceTimeAbnormally>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateSpaceTimeAbnormallyAsync(abnormally, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISpaceTimeAbnormally>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISpaceTimeAbnormally>(ex, "updating SpaceTimeAbnormally");
            }
        }

        /// <summary>
        /// Deletes a SpaceTimeAbnormally.
        /// </summary>
        /// <param name="abnormallyId">The SpaceTimeAbnormally ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SpaceTimeAbnormally deleted successfully</response>
        /// <response code="400">Error deleting SpaceTimeAbnormally</response>
        [HttpDelete("spacetime-abnormally/{abnormallyId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSpaceTimeAbnormally(Guid abnormallyId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSpaceTimeAbnormallyAsync(abnormallyId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SpaceTimeAbnormally");
            }
        }



        /// <summary>
        /// Updates a TemporalRift.
        /// </summary>
        /// <param name="rift">The TemporalRift to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated TemporalRift.</returns>
        /// <response code="200">TemporalRift updated successfully</response>
        /// <response code="400">Error updating TemporalRift</response>
        [HttpPut("temporal-rift")]
        [ProducesResponseType(typeof(OASISResult<ITemporalRift>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ITemporalRift>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTemporalRift([FromBody] ITemporalRift rift, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (rift == null)
                {
                    return BadRequest(new OASISResult<ITemporalRift>
                    {
                        IsError = true,
                        Message = "TemporalRift cannot be null. Please provide a valid TemporalRift object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ITemporalRift>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateTemporalRiftAsync(rift, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ITemporalRift>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ITemporalRift>(ex, "updating TemporalRift");
            }
        }
    }
}
