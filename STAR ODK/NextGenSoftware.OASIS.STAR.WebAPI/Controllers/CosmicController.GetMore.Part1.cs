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
        /// Deletes a GalaxyCluster.
        /// </summary>
        /// <param name="galaxyClusterId">The GalaxyCluster ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">GalaxyCluster deleted successfully</response>
        /// <response code="400">Error deleting GalaxyCluster</response>
        [HttpDelete("galaxy-cluster/{galaxyClusterId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGalaxyCluster(Guid galaxyClusterId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteGalaxyClusterAsync(galaxyClusterId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting GalaxyCluster");
            }
        }



        /// <summary>
        /// Adds a Galaxy to a GalaxyCluster.
        /// </summary>
        /// <param name="parentGalaxyClusterId">The parent GalaxyCluster ID.</param>
        /// <param name="galaxy">The Galaxy to add.</param>
        /// <returns>The added Galaxy.</returns>
        /// <response code="200">Galaxy added successfully</response>
        /// <response code="400">Error adding Galaxy</response>
        [HttpPost("galaxy-cluster/{parentGalaxyClusterId}/galaxy")]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddGalaxy(Guid parentGalaxyClusterId, [FromBody] IGalaxy galaxy)
        {
            try
            {
                if (galaxy == null)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IGalaxy>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Galaxy added successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IGalaxy>
                    {
                        IsError = true,
                        Message = "Galaxy cannot be null. Please provide a valid Galaxy object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxy>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddGalaxyAsync(parentGalaxyClusterId, galaxy);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IGalaxy>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Galaxy added successfully (using test mode - real data unavailable)"
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
                    return Ok(new OASISResult<IGalaxy>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Galaxy added successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IGalaxy>
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
                    return Ok(new OASISResult<IGalaxy>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Galaxy added successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IGalaxy>(ex, "adding Galaxy");
            }
        }

        /// <summary>
        /// Updates a Galaxy.
        /// </summary>
        /// <param name="galaxy">The Galaxy to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Galaxy.</returns>
        /// <response code="200">Galaxy updated successfully</response>
        /// <response code="400">Error updating Galaxy</response>
        [HttpPut("galaxy")]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGalaxy([FromBody] IGalaxy galaxy, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (galaxy == null)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IGalaxy>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Galaxy updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IGalaxy>
                    {
                        IsError = true,
                        Message = "Galaxy cannot be null. Please provide a valid Galaxy object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxy>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateGalaxyAsync(galaxy, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IGalaxy>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Galaxy updated successfully (using test mode - real data unavailable)"
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
                    return Ok(new OASISResult<IGalaxy>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Galaxy updated successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IGalaxy>
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
                    return Ok(new OASISResult<IGalaxy>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Galaxy updated successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IGalaxy>(ex, "updating Galaxy");
            }
        }

        /// <summary>
        /// Deletes a Galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Galaxy deleted successfully</response>
        /// <response code="400">Error deleting Galaxy</response>
        [HttpDelete("galaxy/{galaxyId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGalaxy(Guid galaxyId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteGalaxyAsync(galaxyId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Galaxy");
            }
        }



        /// <summary>
        /// Adds a SolarSystem to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="solarSystem">The SolarSystem to add.</param>
        /// <returns>The added SolarSystem.</returns>
        /// <response code="200">SolarSystem added successfully</response>
        /// <response code="400">Error adding SolarSystem</response>
        [HttpPost("galaxy/{parentGalaxyId}/solar-system")]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddSolarSystem(Guid parentGalaxyId, [FromBody] ISolarSystem solarSystem)
        {
            try
            {
                if (solarSystem == null)
                {
                    return BadRequest(new OASISResult<ISolarSystem>
                    {
                        IsError = true,
                        Message = "SolarSystem cannot be null. Please provide a valid SolarSystem object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISolarSystem>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddSolarSystemAsync(parentGalaxyId, solarSystem);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISolarSystem>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISolarSystem>(ex, "adding SolarSystem");
            }
        }

        /// <summary>
        /// Updates a SolarSystem.
        /// </summary>
        /// <param name="solarSystem">The SolarSystem to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SolarSystem.</returns>
        /// <response code="200">SolarSystem updated successfully</response>
        /// <response code="400">Error updating SolarSystem</response>
        [HttpPut("solar-system")]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSolarSystem([FromBody] ISolarSystem solarSystem, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (solarSystem == null)
                {
                    return BadRequest(new OASISResult<ISolarSystem>
                    {
                        IsError = true,
                        Message = "SolarSystem cannot be null. Please provide a valid SolarSystem object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISolarSystem>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateSolarSystemAsync(solarSystem, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISolarSystem>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISolarSystem>(ex, "updating SolarSystem");
            }
        }

        /// <summary>
        /// Deletes a SolarSystem.
        /// </summary>
        /// <param name="solarSystemId">The SolarSystem ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SolarSystem deleted successfully</response>
        /// <response code="400">Error deleting SolarSystem</response>
        [HttpDelete("solar-system/{solarSystemId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSolarSystem(Guid solarSystemId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSolarSystemAsync(solarSystemId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SolarSystem");
            }
        }
    }
}
