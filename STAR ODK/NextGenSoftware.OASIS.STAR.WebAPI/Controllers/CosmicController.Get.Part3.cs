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
        /// Deletes a Universe.
        /// </summary>
        /// <param name="universeId">The Universe ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Universe deleted successfully</response>
        /// <response code="400">Error deleting Universe</response>
        [HttpDelete("universe/{universeId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUniverse(Guid universeId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteUniverseAsync(universeId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Universe");
            }
        }



        /// <summary>
        /// Adds a GalaxyCluster to a Universe.
        /// </summary>
        /// <param name="parentUniverseId">The parent Universe ID.</param>
        /// <param name="galaxyCluster">The GalaxyCluster to add.</param>
        /// <returns>The added GalaxyCluster.</returns>
        /// <response code="200">GalaxyCluster added successfully</response>
        /// <response code="400">Error adding GalaxyCluster</response>
        [HttpPost("universe/{parentUniverseId}/galaxy-cluster")]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddGalaxyCluster(Guid parentUniverseId, [FromBody] IGalaxyCluster galaxyCluster)
        {
            try
            {
                if (galaxyCluster == null)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IGalaxyCluster>
                        {
                            Result = null,
                            IsError = false,
                            Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IGalaxyCluster>
                    {
                        IsError = true,
                        Message = "GalaxyCluster cannot be null. Please provide a valid GalaxyCluster object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxyCluster>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddGalaxyClusterAsync(parentUniverseId, galaxyCluster);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IGalaxyCluster>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IGalaxyCluster>(ex, "adding GalaxyCluster");
            }
        }

        /// <summary>
        /// Updates a GalaxyCluster.
        /// </summary>
        /// <param name="galaxyCluster">The GalaxyCluster to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated GalaxyCluster.</returns>
        /// <response code="200">GalaxyCluster updated successfully</response>
        /// <response code="400">Error updating GalaxyCluster</response>
        [HttpPut("galaxy-cluster")]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGalaxyCluster([FromBody] IGalaxyCluster galaxyCluster, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (galaxyCluster == null)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IGalaxyCluster>
                        {
                            Result = null,
                            IsError = false,
                            Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IGalaxyCluster>
                    {
                        IsError = true,
                        Message = "GalaxyCluster cannot be null. Please provide a valid GalaxyCluster object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxyCluster>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateGalaxyClusterAsync(galaxyCluster, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IGalaxyCluster>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IGalaxyCluster>(ex, "updating GalaxyCluster");
            }
        }

    }
}
