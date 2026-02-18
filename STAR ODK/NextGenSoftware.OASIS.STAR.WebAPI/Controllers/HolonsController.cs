using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Holon management endpoints for creating, updating, and managing STAR holons.
    /// Holons are the fundamental building blocks of the OASIS system - self-contained units that can contain other holons.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HolonsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all holons in the system.
        /// </summary>
        /// <returns>List of all holons available in the STAR system.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllHolons()
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, 0);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testHolons = TestDataHelper.GetTestSTARHolons(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARHolon>>(testHolons, "Holons retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = TestDataHelper.GetTestSTARHolons(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARHolon>>(testHolons, "Holons retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<STARHolon>>(ex, "GetAllHolons");
            }
        }

        /// <summary>
        /// Retrieves a specific holon by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to retrieve.</param>
        /// <returns>The requested holon details.</returns>
        /// <response code="200">Holon retrieved successfully</response>
        /// <response code="400">Error retrieving holon</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolon(Guid id)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testHolon = TestDataHelper.GetTestSTARHolon(id);
                    return Ok(TestDataHelper.CreateSuccessResult<STARHolon>(testHolon, "Holon retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolon = TestDataHelper.GetTestSTARHolon(id);
                    return Ok(TestDataHelper.CreateSuccessResult<STARHolon>(testHolon, "Holon retrieved successfully (using test data)"));
                }
                return HandleException<STARHolon>(ex, "GetHolon");
            }
        }

        /// <summary>
        /// Creates a new holon for the authenticated avatar.
        /// </summary>
        /// <param name="holon">The holon details to create.</param>
        /// <returns>The created holon with assigned ID and metadata.</returns>
        /// <response code="200">Holon created successfully</response>
        /// <response code="400">Error creating holon</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHolon([FromBody] STARHolon holon)
        {
            try
            {
                var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "creating holon");
            }
        }

        /// <summary>
        /// Updates an existing holon by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to update.</param>
        /// <param name="holon">The updated holon details.</param>
        /// <returns>The updated holon with modified data.</returns>
        /// <response code="200">Holon updated successfully</response>
        /// <response code="400">Error updating holon</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateHolon(Guid id, [FromBody] STARHolon holon)
        {
            try
            {
                holon.Id = id;
                var result = await _starAPI.Holons.UpdateAsync(AvatarId, holon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "updating holon");
            }
        }

        /// <summary>
        /// Deletes a holon by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Holon deleted successfully</response>
        /// <response code="400">Error deleting holon</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteHolon(Guid id)
        {
            try
            {
                var result = await _starAPI.Holons.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting holon");
            }
        }

        /// <summary>
        /// Retrieves all holons of a specific type.
        /// </summary>
        /// <param name="type">The type of holons to retrieve.</param>
        /// <returns>List of holons matching the specified type.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonsByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves all holons that belong to a specific parent holon.
        /// </summary>
        /// <param name="parentId">The unique identifier of the parent holon.</param>
        /// <returns>List of child holons for the specified parent.</returns>
        /// <response code="200">Child holons retrieved successfully</response>
        /// <response code="400">Error retrieving child holons</response>
        [HttpGet("by-parent/{parentId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonsByParent(Guid parentId)
        {
            try
            {
                throw new NotImplementedException("LoadAllForParentAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons for parent: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves all holons that match specific metadata criteria.
        /// </summary>
        /// <param name="key">The metadata key to search for.</param>
        /// <param name="value">The metadata value to match.</param>
        /// <returns>List of holons matching the specified metadata criteria.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons by metadata</response>
        [HttpGet("by-metadata")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonsByMetadata([FromQuery] string key, [FromQuery] string value)
        {
            try
            {
                throw new NotImplementedException("LoadAllByMetaDataAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons by metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches holons by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of holons matching the search query.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error searching holons</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchHolons([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredHolons = result.Result?.Where(h => 
                    h.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    h.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARHolon>>
                {
                    Result = filteredHolons,
                    IsError = false,
                    Message = "Holons retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error searching holons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves holons by a specific status.
        /// </summary>
        /// <param name="status">The holon status to filter by.</param>
        /// <returns>List of holons matching the specified status.</returns>
        /// <response code="200">Holons retrieved successfully</response>
        /// <response code="400">Error retrieving holons by status</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonsByStatus(string status)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredHolons = result.Result?.Where(h => h.Status?.ToString() == status);
                return Ok(new OASISResult<IEnumerable<STARHolon>>
                {
                    Result = filteredHolons,
                    IsError = false,
                    Message = "Holons retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error retrieving holons by status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new holon with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing holon details and source folder path.</param>
        /// <returns>Result of the holon creation operation.</returns>
        /// <response code="200">Holon created successfully</response>
        /// <response code="400">Error creating holon</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHolonWithOptions([FromBody] CreateHolonRequest request)
        {
            try
            {
                var result = await _starAPI.Holons.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "creating holon");
            }
        }

        /// <summary>
        /// Loads a holon by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to load.</param>
        /// <param name="version">The version of the holon to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested holon details.</returns>
        /// <response code="200">Holon loaded successfully</response>
        /// <response code="400">Error loading holon</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolon(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Holons.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "loading holon");
            }
        }

        /// <summary>
        /// Loads a holon from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded holon details.</returns>
        /// <response code="200">Holon loaded successfully</response>
        /// <response code="400">Error loading holon</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolonFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Holons.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "loading holon from path");
            }
        }

        /// <summary>
        /// Loads a holon from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published holon file.</param>
        /// <returns>The loaded holon details.</returns>
        /// <response code="200">Holon loaded successfully</response>
        /// <response code="400">Error loading holon</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolonFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Holons.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "loading holon from published file");
            }
        }

        /// <summary>
        /// Loads all holons for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of holons.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all holons for the avatar.</returns>
        /// <response code="200">Holons loaded successfully</response>
        /// <response code="400">Error loading holons</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllHolonsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Holons.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error loading holons for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a holon to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the holon publish operation.</returns>
        /// <response code="200">Holon published successfully</response>
        /// <response code="400">Error publishing holon</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishHolon(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Holons.PublishAsync(
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
                return HandleException<STARHolon>(ex, "publishing holon");
            }
        }

        /// <summary>
        /// Downloads a holon from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to download.</param>
        /// <param name="version">The version of the holon to download.</param>
        /// <param name="downloadPath">Optional path where the holon should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the holon download operation.</returns>
        /// <response code="200">Holon downloaded successfully</response>
        /// <response code="400">Error downloading holon</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadHolon(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Holons.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedSTARHolon>(ex, "downloading holon");
            }
        }

        /// <summary>
        /// Gets all versions of a specific holon.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to get versions for.</param>
        /// <returns>List of all versions of the specified holon.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Holons.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARHolon>>
                {
                    IsError = true,
                    Message = $"Error retrieving holon versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a holon.
        /// </summary>
        /// <param name="id">The unique identifier of the holon.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested holon version details.</returns>
        /// <response code="200">Holon version loaded successfully</response>
        /// <response code="400">Error loading holon version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolonVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Holons.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "loading holon version");
            }
        }

        /// <summary>
        /// Edits a holon with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the holon edit operation.</returns>
        /// <response code="200">Holon edited successfully</response>
        /// <response code="400">Error editing holon</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditHolon(Guid id, [FromBody] EditHolonRequest request)
        {
            try
            {
                var result = await _starAPI.Holons.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "editing holon");
            }
        }

        /// <summary>
        /// Unpublishes a holon from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to unpublish.</param>
        /// <param name="version">The version of the holon to unpublish.</param>
        /// <returns>Result of the holon unpublish operation.</returns>
        /// <response code="200">Holon unpublished successfully</response>
        /// <response code="400">Error unpublishing holon</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishHolon(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Holons.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "unpublishing holon");
            }
        }

        /// <summary>
        /// Republishes a holon to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to republish.</param>
        /// <param name="version">The version of the holon to republish.</param>
        /// <returns>Result of the holon republish operation.</returns>
        /// <response code="200">Holon republished successfully</response>
        /// <response code="400">Error republishing holon</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishHolon(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Holons.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "republishing holon");
            }
        }

        /// <summary>
        /// Activates a holon.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to activate.</param>
        /// <param name="version">The version of the holon to activate.</param>
        /// <returns>Result of the holon activation operation.</returns>
        /// <response code="200">Holon activated successfully</response>
        /// <response code="400">Error activating holon</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateHolon(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Holons.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "activating holon");
            }
        }

        /// <summary>
        /// Deactivates a holon.
        /// </summary>
        /// <param name="id">The unique identifier of the holon to deactivate.</param>
        /// <param name="version">The version of the holon to deactivate.</param>
        /// <returns>Result of the holon deactivation operation.</returns>
        /// <response code="200">Holon deactivated successfully</response>
        /// <response code="400">Error deactivating holon</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARHolon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateHolon(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Holons.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARHolon>(ex, "deactivating holon");
            }
        }
    }

    public class CreateHolonRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Holon;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<STARHolon, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditHolonRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadedSTARHolon
    {
        public STARHolon Holon { get; set; } = new STARHolon();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
