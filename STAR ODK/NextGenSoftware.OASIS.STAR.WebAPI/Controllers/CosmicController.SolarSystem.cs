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



        /// <summary>
        /// Adds a Star to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="star">The Star to add.</param>
        /// <returns>The added Star.</returns>
        /// <response code="200">Star added successfully</response>
        /// <response code="400">Error adding Star</response>
        [HttpPost("galaxy/{parentGalaxyId}/star")]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddStar(Guid parentGalaxyId, [FromBody] IStar star)
        {
            try
            {
                if (star == null)
                {
                    return BadRequest(new OASISResult<IStar>
                    {
                        IsError = true,
                        Message = "Star cannot be null. Please provide a valid Star object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStar>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddStarAsync(parentGalaxyId, star);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStar>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStar>(ex, "adding Star");
            }
        }

        /// <summary>
        /// Updates a Star.
        /// </summary>
        /// <param name="star">The Star to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Star.</returns>
        /// <response code="200">Star updated successfully</response>
        /// <response code="400">Error updating Star</response>
        [HttpPut("star")]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStar([FromBody] IStar star, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (star == null)
                {
                    return BadRequest(new OASISResult<IStar>
                    {
                        IsError = true,
                        Message = "Star cannot be null. Please provide a valid Star object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStar>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateStarAsync(star, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStar>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStar>(ex, "updating Star");
            }
        }

        /// <summary>
        /// Deletes a Star.
        /// </summary>
        /// <param name="starId">The Star ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Star deleted successfully</response>
        /// <response code="400">Error deleting Star</response>
        [HttpDelete("star/{starId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteStar(Guid starId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteStarAsync(starId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Star");
            }
        }



        /// <summary>
        /// Adds a Planet to a SolarSystem.
        /// </summary>
        /// <param name="parentSolarSystemId">The parent SolarSystem ID.</param>
        /// <param name="planet">The Planet to add.</param>
        /// <returns>The added Planet.</returns>
        /// <response code="200">Planet added successfully</response>
        /// <response code="400">Error adding Planet</response>
        [HttpPost("solar-system/{parentSolarSystemId}/planet")]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddPlanet(Guid parentSolarSystemId, [FromBody] IPlanet planet)
        {
            try
            {
                if (planet == null)
                {
                    return BadRequest(new OASISResult<IPlanet>
                    {
                        IsError = true,
                        Message = "Planet cannot be null. Please provide a valid Planet object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IPlanet>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddPlanetAsync(parentSolarSystemId, planet);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IPlanet>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IPlanet>(ex, "adding Planet");
            }
        }

        /// <summary>
        /// Updates a Planet.
        /// </summary>
        /// <param name="planet">The Planet to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Planet.</returns>
        /// <response code="200">Planet updated successfully</response>
        /// <response code="400">Error updating Planet</response>
        [HttpPut("planet")]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePlanet([FromBody] IPlanet planet, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdatePlanetAsync(planet, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<IPlanet>(ex, "updating Planet");
            }
        }

        /// <summary>
        /// Deletes a Planet.
        /// </summary>
        /// <param name="planetId">The Planet ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Planet deleted successfully</response>
        /// <response code="400">Error deleting Planet</response>
        [HttpDelete("planet/{planetId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeletePlanet(Guid planetId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeletePlanetAsync(planetId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Planet");
            }
        }



        /// <summary>
        /// Adds a Moon to a Planet.
        /// </summary>
        /// <param name="parentPlanetId">The parent Planet ID.</param>
        /// <param name="moon">The Moon to add.</param>
        /// <returns>The added Moon.</returns>
        /// <response code="200">Moon added successfully</response>
        /// <response code="400">Error adding Moon</response>
        [HttpPost("planet/{parentPlanetId}/moon")]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMoon(Guid parentPlanetId, [FromBody] IMoon moon)
        {
            try
            {
                if (moon == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = "Moon cannot be null. Please provide a valid Moon object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMoon>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var planetLoad = await CosmicManager.Data.LoadHolonAsync(parentPlanetId);
                if (planetLoad.IsError || planetLoad.Result == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = $"Error loading parent planet: {planetLoad.Message}"
                    });
                }

                var planet = planetLoad.Result as IPlanet;
                if (planet == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = "Parent holon is not a Planet"
                    });
                }

                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddMoonAsync(planet, moon);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMoon>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMoon>(ex, "adding Moon");
            }
        }

        /// <summary>
        /// Updates a Moon.
        /// </summary>
        /// <param name="moon">The Moon to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Moon.</returns>
        /// <response code="200">Moon updated successfully</response>
        /// <response code="400">Error updating Moon</response>
        [HttpPut("moon")]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMoon([FromBody] IMoon moon, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (moon == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = "Moon cannot be null. Please provide a valid Moon object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMoon>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateMoonAsync(moon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMoon>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMoon>(ex, "updating Moon");
            }
        }

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
    }
}