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
    /// Celestial Bodies Metadata management endpoints for creating, updating, and managing STAR Celestial Bodies Metadata.
    /// Celestial Bodies Metadata represent configuration and metadata for celestial bodies in the OASIS ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CelestialBodiesMetaDataController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all Celestial Bodies Metadata in the system.
        /// </summary>
        /// <returns>List of all Celestial Bodies Metadata available in the STAR system.</returns>
        /// <response code="200">Celestial Bodies Metadata retrieved successfully</response>
        /// <response code="400">Error retrieving Celestial Bodies Metadata</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllCelestialBodiesMetaData()
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading Celestial Bodies Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific Celestial Body Metadata by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to retrieve.</param>
        /// <returns>The requested Celestial Body Metadata details.</returns>
        /// <response code="200">Celestial Body Metadata retrieved successfully</response>
        /// <response code="400">Error retrieving Celestial Body Metadata</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodyMetaData(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new Celestial Body Metadata for the authenticated avatar.
        /// </summary>
        /// <param name="metadata">The Celestial Body Metadata details to create.</param>
        /// <returns>The created Celestial Body Metadata with assigned ID and metadata.</returns>
        /// <response code="200">Celestial Body Metadata created successfully</response>
        /// <response code="400">Error creating Celestial Body Metadata</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialBodyMetaData([FromBody] CelestialBodyMetaDataDNA metadata)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.UpdateAsync(AvatarId, metadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error creating Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Updates an existing Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to update.</param>
        /// <param name="metadata">The updated Celestial Body Metadata details.</param>
        /// <returns>The updated Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata updated successfully</response>
        /// <response code="400">Error updating Celestial Body Metadata</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCelestialBodyMetaData(Guid id, [FromBody] CelestialBodyMetaDataDNA metadata)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.UpdateAsync(AvatarId, metadata);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error updating Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deletes a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to delete.</param>
        /// <returns>Success status of the deletion operation.</returns>
        /// <response code="200">Celestial Body Metadata deleted successfully</response>
        /// <response code="400">Error deleting Celestial Body Metadata</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCelestialBodyMetaData(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Clones an existing Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to clone.</param>
        /// <param name="request">The clone request details.</param>
        /// <returns>The cloned Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata cloned successfully</response>
        /// <response code="400">Error cloning Celestial Body Metadata</response>
        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneCelestialBodyMetaData(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a Celestial Body Metadata to the STARNET.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to publish.</param>
        /// <param name="request">The publish request details.</param>
        /// <returns>The published Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata published successfully</response>
        /// <response code="400">Error publishing Celestial Body Metadata</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishCelestialBodyMetaData(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.PublishAsync(AvatarId, request.SourcePath, request.LaunchTarget, request.PublishPath, request.Edit, request.RegisterOnSTARNET, request.GenerateBinary, request.UploadToCloud);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error publishing Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for Celestial Bodies Metadata based on search criteria.
        /// </summary>
        /// <param name="searchTerm">The search term to look for.</param>
        /// <param name="showAllVersions">Whether to show all versions of the results.</param>
        /// <param name="version">The version to search for.</param>
        /// <returns>List of matching Celestial Bodies Metadata.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchCelestialBodiesMetaData([FromQuery] string searchTerm, [FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.SearchAsync<CelestialBodyMetaDataDNA>(AvatarId, searchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error searching Celestial Bodies Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves all versions of a specific Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata.</param>
        /// <returns>List of all versions of the Celestial Body Metadata.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodyMetaDataVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new Celestial Body Metadata with advanced options.
        /// </summary>
        /// <param name="request">The create request with options.</param>
        /// <returns>The created Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata created successfully</response>
        /// <response code="400">Error creating Celestial Body Metadata</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialBodyMetaDataWithOptions([FromBody] CreateCelestialBodyMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error creating Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a Celestial Body Metadata from a file path.
        /// </summary>
        /// <param name="path">The file path to load from.</param>
        /// <returns>The loaded Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata loaded successfully</response>
        /// <response code="400">Error loading Celestial Body Metadata</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBodyMetaDataFromPath([FromQuery] string path)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadForSourceOrInstalledFolderAsync(AvatarId, path);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Celestial Body Metadata from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a Celestial Body Metadata from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The published file path to load from.</param>
        /// <returns>The loaded Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata loaded successfully</response>
        /// <response code="400">Error loading Celestial Body Metadata</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBodyMetaDataFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading Celestial Body Metadata from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all Celestial Bodies Metadata for the current avatar.
        /// </summary>
        /// <returns>List of all Celestial Bodies Metadata for the avatar.</returns>
        /// <response code="200">Celestial Bodies Metadata loaded successfully</response>
        /// <response code="400">Error loading Celestial Bodies Metadata</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllCelestialBodyMetaDataForAvatar()
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadAllForAvatarAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error loading Celestial Bodies Metadata for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for Celestial Bodies Metadata using advanced search criteria.
        /// </summary>
        /// <param name="request">The search request with criteria.</param>
        /// <returns>List of matching Celestial Bodies Metadata.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchCelestialBodiesMetaData([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.SearchAsync<CelestialBodyMetaDataDNA>(AvatarId, request.SearchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, false, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<CelestialBodyMetaDataDNA>>
                {
                    IsError = true,
                    Message = $"Error searching Celestial Bodies Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to download.</param>
        /// <param name="request">The download request details.</param>
        /// <returns>The download result.</returns>
        /// <response code="200">Download completed successfully</response>
        /// <response code="400">Error downloading Celestial Body Metadata</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadCelestialBodyMetaData(Guid id, [FromBody] DownloadCelestialBodyMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.DownloadAsync(AvatarId, id, "latest", request.DestinationPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error downloading Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata.</param>
        /// <param name="version">The version to load.</param>
        /// <returns>The specific version of the Celestial Body Metadata.</returns>
        /// <response code="200">Version loaded successfully</response>
        /// <response code="400">Error loading version</response>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBodyMetaDataVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error loading version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to edit.</param>
        /// <param name="request">The edit request details.</param>
        /// <returns>The edited Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata edited successfully</response>
        /// <response code="400">Error editing Celestial Body Metadata</response>
        [HttpPut("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditCelestialBodyMetaData(Guid id, [FromBody] EditCelestialBodyMetaDataRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error editing Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to unpublish.</param>
        /// <param name="version">The version to unpublish.</param>
        /// <returns>The unpublished Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata unpublished successfully</response>
        /// <response code="400">Error unpublishing Celestial Body Metadata</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishCelestialBodyMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error unpublishing Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to republish.</param>
        /// <param name="request">The republish request details.</param>
        /// <param name="version">The version to republish.</param>
        /// <returns>The republished Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata republished successfully</response>
        /// <response code="400">Error republishing Celestial Body Metadata</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishCelestialBodyMetaData(Guid id, [FromBody] PublishRequest request, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error republishing Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to activate.</param>
        /// <param name="version">The version to activate.</param>
        /// <returns>The activated Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata activated successfully</response>
        /// <response code="400">Error activating Celestial Body Metadata</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateCelestialBodyMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error activating Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a Celestial Body Metadata.
        /// </summary>
        /// <param name="id">The unique identifier of the Celestial Body Metadata to deactivate.</param>
        /// <param name="version">The version to deactivate.</param>
        /// <returns>The deactivated Celestial Body Metadata.</returns>
        /// <response code="200">Celestial Body Metadata deactivated successfully</response>
        /// <response code="400">Error deactivating Celestial Body Metadata</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<CelestialBodyMetaDataDNA>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateCelestialBodyMetaData(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodiesMetaDataDNA.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<CelestialBodyMetaDataDNA>
                {
                    IsError = true,
                    Message = $"Error deactivating Celestial Body Metadata: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateCelestialBodyMetaDataRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CelestialBodyType HolonSubType { get; set; }
        public string SourceFolderPath { get; set; } = string.Empty;
        public ISTARNETCreateOptions<CelestialBodyMetaDataDNA, STARNETDNA>? CreateOptions { get; set; }
    }

    public class EditCelestialBodyMetaDataRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadCelestialBodyMetaDataRequest
    {
        public string DestinationPath { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
    }
}
