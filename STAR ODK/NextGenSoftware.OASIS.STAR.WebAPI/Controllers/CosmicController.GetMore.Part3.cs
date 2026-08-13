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
        /// Deletes a Moon.
        /// </summary>
        /// <param name="moonId">The Moon ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Moon deleted successfully</response>
        /// <response code="400">Error deleting Moon</response>
        [HttpDelete("moon/{moonId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMoon(Guid moonId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteMoonAsync(moonId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Moon");
            }
        }



        /// <summary>
        /// Adds an Asteroid to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="asteroid">The Asteroid to add.</param>
        /// <returns>The added Asteroid.</returns>
        /// <response code="200">Asteroid added successfully</response>
        /// <response code="400">Error adding Asteroid</response>
        [HttpPost("galaxy/{parentGalaxyId}/asteroid")]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddAsteroid(Guid parentGalaxyId, [FromBody] IAsteroid asteroid)
        {
            try
            {
                if (asteroid == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = "Asteroid cannot be null. Please provide a valid Asteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IAsteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var galaxyLoadResult = await CosmicManager.Data.LoadHolonAsync(parentGalaxyId, childHolonType: HolonType.Galaxy);
                if (galaxyLoadResult.IsError || galaxyLoadResult.Result == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = $"Error loading galaxy: {galaxyLoadResult.Message}"
                    });
                }
                var galaxy = galaxyLoadResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = "Loaded holon is not a galaxy"
                    });
                }
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddAsteroidAsync(galaxy, asteroid);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IAsteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IAsteroid>(ex, "adding Asteroid");
            }
        }

        /// <summary>
        /// Updates an Asteroid.
        /// </summary>
        /// <param name="asteroid">The Asteroid to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Asteroid.</returns>
        /// <response code="200">Asteroid updated successfully</response>
        /// <response code="400">Error updating Asteroid</response>
        [HttpPut("asteroid")]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAsteroid([FromBody] IAsteroid asteroid, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (asteroid == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = "Asteroid cannot be null. Please provide a valid Asteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IAsteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateAsteroidAsync(asteroid, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IAsteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IAsteroid>(ex, "updating Asteroid");
            }
        }

        /// <summary>
        /// Deletes an Asteroid.
        /// </summary>
        /// <param name="asteroidId">The Asteroid ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Asteroid deleted successfully</response>
        /// <response code="400">Error deleting Asteroid</response>
        [HttpDelete("asteroid/{asteroidId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAsteroid(Guid asteroidId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteAsteroidAsync(asteroidId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Asteroid");
            }
        }



        /// <summary>
        /// Adds a Comet to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="comet">The Comet to add.</param>
        /// <returns>The added Comet.</returns>
        /// <response code="200">Comet added successfully</response>
        /// <response code="400">Error adding Comet</response>
        [HttpPost("galaxy/{parentGalaxyId}/comet")]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddComet(Guid parentGalaxyId, [FromBody] IComet comet)
        {
            try
            {
                if (comet == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = "Comet cannot be null. Please provide a valid Comet object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IComet>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var galaxyLoadResult = await CosmicManager.Data.LoadHolonAsync(parentGalaxyId, childHolonType: HolonType.Galaxy);
                if (galaxyLoadResult.IsError || galaxyLoadResult.Result == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = $"Error loading galaxy: {galaxyLoadResult.Message}"
                    });
                }
                var galaxy = galaxyLoadResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = "Loaded holon is not a galaxy"
                    });
                }
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddCometAsync(galaxy, comet);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IComet>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IComet>(ex, "adding Comet");
            }
        }

        /// <summary>
        /// Updates a Comet.
        /// </summary>
        /// <param name="comet">The Comet to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Comet.</returns>
        /// <response code="200">Comet updated successfully</response>
        /// <response code="400">Error updating Comet</response>
        [HttpPut("comet")]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateComet([FromBody] IComet comet, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (comet == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = "Comet cannot be null. Please provide a valid Comet object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IComet>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateCometAsync(comet, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IComet>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IComet>(ex, "updating Comet");
            }
        }

        /// <summary>
        /// Deletes a Comet.
        /// </summary>
        /// <param name="cometId">The Comet ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Comet deleted successfully</response>
        /// <response code="400">Error deleting Comet</response>
        [HttpDelete("comet/{cometId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteComet(Guid cometId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteCometAsync(cometId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Comet");
            }
        }



        /// <summary>
        /// Adds a Meteroid to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="meteroid">The Meteroid to add.</param>
        /// <returns>The added Meteroid.</returns>
        /// <response code="200">Meteroid added successfully</response>
        /// <response code="400">Error adding Meteroid</response>
        [HttpPost("galaxy/{parentGalaxyId}/meteroid")]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMeteroid(Guid parentGalaxyId, [FromBody] IMeteroid meteroid)
        {
            try
            {
                if (meteroid == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = "Meteroid cannot be null. Please provide a valid Meteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMeteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var galaxyLoadResult = await CosmicManager.Data.LoadHolonAsync(parentGalaxyId, childHolonType: HolonType.Galaxy);
                if (galaxyLoadResult.IsError || galaxyLoadResult.Result == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = $"Error loading galaxy: {galaxyLoadResult.Message}"
                    });
                }
                var galaxy = galaxyLoadResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = "Loaded holon is not a galaxy"
                    });
                }
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddMeteroidAsync(galaxy, meteroid);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMeteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMeteroid>(ex, "adding Meteroid");
            }
        }

        /// <summary>
        /// Updates a Meteroid.
        /// </summary>
        /// <param name="meteroid">The Meteroid to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Meteroid.</returns>
        /// <response code="200">Meteroid updated successfully</response>
        /// <response code="400">Error updating Meteroid</response>
        [HttpPut("meteroid")]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMeteroid([FromBody] IMeteroid meteroid, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (meteroid == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = "Meteroid cannot be null. Please provide a valid Meteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMeteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateMeteroidAsync(meteroid, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMeteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMeteroid>(ex, "updating Meteroid");
            }
        }

        /// <summary>
        /// Deletes a Meteroid.
        /// </summary>
        /// <param name="meteroidId">The Meteroid ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Meteroid deleted successfully</response>
        /// <response code="400">Error deleting Meteroid</response>
        [HttpDelete("meteroid/{meteroidId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMeteroid(Guid meteroidId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteMeteroidAsync(meteroidId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Meteroid");
            }
        }



        /// <summary>
        /// Gets all planets for a solar system.
        /// </summary>
        /// <param name="solarSystemId">The SolarSystem ID.</param>
        /// <returns>List of planets.</returns>
        /// <response code="200">Planets retrieved successfully</response>
        /// <response code="400">Error retrieving planets</response>
        [HttpGet("solar-system/{solarSystemId}/planets")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlanetsForSolarSystem(Guid solarSystemId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetPlanetsForSolarSystemAsync(solarSystemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IPlanet>>
                {
                    IsError = true,
                    Message = $"Error retrieving planets: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }
}
