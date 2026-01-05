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

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Zomes management endpoints for creating, updating, and managing STAR zomes.
    /// Zomes represent Holochain applications and distributed computing modules within the STAR ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ZomesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all zomes in the system.
        /// </summary>
        /// <returns>List of all zomes available in the STAR system.</returns>
        /// <response code="200">Zomes retrieved successfully</response>
        /// <response code="400">Error retrieving zomes</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllZomes()
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific zome by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to retrieve.</param>
        /// <returns>The requested zome details.</returns>
        /// <response code="200">Zome retrieved successfully</response>
        /// <response code="400">Error retrieving zome</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZome(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new zome for the authenticated avatar.
        /// </summary>
        /// <param name="zome">The zome details to create.</param>
        /// <returns>The created zome with assigned ID and metadata.</returns>
        /// <response code="200">Zome created successfully</response>
        /// <response code="400">Error creating zome</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateZome([FromBody] STARZome zome)
        {
            try
            {
                var result = await _starAPI.Zomes.UpdateAsync(AvatarId, zome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error creating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateZome(Guid id, [FromBody] STARZome zome)
        {
            try
            {
                zome.Id = id;
                var result = await _starAPI.Zomes.UpdateAsync(AvatarId, zome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error updating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteZome(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetZomesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("in-space/{spaceId}")]
        public async Task<IActionResult> GetZomesInSpace(Guid spaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes in space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new zome with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing zome details and source folder path.</param>
        /// <returns>Result of the zome creation operation.</returns>
        /// <response code="200">Zome created successfully</response>
        /// <response code="400">Error creating zome</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateZomeWithOptions([FromBody] CreateZomeRequest request)
        {
            try
            {
                var result = await _starAPI.Zomes.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error creating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a zome by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to load.</param>
        /// <param name="version">The version of the zome to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested zome details.</returns>
        /// <response code="200">Zome loaded successfully</response>
        /// <response code="400">Error loading zome</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZome(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Zomes.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a zome from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded zome details.</returns>
        /// <response code="200">Zome loaded successfully</response>
        /// <response code="400">Error loading zome</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZomeFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Zomes.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a zome from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published zome file.</param>
        /// <returns>The loaded zome details.</returns>
        /// <response code="200">Zome loaded successfully</response>
        /// <response code="400">Error loading zome</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZomeFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all zomes for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of zomes.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all zomes for the avatar.</returns>
        /// <response code="200">Zomes loaded successfully</response>
        /// <response code="400">Error loading zomes</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllZomesForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error loading zomes for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for zomes by name or description.
        /// </summary>
        /// <param name="searchTerm">The search term to look for in zome names and descriptions.</param>
        /// <param name="searchOnlyForCurrentAvatar">Whether to search only for current avatar's zomes.</param>
        /// <param name="showAllVersions">Whether to show all versions of matching zomes.</param>
        /// <param name="version">Specific version to search for (0 for latest).</param>
        /// <returns>List of zomes matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchZomes([FromQuery] string searchTerm, [FromQuery] bool searchOnlyForCurrentAvatar = true, [FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Zomes.SearchAsync<STARZome>(avatarId: AvatarId, searchTerm: searchTerm, searchOnlyForCurrentAvatar: searchOnlyForCurrentAvatar, showAllVersions: showAllVersions, version: version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error searching zomes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a zome to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the zome publish operation.</returns>
        /// <response code="200">Zome published successfully</response>
        /// <response code="400">Error publishing zome</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishZome(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Zomes.PublishAsync(
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
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error publishing zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a zome from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to download.</param>
        /// <param name="version">The version of the zome to download.</param>
        /// <param name="downloadPath">Optional path where the zome should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the zome download operation.</returns>
        /// <response code="200">Zome downloaded successfully</response>
        /// <response code="400">Error downloading zome</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadZome(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Zomes.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DownloadedSTARZome>
                {
                    IsError = true,
                    Message = $"Error downloading zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific zome.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to get versions for.</param>
        /// <returns>List of all versions of the specified zome.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARZome>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZomeVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARZome>>
                {
                    IsError = true,
                    Message = $"Error retrieving zome versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a zome.
        /// </summary>
        /// <param name="id">The unique identifier of the zome.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested zome version details.</returns>
        /// <response code="200">Zome version loaded successfully</response>
        /// <response code="400">Error loading zome version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadZomeVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Zomes.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error loading zome version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a zome with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the zome edit operation.</returns>
        /// <response code="200">Zome edited successfully</response>
        /// <response code="400">Error editing zome</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditZome(Guid id, [FromBody] EditZomeRequest request)
        {
            try
            {
                var result = await _starAPI.Zomes.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error editing zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a zome from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to unpublish.</param>
        /// <param name="version">The version of the zome to unpublish.</param>
        /// <returns>Result of the zome unpublish operation.</returns>
        /// <response code="200">Zome unpublished successfully</response>
        /// <response code="400">Error unpublishing zome</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishZome(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Zomes.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error unpublishing zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a zome to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to republish.</param>
        /// <param name="version">The version of the zome to republish.</param>
        /// <returns>Result of the zome republish operation.</returns>
        /// <response code="200">Zome republished successfully</response>
        /// <response code="400">Error republishing zome</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishZome(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Zomes.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error republishing zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a zome.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to activate.</param>
        /// <param name="version">The version of the zome to activate.</param>
        /// <returns>Result of the zome activation operation.</returns>
        /// <response code="200">Zome activated successfully</response>
        /// <response code="400">Error activating zome</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateZome(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Zomes.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error activating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a zome.
        /// </summary>
        /// <param name="id">The unique identifier of the zome to deactivate.</param>
        /// <param name="version">The version of the zome to deactivate.</param>
        /// <returns>Result of the zome deactivation operation.</returns>
        /// <response code="200">Zome deactivated successfully</response>
        /// <response code="400">Error deactivating zome</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARZome>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateZome(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Zomes.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARZome>
                {
                    IsError = true,
                    Message = $"Error deactivating zome: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateZomeRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Zome;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<STARZome, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditZomeRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadedSTARZome
    {
        public STARZome Zome { get; set; } = new STARZome();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
