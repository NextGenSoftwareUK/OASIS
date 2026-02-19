using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Geo-HotSpots management endpoints for creating, updating, and managing STAR geo-hotspots.
    /// Geo-hotspots represent geographical locations of interest, events, or activities within the OASIS Omniverse/Our World and can optionally contain AR content.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GeoHotSpotsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all geo hot spots in the system.
        /// </summary>
        /// <returns>List of all geo hot spots available in the STAR system.</returns>
        /// <response code="200">Geo hot spots retrieved successfully</response>
        /// <response code="400">Error retrieving geo hot spots</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllGeoHotSpots()
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadAllAsync(AvatarId, 0);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testHotSpots = new List<GeoHotSpot>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<GeoHotSpot>>(testHotSpots, "Geo hot spots retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHotSpots = new List<GeoHotSpot>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<GeoHotSpot>>(testHotSpots, "Geo hot spots retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<GeoHotSpot>>(ex, "GetAllGeoHotSpots");
            }
        }

        /// <summary>
        /// Retrieves a specific geo hot spot by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to retrieve.</param>
        /// <returns>The requested geo hot spot details.</returns>
        /// <response code="200">Geo hot spot retrieved successfully</response>
        /// <response code="400">Error retrieving geo hot spot</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoHotSpot(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    return Ok(TestDataHelper.CreateSuccessResult<GeoHotSpot>(null, "Geo hot spot retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(TestDataHelper.CreateSuccessResult<GeoHotSpot>(null, "Geo hot spot retrieved successfully (using test data)"));
                }
                return HandleException<GeoHotSpot>(ex, "GetGeoHotSpot");
            }
        }

        /// <summary>
        /// Creates a new geo hot spot for the authenticated avatar.
        /// </summary>
        /// <param name="hotSpot">The geo hot spot details to create.</param>
        /// <returns>The created geo hot spot with assigned ID and metadata.</returns>
        /// <response code="200">Geo hot spot created successfully</response>
        /// <response code="400">Error creating geo hot spot</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGeoHotSpot([FromBody] GeoHotSpot hotSpot)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.UpdateAsync(AvatarId, hotSpot);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "creating geo hot spot");
            }
        }

        /// <summary>
        /// Updates an existing geo hot spot by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to update.</param>
        /// <param name="hotSpot">The updated geo hot spot details.</param>
        /// <returns>The updated geo hot spot with modified data.</returns>
        /// <response code="200">Geo hot spot updated successfully</response>
        /// <response code="400">Error updating geo hot spot</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGeoHotSpot(Guid id, [FromBody] GeoHotSpot hotSpot)
        {
            try
            {
                hotSpot.Id = id;
                var result = await _starAPI.GeoHotSpots.UpdateAsync(AvatarId, hotSpot);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "updating geo hot spot");
            }
        }

        /// <summary>
        /// Deletes a geo hot spot by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Geo hot spot deleted successfully</response>
        /// <response code="400">Error deleting geo hot spot</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGeoHotSpot(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting geo hot spot");
            }
        }

        /// <summary>
        /// Retrieves geo hot spots within a specified radius of given coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate for the search center.</param>
        /// <param name="longitude">The longitude coordinate for the search center.</param>
        /// <param name="radiusKm">The search radius in kilometers (default: 10.0).</param>
        /// <returns>List of geo hot spots within the specified radius.</returns>
        /// <response code="200">Nearby geo hot spots retrieved successfully</response>
        /// <response code="400">Error retrieving nearby geo hot spots</response>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNearbyGeoHotSpots([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                throw new NotImplementedException("LoadAllNearAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error loading nearby geo hot spots: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new geo hot spot with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing geo hot spot details and source folder path.</param>
        /// <returns>Result of the geo hot spot creation operation.</returns>
        /// <response code="200">Geo hot spot created successfully</response>
        /// <response code="400">Error creating geo hot spot</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGeoHotSpotWithOptions([FromBody] CreateGeoHotSpotRequest request)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "creating geo hot spot");
            }
        }

        /// <summary>
        /// Loads a geo hot spot by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to load.</param>
        /// <param name="version">The version of the geo hot spot to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested geo hot spot details.</returns>
        /// <response code="200">Geo hot spot loaded successfully</response>
        /// <response code="400">Error loading geo hot spot</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoHotSpot(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<GeoHotSpot>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.GeoHotSpots.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "loading geo hot spot");
            }
        }

        /// <summary>
        /// Loads a geo hot spot from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded geo hot spot details.</returns>
        /// <response code="200">Geo hot spot loaded successfully</response>
        /// <response code="400">Error loading geo hot spot</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoHotSpotFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<GeoHotSpot>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.GeoHotSpots.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "loading geo hot spot from path");
            }
        }

        /// <summary>
        /// Loads a geo hot spot from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published geo hot spot file.</param>
        /// <returns>The loaded geo hot spot details.</returns>
        /// <response code="200">Geo hot spot loaded successfully</response>
        /// <response code="400">Error loading geo hot spot</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoHotSpotFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "loading geo hot spot from published file");
            }
        }

        /// <summary>
        /// Loads all geo hot spots for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of geo hot spots.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all geo hot spots for the avatar.</returns>
        /// <response code="200">Geo hot spots loaded successfully</response>
        /// <response code="400">Error loading geo hot spots</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllGeoHotSpotsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error loading geo hot spots for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a geo hot spot to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the geo hot spot publish operation.</returns>
        /// <response code="200">Geo hot spot published successfully</response>
        /// <response code="400">Error publishing geo hot spot</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishGeoHotSpot(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.PublishAsync(
                    AvatarId, 
                    request.SourcePath, 
                    request.LaunchTarget, 
                    request.PublishPath, 
                    request.Edit, 
                    request.RegisterOnSTARNET, 
                    request.GenerateBinary, 
                    request.UploadToCloud
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "publishing geo hot spot");
            }
        }

        /// <summary>
        /// Downloads a geo hot spot from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to download.</param>
        /// <param name="version">The version of the geo hot spot to download.</param>
        /// <param name="downloadPath">Optional path where the geo hot spot should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the geo hot spot download operation.</returns>
        /// <response code="200">Geo hot spot downloaded successfully</response>
        /// <response code="400">Error downloading geo hot spot</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedGeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedGeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadGeoHotSpot(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedGeoHotSpot>(ex, "downloading geo hot spot");
            }
        }

        /// <summary>
        /// Gets all versions of a specific geo hot spot.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to get versions for.</param>
        /// <returns>List of all versions of the specified geo hot spot.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GeoHotSpot>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoHotSpotVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GeoHotSpot>>
                {
                    IsError = true,
                    Message = $"Error retrieving geo hot spot versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a geo hot spot.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested geo hot spot version details.</returns>
        /// <response code="200">Geo hot spot version loaded successfully</response>
        /// <response code="400">Error loading geo hot spot version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoHotSpotVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "loading geo hot spot version");
            }
        }

        /// <summary>
        /// Edits a geo hot spot with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the geo hot spot edit operation.</returns>
        /// <response code="200">Geo hot spot edited successfully</response>
        /// <response code="400">Error editing geo hot spot</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditGeoHotSpot(Guid id, [FromBody] EditGeoHotSpotRequest request)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "editing geo hot spot");
            }
        }

        /// <summary>
        /// Unpublishes a geo hot spot from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to unpublish.</param>
        /// <param name="version">The version of the geo hot spot to unpublish.</param>
        /// <returns>Result of the geo hot spot unpublish operation.</returns>
        /// <response code="200">Geo hot spot unpublished successfully</response>
        /// <response code="400">Error unpublishing geo hot spot</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishGeoHotSpot(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "unpublishing geo hot spot");
            }
        }

        /// <summary>
        /// Republishes a geo hot spot to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to republish.</param>
        /// <param name="version">The version of the geo hot spot to republish.</param>
        /// <returns>Result of the geo hot spot republish operation.</returns>
        /// <response code="200">Geo hot spot republished successfully</response>
        /// <response code="400">Error republishing geo hot spot</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishGeoHotSpot(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "republishing geo hot spot");
            }
        }

        /// <summary>
        /// Activates a geo hot spot.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to activate.</param>
        /// <param name="version">The version of the geo hot spot to activate.</param>
        /// <returns>Result of the geo hot spot activation operation.</returns>
        /// <response code="200">Geo hot spot activated successfully</response>
        /// <response code="400">Error activating geo hot spot</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateGeoHotSpot(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "activating geo hot spot");
            }
        }

        /// <summary>
        /// Deactivates a geo hot spot.
        /// </summary>
        /// <param name="id">The unique identifier of the geo hot spot to deactivate.</param>
        /// <param name="version">The version of the geo hot spot to deactivate.</param>
        /// <returns>Result of the geo hot spot deactivation operation.</returns>
        /// <response code="200">Geo hot spot deactivated successfully</response>
        /// <response code="400">Error deactivating geo hot spot</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<GeoHotSpot>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateGeoHotSpot(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoHotSpots.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<GeoHotSpot>(ex, "deactivating geo hot spot");
            }
        }
    }

    public class CreateGeoHotSpotRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.GeoHotSpot;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<GeoHotSpot, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditGeoHotSpotRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }


    public class DownloadedGeoHotSpot
    {
        public GeoHotSpot GeoHotSpot { get; set; } = new GeoHotSpot();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
