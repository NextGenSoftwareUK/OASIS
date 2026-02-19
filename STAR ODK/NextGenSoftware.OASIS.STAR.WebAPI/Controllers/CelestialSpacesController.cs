using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Celestial Spaces management endpoints for creating, updating, and managing STAR celestial spaces.
    /// Celestial spaces represent Solar Systems's, Galxies, Universes etc within the OASIS Omniverse that contain celestial bodies.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CelestialSpacesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all celestial spaces in the system.
        /// </summary>
        /// <returns>List of all celestial spaces available in the STAR system.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error retrieving celestial spaces</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllCelestialSpaces()
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    // Create test celestial spaces - using empty list for now as STARCelestialSpace type may need specific implementation
                    var testSpaces = new List<STARCelestialSpace>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARCelestialSpace>>(testSpaces, "Celestial spaces retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testSpaces = new List<STARCelestialSpace>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARCelestialSpace>>(testSpaces, "Celestial spaces retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<STARCelestialSpace>>(ex, "GetAllCelestialSpaces");
            }
        }

        /// <summary>
        /// Retrieves a specific celestial space by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to retrieve.</param>
        /// <returns>The requested celestial space details.</returns>
        /// <response code="200">Celestial space retrieved successfully</response>
        /// <response code="400">Error retrieving celestial space</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpace(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    // Create test celestial space - using null for now as STARCelestialSpace type may need specific implementation
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialSpace>(null, "Celestial space retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialSpace>(null, "Celestial space retrieved successfully (using test data)"));
                }
                return HandleException<STARCelestialSpace>(ex, "GetCelestialSpace");
            }
        }

        /// <summary>
        /// Creates a new celestial space for the authenticated avatar.
        /// </summary>
        /// <param name="celestialSpace">The celestial space details to create.</param>
        /// <returns>The created celestial space with assigned ID and metadata.</returns>
        /// <response code="200">Celestial space created successfully</response>
        /// <response code="400">Error creating celestial space</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialSpace([FromBody] STARCelestialSpace celestialSpace)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.UpdateAsync(AvatarId, celestialSpace);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "creating celestial space");
            }
        }

        /// <summary>
        /// Updates an existing celestial space by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to update.</param>
        /// <param name="celestialSpace">The updated celestial space details.</param>
        /// <returns>The updated celestial space with modified data.</returns>
        /// <response code="200">Celestial space updated successfully</response>
        /// <response code="400">Error updating celestial space</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCelestialSpace(Guid id, [FromBody] STARCelestialSpace celestialSpace)
        {
            try
            {
                celestialSpace.Id = id;
                var result = await _starAPI.CelestialSpaces.UpdateAsync(AvatarId, celestialSpace);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "updating celestial space");
            }
        }

        /// <summary>
        /// Deletes a celestial space by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Celestial space deleted successfully</response>
        /// <response code="400">Error deleting celestial space</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCelestialSpace(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting celestial space");
            }
        }

        /// <summary>
        /// Retrieves celestial spaces by a specific type.
        /// </summary>
        /// <param name="type">The celestial space type to filter by.</param>
        /// <returns>List of celestial spaces matching the specified type.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error retrieving celestial spaces by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpacesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves celestial spaces within a specific parent celestial space.
        /// </summary>
        /// <param name="parentSpaceId">The unique identifier of the parent celestial space.</param>
        /// <returns>List of celestial spaces within the specified parent space.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error retrieving celestial spaces in parent space</response>
        [HttpGet("in-space/{parentSpaceId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpacesInSpace(Guid parentSpaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces in parent space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches celestial spaces by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of celestial spaces matching the search query.</returns>
        /// <response code="200">Celestial spaces retrieved successfully</response>
        /// <response code="400">Error searching celestial spaces</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchCelestialSpaces([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredCelestialSpaces = result.Result?.Where(cs => 
                    cs.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    cs.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    Result = filteredCelestialSpaces,
                    IsError = false,
                    Message = "Celestial spaces retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error searching celestial spaces: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new celestial space with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing celestial space details and source folder path.</param>
        /// <returns>Result of the celestial space creation operation.</returns>
        /// <response code="200">Celestial space created successfully</response>
        /// <response code="400">Error creating celestial space</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialSpaceWithOptions([FromBody] CreateCelestialSpaceRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "creating celestial space");
            }
        }

        /// <summary>
        /// Loads a celestial space by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to load.</param>
        /// <param name="version">The version of the celestial space to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested celestial space details.</returns>
        /// <response code="200">Celestial space loaded successfully</response>
        /// <response code="400">Error loading celestial space</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialSpace(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<STARCelestialSpace>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.CelestialSpaces.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "loading celestial space");
            }
        }

        /// <summary>
        /// Loads a celestial space from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded celestial space details.</returns>
        /// <response code="200">Celestial space loaded successfully</response>
        /// <response code="400">Error loading celestial space</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialSpaceFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<STARCelestialSpace>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.CelestialSpaces.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "loading celestial space from path");
            }
        }

        /// <summary>
        /// Loads a celestial space from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published celestial space file.</param>
        /// <returns>The loaded celestial space details.</returns>
        /// <response code="200">Celestial space loaded successfully</response>
        /// <response code="400">Error loading celestial space</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialSpaceFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "loading celestial space from published file");
            }
        }

        /// <summary>
        /// Loads all celestial spaces for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of celestial spaces.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all celestial spaces for the avatar.</returns>
        /// <response code="200">Celestial spaces loaded successfully</response>
        /// <response code="400">Error loading celestial spaces</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllCelestialSpacesForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error loading celestial spaces for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a celestial space to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the celestial space publish operation.</returns>
        /// <response code="200">Celestial space published successfully</response>
        /// <response code="400">Error publishing celestial space</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishCelestialSpace(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.PublishAsync(
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
                return HandleException<STARCelestialSpace>(ex, "publishing celestial space");
            }
        }

        /// <summary>
        /// Downloads a celestial space from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to download.</param>
        /// <param name="version">The version of the celestial space to download.</param>
        /// <param name="downloadPath">Optional path where the celestial space should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the celestial space download operation.</returns>
        /// <response code="200">Celestial space downloaded successfully</response>
        /// <response code="400">Error downloading celestial space</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadCelestialSpace(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedSTARCelestialSpace>(ex, "downloading celestial space");
            }
        }

        /// <summary>
        /// Gets all versions of a specific celestial space.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to get versions for.</param>
        /// <returns>List of all versions of the specified celestial space.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialSpace>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialSpaceVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialSpace>>
                {
                    IsError = true,
                    Message = $"Error retrieving celestial space versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a celestial space.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested celestial space version details.</returns>
        /// <response code="200">Celestial space version loaded successfully</response>
        /// <response code="400">Error loading celestial space version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialSpaceVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "loading celestial space version");
            }
        }

        /// <summary>
        /// Edits a celestial space with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the celestial space edit operation.</returns>
        /// <response code="200">Celestial space edited successfully</response>
        /// <response code="400">Error editing celestial space</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditCelestialSpace(Guid id, [FromBody] EditCelestialSpaceRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "editing celestial space");
            }
        }

        /// <summary>
        /// Unpublishes a celestial space from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to unpublish.</param>
        /// <param name="version">The version of the celestial space to unpublish.</param>
        /// <returns>Result of the celestial space unpublish operation.</returns>
        /// <response code="200">Celestial space unpublished successfully</response>
        /// <response code="400">Error unpublishing celestial space</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishCelestialSpace(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "unpublishing celestial space");
            }
        }

        /// <summary>
        /// Republishes a celestial space to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to republish.</param>
        /// <param name="version">The version of the celestial space to republish.</param>
        /// <returns>Result of the celestial space republish operation.</returns>
        /// <response code="200">Celestial space republished successfully</response>
        /// <response code="400">Error republishing celestial space</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishCelestialSpace(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "republishing celestial space");
            }
        }

        /// <summary>
        /// Activates a celestial space.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to activate.</param>
        /// <param name="version">The version of the celestial space to activate.</param>
        /// <returns>Result of the celestial space activation operation.</returns>
        /// <response code="200">Celestial space activated successfully</response>
        /// <response code="400">Error activating celestial space</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateCelestialSpace(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "activating celestial space");
            }
        }

        /// <summary>
        /// Deactivates a celestial space.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial space to deactivate.</param>
        /// <param name="version">The version of the celestial space to deactivate.</param>
        /// <returns>Result of the celestial space deactivation operation.</returns>
        /// <response code="200">Celestial space deactivated successfully</response>
        /// <response code="400">Error deactivating celestial space</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialSpace>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateCelestialSpace(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialSpaces.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialSpace>(ex, "deactivating celestial space");
            }
        }
    }

    public class CreateCelestialSpaceRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.STARCelestialSpace;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<STARCelestialSpace, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditCelestialSpaceRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadedSTARCelestialSpace
    {
        public STARCelestialSpace CelestialSpace { get; set; } = new STARCelestialSpace();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
