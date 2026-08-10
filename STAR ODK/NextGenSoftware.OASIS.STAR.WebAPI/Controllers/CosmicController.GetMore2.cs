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

        /// <summary>
        /// Deletes a TemporalRift.
        /// </summary>
        /// <param name="riftId">The TemporalRift ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">TemporalRift deleted successfully</response>
        /// <response code="400">Error deleting TemporalRift</response>
        [HttpDelete("temporal-rift/{riftId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteTemporalRift(Guid riftId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteTemporalRiftAsync(riftId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting TemporalRift");
            }
        }



        /// <summary>
        /// Updates a StarDust.
        /// </summary>
        /// <param name="starDust">The StarDust to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated StarDust.</returns>
        /// <response code="200">StarDust updated successfully</response>
        /// <response code="400">Error updating StarDust</response>
        [HttpPut("stardust")]
        [ProducesResponseType(typeof(OASISResult<IStarDust>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStarDust>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStarDust([FromBody] IStarDust starDust, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (starDust == null)
                {
                    return BadRequest(new OASISResult<IStarDust>
                    {
                        IsError = true,
                        Message = "StarDust cannot be null. Please provide a valid StarDust object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStarDust>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateStarDustAsync(starDust, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStarDust>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStarDust>(ex, "updating StarDust");
            }
        }

        /// <summary>
        /// Deletes a StarDust.
        /// </summary>
        /// <param name="starDustId">The StarDust ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">StarDust deleted successfully</response>
        /// <response code="400">Error deleting StarDust</response>
        [HttpDelete("stardust/{starDustId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteStarDust(Guid starDustId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteStarDustAsync(starDustId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting StarDust");
            }
        }



        /// <summary>
        /// Updates a CosmicWave.
        /// </summary>
        /// <param name="wave">The CosmicWave to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated CosmicWave.</returns>
        /// <response code="200">CosmicWave updated successfully</response>
        /// <response code="400">Error updating CosmicWave</response>
        [HttpPut("cosmic-wave")]
        [ProducesResponseType(typeof(OASISResult<ICosmicWave>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ICosmicWave>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCosmicWave([FromBody] ICosmicWave wave, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (wave == null)
                {
                    return BadRequest(new OASISResult<ICosmicWave>
                    {
                        IsError = true,
                        Message = "CosmicWave cannot be null. Please provide a valid CosmicWave object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ICosmicWave>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateCosmicWaveAsync(wave, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ICosmicWave>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ICosmicWave>(ex, "updating CosmicWave");
            }
        }

        /// <summary>
        /// Deletes a CosmicWave.
        /// </summary>
        /// <param name="waveId">The CosmicWave ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">CosmicWave deleted successfully</response>
        /// <response code="400">Error deleting CosmicWave</response>
        [HttpDelete("cosmic-wave/{waveId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCosmicWave(Guid waveId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteCosmicWaveAsync(waveId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting CosmicWave");
            }
        }



        /// <summary>
        /// Updates a CosmicRay.
        /// </summary>
        /// <param name="ray">The CosmicRay to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated CosmicRay.</returns>
        /// <response code="200">CosmicRay updated successfully</response>
        /// <response code="400">Error updating CosmicRay</response>
        [HttpPut("cosmic-ray")]
        [ProducesResponseType(typeof(OASISResult<ICosmicRay>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ICosmicRay>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCosmicRay([FromBody] ICosmicRay ray, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (ray == null)
                {
                    return BadRequest(new OASISResult<ICosmicRay>
                    {
                        IsError = true,
                        Message = "CosmicRay cannot be null. Please provide a valid CosmicRay object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ICosmicRay>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateCosmicRayAsync(ray, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ICosmicRay>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ICosmicRay>(ex, "updating CosmicRay");
            }
        }

        /// <summary>
        /// Deletes a CosmicRay.
        /// </summary>
        /// <param name="rayId">The CosmicRay ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">CosmicRay deleted successfully</response>
        /// <response code="400">Error deleting CosmicRay</response>
        [HttpDelete("cosmic-ray/{rayId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCosmicRay(Guid rayId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteCosmicRayAsync(rayId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting CosmicRay");
            }
        }



        /// <summary>
        /// Updates a GravitationalWave.
        /// </summary>
        /// <param name="wave">The GravitationalWave to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated GravitationalWave.</returns>
        /// <response code="200">GravitationalWave updated successfully</response>
        /// <response code="400">Error updating GravitationalWave</response>
        [HttpPut("gravitational-wave")]
        [ProducesResponseType(typeof(OASISResult<IGravitationalWave>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGravitationalWave>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGravitationalWave([FromBody] IGravitationalWave wave, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (wave == null)
                {
                    return BadRequest(new OASISResult<IGravitationalWave>
                    {
                        IsError = true,
                        Message = "GravitationalWave cannot be null. Please provide a valid GravitationalWave object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGravitationalWave>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateGravitationalWaveAsync(wave, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGravitationalWave>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGravitationalWave>(ex, "updating GravitationalWave");
            }
        }

        /// <summary>
        /// Deletes a GravitationalWave.
        /// </summary>
        /// <param name="waveId">The GravitationalWave ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">GravitationalWave deleted successfully</response>
        /// <response code="400">Error deleting GravitationalWave</response>
        [HttpDelete("gravitational-wave/{waveId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGravitationalWave(Guid waveId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteGravitationalWaveAsync(waveId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting GravitationalWave");
            }
        }

    }
}
