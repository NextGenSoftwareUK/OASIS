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
    /// Zomes Metadata management endpoints for creating, updating, and managing STAR Zomes Metadata.
    /// Zomes Metadata represent configuration and metadata for zomes (code modules) in the OASIS ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ZomesMetaDataController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all Zomes Metadata in the system.
        /// </summary>
        /// <returns>List of all Zomes Metadata available in the STAR system.</returns>
        /// <response code="200">Zomes Metadata retrieved successfully</response>
        /// <response code="400">Error retrieving Zomes Metadata</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllZomesMetaData()
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ZomeMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading Zomes Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific Zome Metadata by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to retrieve.</param>
        /// <returns>The requested Zome Metadata details.</returns>
        /// <response code="200">Zome Metadata retrieved successfully</response>
        /// <response code="400">Error retrieving Zome Metadata</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZomeMetaData(Guid id)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new Zome Metadata for the authenticated avatar.
        /// </summary>
        /// <param name="metadata">The Zome Metadata details to create.</param>
        /// <returns>The created Zome Metadata with assigned ID and metadata.</returns>
        /// <response code="200">Zome Metadata created successfully</response>
        /// <response code="400">Error creating Zome Metadata</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateZomeMetaData([FromBody] ZomeMetaDataDNA metadata)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.UpdateAsync(AvatarId, metadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error creating Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to update.</param>
        /// <param name="metadata">The updated Zome Metadata details.</param>
        /// <returns>The updated Zome Metadata.</returns>
        /// <response code="200">Zome Metadata updated successfully</response>
        /// <response code="400">Error updating Zome Metadata</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateZomeMetaData(Guid id, [FromBody] ZomeMetaDataDNA metadata)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.UpdateAsync(AvatarId, metadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error updating Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to delete.</param>
        /// <returns>Success status of the deletion operation.</returns>
        /// <response code="200">Zome Metadata deleted successfully</response>
        /// <response code="400">Error deleting Zome Metadata</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteZomeMetaData(Guid id)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Clones an existing Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to clone.</param>
        /// <param name="request">The clone request details.</param>
        /// <returns>The cloned Zome Metadata.</returns>
        /// <response code="200">Zome Metadata cloned successfully</response>
        /// <response code="400">Error cloning Zome Metadata</response>
        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneZomeMetaData(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a Zome Metadata to the STARNET.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to publish.</param>
        /// <param name="request">The publish request details.</param>
        /// <returns>The published Zome Metadata.</returns>
        /// <response code="200">Zome Metadata published successfully</response>
        /// <response code="400">Error publishing Zome Metadata</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishZomeMetaData(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.PublishAsync(AvatarId, request.SourcePath, request.LaunchTarget, request.PublishPath, request.Edit, request.RegisterOnSTARNET, request.GenerateBinary, request.UploadToCloud);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error publishing Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for Zomes Metadata based on search criteria.
        /// </summary>
        /// <param name="searchTerm">The search term to look for.</param>
        /// <param name="showAllVersions">Whether to show all versions of the results.</param>
        /// <param name="version">The version to search for.</param>
        /// <returns>List of matching Zomes Metadata.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchZomesMetaData([FromQuery] string searchTerm, [FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.SearchAsync<ZomeMetaDataDNA>(AvatarId, searchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ZomeMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error searching Zomes Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves all versions of a specific Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata.</param>
        /// <returns>List of all versions of the Zome Metadata.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZomeMetaDataVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ZomeMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new Zome Metadata with advanced options.
        /// </summary>
        /// <param name="request">The create request with options.</param>
        /// <returns>The created Zome Metadata.</returns>
        /// <response code="200">Zome Metadata created successfully</response>
        /// <response code="400">Error creating Zome Metadata</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateZomeMetaDataWithOptions([FromBody] CreateZomeMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error creating Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a Zome Metadata from a file path.
        /// </summary>
        /// <param name="path">The file path to load from.</param>
        /// <returns>The loaded Zome Metadata.</returns>
        /// <response code="200">Zome Metadata loaded successfully</response>
        /// <response code="400">Error loading Zome Metadata</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZomeMetaDataFromPath([FromQuery] string path)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadForSourceOrInstalledFolderAsync(AvatarId, path);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Zome Metadata from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a Zome Metadata from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The published file path to load from.</param>
        /// <returns>The loaded Zome Metadata.</returns>
        /// <response code="200">Zome Metadata loaded successfully</response>
        /// <response code="400">Error loading Zome Metadata</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZomeMetaDataFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Zome Metadata from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all Zomes Metadata for the current avatar.
        /// </summary>
        /// <returns>List of all Zomes Metadata for the avatar.</returns>
        /// <response code="200">Zomes Metadata loaded successfully</response>
        /// <response code="400">Error loading Zomes Metadata</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllZomeMetaDataForAvatar()
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadAllForAvatarAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ZomeMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading Zomes Metadata for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for Zomes Metadata using advanced search criteria.
        /// </summary>
        /// <param name="request">The search request with criteria.</param>
        /// <returns>List of matching Zomes Metadata.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ZomeMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchZomesMetaData([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.SearchAsync<ZomeMetaDataDNA>(AvatarId, request.SearchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, false, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ZomeMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error searching Zomes Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to download.</param>
        /// <param name="request">The download request details.</param>
        /// <returns>The download result.</returns>
        /// <response code="200">Download completed successfully</response>
        /// <response code="400">Error downloading Zome Metadata</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadZomeMetaData(Guid id, [FromBody] DownloadZomeMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.DownloadAsync(AvatarId, id, "latest", request.DestinationPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error downloading Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata.</param>
        /// <param name="version">The version to load.</param>
        /// <returns>The specific version of the Zome Metadata.</returns>
        /// <response code="200">Version loaded successfully</response>
        /// <response code="400">Error loading version</response>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZomeMetaDataVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to edit.</param>
        /// <param name="request">The edit request details.</param>
        /// <returns>The edited Zome Metadata.</returns>
        /// <response code="200">Zome Metadata edited successfully</response>
        /// <response code="400">Error editing Zome Metadata</response>
        [HttpPut("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditZomeMetaData(Guid id, [FromBody] EditZomeMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error editing Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to unpublish.</param>
        /// <param name="version">The version to unpublish.</param>
        /// <returns>The unpublished Zome Metadata.</returns>
        /// <response code="200">Zome Metadata unpublished successfully</response>
        /// <response code="400">Error unpublishing Zome Metadata</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishZomeMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error unpublishing Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to republish.</param>
        /// <param name="request">The republish request details.</param>
        /// <param name="version">The version to republish.</param>
        /// <returns>The republished Zome Metadata.</returns>
        /// <response code="200">Zome Metadata republished successfully</response>
        /// <response code="400">Error republishing Zome Metadata</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishZomeMetaData(Guid id, [FromBody] PublishRequest request, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error republishing Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to activate.</param>
        /// <param name="version">The version to activate.</param>
        /// <returns>The activated Zome Metadata.</returns>
        /// <response code="200">Zome Metadata activated successfully</response>
        /// <response code="400">Error activating Zome Metadata</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateZomeMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error activating Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a Zome Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Zome Metadata to deactivate.</param>
        /// <param name="version">The version to deactivate.</param>
        /// <returns>The deactivated Zome Metadata.</returns>
        /// <response code="200">Zome Metadata deactivated successfully</response>
        /// <response code="400">Error deactivating Zome Metadata</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ZomeMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateZomeMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.ZomesMetaDataDNA.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<ZomeMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error deactivating Zome Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateZomeMetaDataRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ZomeType HolonSubType { get; set; }
        public string SourceFolderPath { get; set; } = string.Empty;
        public ISTARNETCreateOptions<ZomeMetaDataDNA, STARNETDNA>? CreateOptions { get; set; }
    }

    public class EditZomeMetaDataRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadZomeMetaDataRequest
    {
        public string DestinationPath { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
    }
}