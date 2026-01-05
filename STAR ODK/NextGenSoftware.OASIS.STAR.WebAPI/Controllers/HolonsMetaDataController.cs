using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Holons Metadata management endpoints for creating, updating, and managing STAR Holons Metadata.
    /// Holons Metadata represent configuration and metadata for holons (data objects) in the OASIS ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HolonsMetaDataController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all Holons Metadata in the system.
        /// </summary>
        /// <returns>List of all Holons Metadata available in the STAR system.</returns>
        /// <response code="200">Holons Metadata retrieved successfully</response>
        /// <response code="400">Error retrieving Holons Metadata</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllHolonsMetaData()
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<HolonMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading Holons Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific Holon Metadata by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to retrieve.</param>
        /// <returns>The requested Holon Metadata details.</returns>
        /// <response code="200">Holon Metadata retrieved successfully</response>
        /// <response code="400">Error retrieving Holon Metadata</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonMetaData(Guid id)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new Holon Metadata for the authenticated avatar.
        /// </summary>
        /// <param name="metadata">The Holon Metadata details to create.</param>
        /// <returns>The created Holon Metadata with assigned ID and metadata.</returns>
        /// <response code="200">Holon Metadata created successfully</response>
        /// <response code="400">Error creating Holon Metadata</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHolonMetaData([FromBody] HolonMetaDataDNA metadata)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.UpdateAsync(AvatarId, metadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error creating Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to update.</param>
        /// <param name="metadata">The updated Holon Metadata details.</param>
        /// <returns>The updated Holon Metadata.</returns>
        /// <response code="200">Holon Metadata updated successfully</response>
        /// <response code="400">Error updating Holon Metadata</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateHolonMetaData(Guid id, [FromBody] HolonMetaDataDNA metadata)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.UpdateAsync(AvatarId, metadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error updating Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to delete.</param>
        /// <returns>Success status of the deletion operation.</returns>
        /// <response code="200">Holon Metadata deleted successfully</response>
        /// <response code="400">Error deleting Holon Metadata</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteHolonMetaData(Guid id)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Clones an existing Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to clone.</param>
        /// <param name="request">The clone request details.</param>
        /// <returns>The cloned Holon Metadata.</returns>
        /// <response code="200">Holon Metadata cloned successfully</response>
        /// <response code="400">Error cloning Holon Metadata</response>
        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneHolonMetaData(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a Holon Metadata to the STARNET.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to publish.</param>
        /// <param name="request">The publish request details.</param>
        /// <returns>The published Holon Metadata.</returns>
        /// <response code="200">Holon Metadata published successfully</response>
        /// <response code="400">Error publishing Holon Metadata</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishHolonMetaData(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.PublishAsync(AvatarId, request.SourcePath, request.LaunchTarget, request.PublishPath, request.Edit, request.RegisterOnSTARNET, request.GenerateBinary, request.UploadToCloud);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error publishing Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for Holons Metadata based on search criteria.
        /// </summary>
        /// <param name="searchTerm">The search term to look for.</param>
        /// <param name="showAllVersions">Whether to show all versions of the results.</param>
        /// <param name="version">The version to search for.</param>
        /// <returns>List of matching Holons Metadata.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchHolonsMetaData([FromQuery] string searchTerm, [FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.SearchAsync<HolonMetaDataDNA>(avatarId: AvatarId, searchTerm: searchTerm, searchOnlyForCurrentAvatar: true, showAllVersions: showAllVersions, version: version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<HolonMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error searching Holons Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves all versions of a specific Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata.</param>
        /// <returns>List of all versions of the Holon Metadata.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHolonMetaDataVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<HolonMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new Holon Metadata with advanced options.
        /// </summary>
        /// <param name="request">The create request with options.</param>
        /// <returns>The created Holon Metadata.</returns>
        /// <response code="200">Holon Metadata created successfully</response>
        /// <response code="400">Error creating Holon Metadata</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHolonMetaDataWithOptions([FromBody] CreateHolonMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error creating Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a Holon Metadata from a file path.
        /// </summary>
        /// <param name="path">The file path to load from.</param>
        /// <returns>The loaded Holon Metadata.</returns>
        /// <response code="200">Holon Metadata loaded successfully</response>
        /// <response code="400">Error loading Holon Metadata</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolonMetaDataFromPath([FromQuery] string path)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadForSourceOrInstalledFolderAsync(AvatarId, path);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Holon Metadata from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a Holon Metadata from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The published file path to load from.</param>
        /// <returns>The loaded Holon Metadata.</returns>
        /// <response code="200">Holon Metadata loaded successfully</response>
        /// <response code="400">Error loading Holon Metadata</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolonMetaDataFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Holon Metadata from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all Holons Metadata for the current avatar.
        /// </summary>
        /// <returns>List of all Holons Metadata for the avatar.</returns>
        /// <response code="200">Holons Metadata loaded successfully</response>
        /// <response code="400">Error loading Holons Metadata</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllHolonMetaDataForAvatar()
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadAllForAvatarAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<HolonMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading Holons Metadata for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for Holons Metadata using advanced search criteria.
        /// </summary>
        /// <param name="request">The search request with criteria.</param>
        /// <returns>List of matching Holons Metadata.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<HolonMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchHolonsMetaData([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.SearchAsync<HolonMetaDataDNA>(avatarId: AvatarId, searchTerm: request.SearchTerm, searchOnlyForCurrentAvatar: true);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<HolonMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error searching Holons Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to download.</param>
        /// <param name="request">The download request details.</param>
        /// <returns>The download result.</returns>
        /// <response code="200">Download completed successfully</response>
        /// <response code="400">Error downloading Holon Metadata</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadHolonMetaData(Guid id, [FromBody] DownloadHolonMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.DownloadAsync(AvatarId, id, "latest", request.DestinationPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error downloading Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata.</param>
        /// <param name="version">The version to load.</param>
        /// <returns>The specific version of the Holon Metadata.</returns>
        /// <response code="200">Version loaded successfully</response>
        /// <response code="400">Error loading version</response>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadHolonMetaDataVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to edit.</param>
        /// <param name="request">The edit request details.</param>
        /// <returns>The edited Holon Metadata.</returns>
        /// <response code="200">Holon Metadata edited successfully</response>
        /// <response code="400">Error editing Holon Metadata</response>
        [HttpPut("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditHolonMetaData(Guid id, [FromBody] EditHolonMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error editing Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to unpublish.</param>
        /// <param name="version">The version to unpublish.</param>
        /// <returns>The unpublished Holon Metadata.</returns>
        /// <response code="200">Holon Metadata unpublished successfully</response>
        /// <response code="400">Error unpublishing Holon Metadata</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishHolonMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error unpublishing Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to republish.</param>
        /// <param name="request">The republish request details.</param>
        /// <param name="version">The version to republish.</param>
        /// <returns>The republished Holon Metadata.</returns>
        /// <response code="200">Holon Metadata republished successfully</response>
        /// <response code="400">Error republishing Holon Metadata</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishHolonMetaData(Guid id, [FromBody] PublishRequest request, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error republishing Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to activate.</param>
        /// <param name="version">The version to activate.</param>
        /// <returns>The activated Holon Metadata.</returns>
        /// <response code="200">Holon Metadata activated successfully</response>
        /// <response code="400">Error activating Holon Metadata</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateHolonMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error activating Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a Holon Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Holon Metadata to deactivate.</param>
        /// <param name="version">The version to deactivate.</param>
        /// <returns>The deactivated Holon Metadata.</returns>
        /// <response code="200">Holon Metadata deactivated successfully</response>
        /// <response code="400">Error deactivating Holon Metadata</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<HolonMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateHolonMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.HolonsMetaDataDNA.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<HolonMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error deactivating Holon Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateHolonMetaDataRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HolonType HolonSubType { get; set; }
        public string SourceFolderPath { get; set; } = string.Empty;
        public ISTARNETCreateOptions<HolonMetaDataDNA, STARNETDNA>? CreateOptions { get; set; }
    }

    public class EditHolonMetaDataRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadHolonMetaDataRequest
    {
        public string DestinationPath { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
    }
}