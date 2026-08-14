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
        /// Gets all solar systems for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of solar systems.</returns>
        /// <response code="200">Solar systems retrieved successfully</response>
        /// <response code="400">Error retrieving solar systems</response>
        [HttpGet("galaxy/{galaxyId}/solar-systems")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ISolarSystem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ISolarSystem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSolarSystemsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetSolarSystemsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ISolarSystem>>
                {
                    IsError = true,
                    Message = $"Error retrieving solar systems: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all stars for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of stars.</returns>
        /// <response code="200">Stars retrieved successfully</response>
        /// <response code="400">Error retrieving stars</response>
        [HttpGet("galaxy/{galaxyId}/stars")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IStar>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IStar>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStarsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetStarsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IStar>>
                {
                    IsError = true,
                    Message = $"Error retrieving stars: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all planets for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of planets.</returns>
        /// <response code="200">Planets retrieved successfully</response>
        /// <response code="400">Error retrieving planets</response>
        [HttpGet("galaxy/{galaxyId}/planets")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlanetsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetPlanetsForGalaxyAsync(galaxyId);
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

        /// <summary>
        /// Gets all moons for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of moons.</returns>
        /// <response code="200">Moons retrieved successfully</response>
        /// <response code="400">Error retrieving moons</response>
        [HttpGet("galaxy/{galaxyId}/moons")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IMoon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IMoon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMoonsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetMoonsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IMoon>>
                {
                    IsError = true,
                    Message = $"Error retrieving moons: {ex.Message}",
                    Exception = ex
                });
            }
        }



        /// <summary>
        /// Updates a Nebula.
        /// </summary>
        /// <param name="nebula">The Nebula to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Nebula.</returns>
        /// <response code="200">Nebula updated successfully</response>
        /// <response code="400">Error updating Nebula</response>
        [HttpPut("nebula")]
        [ProducesResponseType(typeof(OASISResult<INebula>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<INebula>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateNebula([FromBody] INebula nebula, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (nebula == null)
                {
                    return BadRequest(new OASISResult<INebula>
                    {
                        IsError = true,
                        Message = "Nebula cannot be null. Please provide a valid Nebula object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<INebula>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateNebulaAsync(nebula, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<INebula>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<INebula>(ex, "updating Nebula");
            }
        }

        /// <summary>
        /// Deletes a Nebula.
        /// </summary>
        /// <param name="nebulaId">The Nebula ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Nebula deleted successfully</response>
        /// <response code="400">Error deleting Nebula</response>
        [HttpDelete("nebula/{nebulaId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteNebula(Guid nebulaId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteNebulaAsync(nebulaId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Nebula");
            }
        }



        /// <summary>
        /// Updates a SuperVerse.
        /// </summary>
        /// <param name="superVerse">The SuperVerse to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SuperVerse.</returns>
        /// <response code="200">SuperVerse updated successfully</response>
        /// <response code="400">Error updating SuperVerse</response>
        [HttpPut("superverse")]
        [ProducesResponseType(typeof(OASISResult<ISuperVerse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISuperVerse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSuperVerse([FromBody] ISuperVerse superVerse, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (superVerse == null)
                {
                    return BadRequest(new OASISResult<ISuperVerse>
                    {
                        IsError = true,
                        Message = "SuperVerse cannot be null. Please provide a valid SuperVerse object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISuperVerse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateSuperVerseAsync(superVerse, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISuperVerse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISuperVerse>(ex, "updating SuperVerse");
            }
        }

        /// <summary>
        /// Deletes a SuperVerse.
        /// </summary>
        /// <param name="superVerseId">The SuperVerse ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SuperVerse deleted successfully</response>
        /// <response code="400">Error deleting SuperVerse</response>
        [HttpDelete("superverse/{superVerseId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSuperVerse(Guid superVerseId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSuperVerseAsync(superVerseId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SuperVerse");
            }
        }



        /// <summary>
        /// Updates a WormHole.
        /// </summary>
        /// <param name="wormHole">The WormHole to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated WormHole.</returns>
        /// <response code="200">WormHole updated successfully</response>
        /// <response code="400">Error updating WormHole</response>
        [HttpPut("wormhole")]
        [ProducesResponseType(typeof(OASISResult<IWormHole>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IWormHole>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateWormHole([FromBody] IWormHole wormHole, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (wormHole == null)
                {
                    return BadRequest(new OASISResult<IWormHole>
                    {
                        IsError = true,
                        Message = "WormHole cannot be null. Please provide a valid WormHole object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IWormHole>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateWormHoleAsync(wormHole, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IWormHole>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IWormHole>(ex, "updating WormHole");
            }
        }

        /// <summary>
        /// Deletes a WormHole.
        /// </summary>
        /// <param name="wormHoleId">The WormHole ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">WormHole deleted successfully</response>
        /// <response code="400">Error deleting WormHole</response>
        [HttpDelete("wormhole/{wormHoleId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteWormHole(Guid wormHoleId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteWormHoleAsync(wormHoleId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting WormHole");
            }
        }



        /// <summary>
        /// Updates a BlackHole.
        /// </summary>
        /// <param name="blackHole">The BlackHole to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated BlackHole.</returns>
        /// <response code="200">BlackHole updated successfully</response>
        /// <response code="400">Error updating BlackHole</response>
        [HttpPut("blackhole")]
        [ProducesResponseType(typeof(OASISResult<IBlackHole>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IBlackHole>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBlackHole([FromBody] IBlackHole blackHole, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (blackHole == null)
                {
                    return BadRequest(new OASISResult<IBlackHole>
                    {
                        IsError = true,
                        Message = "BlackHole cannot be null. Please provide a valid BlackHole object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IBlackHole>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateBlackHoleAsync(blackHole, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IBlackHole>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IBlackHole>(ex, "updating BlackHole");
            }
        }
    }
}
