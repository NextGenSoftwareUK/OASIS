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
    /// OAPPs (OASIS Applications) management endpoints for creating, updating, and managing STAR OAPPs.
    /// OAPPs represent applications and services that plug into the OASIS ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OAPPsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all OAPPs in the system.
        /// </summary>
        /// <returns>List of all OAPPs available in the STAR system.</returns>
        /// <response code="200">OAPPs retrieved successfully</response>
        /// <response code="400">Error retrieving OAPPs</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllOAPPs()
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<OAPP>>
                {
                    IsError = true,
                    Message = $"Error loading OAPPs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific OAPP by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to retrieve.</param>
        /// <returns>The requested OAPP details.</returns>
        /// <response code="200">OAPP retrieved successfully</response>
        /// <response code="400">Error retrieving OAPP</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOAPP(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error loading OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new OAPP for the authenticated avatar.
        /// </summary>
        /// <param name="oapp">The OAPP details to create.</param>
        /// <returns>The created OAPP with assigned ID and metadata.</returns>
        /// <response code="200">OAPP created successfully</response>
        /// <response code="400">Error creating OAPP</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOAPP([FromBody] OAPP oapp)
        {
            try
            {
                var result = await _starAPI.OAPPs.UpdateAsync(AvatarId, oapp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error creating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOAPP(Guid id, [FromBody] OAPP oapp)
        {
            try
            {
                oapp.Id = id;
                var result = await _starAPI.OAPPs.UpdateAsync(AvatarId, oapp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error updating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOAPP(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPs.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneOAPP(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes an OAPP to the STARNET system with optional cloud upload.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the OAPP publish operation.</returns>
        /// <response code="200">OAPP published successfully</response>
        /// <response code="400">Error publishing OAPP</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishOAPP(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.PublishAsync(
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
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error publishing OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads an OAPP from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to download.</param>
        /// <param name="version">The version of the OAPP to download.</param>
        /// <param name="downloadPath">Optional path where the OAPP should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the OAPP download operation.</returns>
        /// <response code="200">OAPP downloaded successfully</response>
        /// <response code="400">Error downloading OAPP</response>
        // removed duplicate download endpoint (see POST {id}/download below)

        /// <summary>
        /// Searches for OAPPs by name or description.
        /// </summary>
        /// <param name="searchTerm">The search term to look for in OAPP names and descriptions.</param>
        /// <param name="showAllVersions">Whether to show all versions of matching OAPPs.</param>
        /// <param name="version">Specific version to search for (0 for latest).</param>
        /// <returns>List of OAPPs matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchOAPPs([FromQuery] string searchTerm, [FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.OAPPs.SearchAsync<OAPP>(AvatarId, searchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<OAPP>>
                {
                    IsError = true,
                    Message = $"Error searching OAPPs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific OAPP.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to get versions for.</param>
        /// <returns>List of all versions of the specified OAPP.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOAPPVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<OAPP>>
                {
                    IsError = true,
                    Message = $"Error retrieving OAPP versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new OAPP with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing OAPP details and source folder path.</param>
        /// <returns>Result of the OAPP creation operation.</returns>
        /// <response code="200">OAPP created successfully</response>
        /// <response code="400">Error creating OAPP</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOAPPWithOptions([FromBody] CreateOAPPRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error creating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an OAPP from the specified path.
        /// </summary>
        /// <param name="path">The path to load the OAPP from.</param>
        /// <returns>Result of the OAPP load operation.</returns>
        /// <response code="200">OAPP loaded successfully</response>
        /// <response code="400">Error loading OAPP</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadOAPPFromPath([FromQuery] string path)
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadForSourceOrInstalledFolderAsync(AvatarId, path, HolonType.OAPP);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error loading OAPP from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an OAPP from published sources.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to load from published.</param>
        /// <returns>Result of the OAPP load operation.</returns>
        /// <response code="200">OAPP loaded successfully</response>
        /// <response code="400">Error loading OAPP</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadOAPPFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error loading OAPP from published: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all OAPPs for the current avatar.
        /// </summary>
        /// <returns>List of all OAPPs for the avatar.</returns>
        /// <response code="200">OAPPs loaded successfully</response>
        /// <response code="400">Error loading OAPPs</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllOAPPsForAvatar()
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadAllForAvatarAsync(AvatarId, false, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<OAPP>>
                {
                    IsError = true,
                    Message = $"Error loading OAPPs for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for OAPPs based on the provided search criteria.
        /// </summary>
        /// <param name="request">Search request containing search parameters.</param>
        /// <returns>List of OAPPs matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<OAPP>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchOAPPs([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.SearchAsync<OAPP>(AvatarId, request.SearchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, false, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<OAPP>>
                {
                    IsError = true,
                    Message = $"Error searching OAPPs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads an OAPP from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to download.</param>
        /// <param name="request">Download request containing destination path and options.</param>
        /// <returns>Result of the OAPP download operation.</returns>
        /// <response code="200">OAPP downloaded successfully</response>
        /// <response code="400">Error downloading OAPP</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadOAPP(Guid id, [FromBody] DownloadOAPPRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.DownloadAsync(AvatarId, id, 0, request.DestinationPath, request.Overwrite);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error downloading OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of an OAPP.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP.</param>
        /// <param name="version">The version to load.</param>
        /// <returns>Result of the OAPP version load operation.</returns>
        /// <response code="200">OAPP version loaded successfully</response>
        /// <response code="400">Error loading OAPP version</response>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadOAPPVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.OAPPs.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error loading OAPP version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits an OAPP with the provided changes.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to edit.</param>
        /// <param name="request">Edit request containing the changes to apply.</param>
        /// <returns>Result of the OAPP edit operation.</returns>
        /// <response code="200">OAPP edited successfully</response>
        /// <response code="400">Error editing OAPP</response>
        [HttpPut("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditOAPP(Guid id, [FromBody] EditOAPPRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPs.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error editing OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes an OAPP from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to unpublish.</param>
        /// <returns>Result of the OAPP unpublish operation.</returns>
        /// <response code="200">OAPP unpublished successfully</response>
        /// <response code="400">Error unpublishing OAPP</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishOAPP(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.OAPPs.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error unpublishing OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes an OAPP to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to republish.</param>
        /// <param name="request">Republish request containing updated parameters.</param>
        /// <returns>Result of the OAPP republish operation.</returns>
        /// <response code="200">OAPP republished successfully</response>
        /// <response code="400">Error republishing OAPP</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishOAPP(Guid id, [FromBody] PublishRequest request, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.OAPPs.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error republishing OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates an OAPP in the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to activate.</param>
        /// <returns>Result of the OAPP activation operation.</returns>
        /// <response code="200">OAPP activated successfully</response>
        /// <response code="400">Error activating OAPP</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateOAPP(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.OAPPs.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error activating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates an OAPP in the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the OAPP to deactivate.</param>
        /// <returns>Result of the OAPP deactivation operation.</returns>
        /// <response code="200">OAPP deactivated successfully</response>
        /// <response code="400">Error deactivating OAPP</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<OAPP>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateOAPP(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.OAPPs.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<OAPP>
                {
                    IsError = true,
                    Message = $"Error deactivating OAPP: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateOAPPRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OAPPType HolonSubType { get; set; }
        public string SourceFolderPath { get; set; } = string.Empty;
        public ISTARNETCreateOptions<OAPP, STARNETDNA>? CreateOptions { get; set; }
    }

    public class EditOAPPRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadOAPPRequest
    {
        public string DestinationPath { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
    }

}
