using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Runtimes management endpoints for creating, updating, and managing STAR runtimes.
    /// Runtimes represent execution environments and runtime configurations within the STAR ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RuntimesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all runtimes in the system.
        /// </summary>
        /// <returns>List of all runtimes available in the STAR system.</returns>
        /// <response code="200">Runtimes retrieved successfully</response>
        /// <response code="400">Error retrieving runtimes</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllRuntimes()
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading runtimes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific runtime by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to retrieve.</param>
        /// <returns>The requested runtime details.</returns>
        /// <response code="200">Runtime retrieved successfully</response>
        /// <response code="400">Error retrieving runtime</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new runtime for the authenticated avatar.
        /// </summary>
        /// <param name="request">The runtime creation request details.</param>
        /// <returns>The created runtime with assigned ID and metadata.</returns>
        /// <response code="200">Runtime created successfully</response>
        /// <response code="400">Error creating runtime</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRuntime([FromBody] CreateRuntimeRequest request)
        {
            try
            {
                // Create a new runtime using the RuntimeManager
                var result = await _starAPI.Runtimes.CreateAsync(
                    AvatarId,
                    request.Name,
                    request.Description,
                    request.RuntimeType,
                    request.Version,
                    null // createOptions
                );
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error creating runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRuntime(Guid id, [FromBody] UpdateRuntimeRequest request)
        {
            try
            {
                // Load existing runtime
                var existingResult = await _starAPI.Runtimes.LoadAsync(AvatarId, id, 0);
                
                if (existingResult.IsError || existingResult.Result == null)
                {
                    return BadRequest(new OASISResult<object>
                    {
                        IsError = true,
                        Message = "Runtime not found",
                        Exception = existingResult.Exception
                    });
                }

                // Update runtime properties
                var runtime = existingResult.Result;
                runtime.Name = request.Name ?? runtime.Name;
                runtime.Description = request.Description ?? runtime.Description;
                
                // Update the runtime
                var result = await _starAPI.Runtimes.UpdateAsync(AvatarId, runtime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error updating runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchRuntimes([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Search term is required"
                    });
                }

                // Get all runtimes and filter by search term
                var allRuntimesResult = await _starAPI.Runtimes.LoadAllAsync(AvatarId, null);
                
                if (allRuntimesResult.IsError || allRuntimesResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load runtimes for search",
                        Exception = allRuntimesResult.Exception
                    });
                }

                // Filter runtimes by search term
                var filteredRuntimes = allRuntimesResult.Result
                    .Where(runtime => 
                        runtime.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        runtime.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = filteredRuntimes,
                    IsError = false,
                    Message = $"Found {filteredRuntimes.Count} runtimes matching '{searchTerm}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error searching runtimes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetRuntimesByType(string type)
        {
            try
            {
                // Get all runtimes and filter by type
                var allRuntimesResult = await _starAPI.Runtimes.LoadAllAsync(AvatarId, null);
                
                if (allRuntimesResult.IsError || allRuntimesResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load runtimes",
                        Exception = allRuntimesResult.Exception
                    });
                }

                // Filter runtimes by type (assuming type is stored in metadata)
                var typeRuntimes = allRuntimesResult.Result
                    .Where(runtime => 
                        runtime.MetaData?.ContainsKey("RuntimeType") == true &&
                        string.Equals(runtime.MetaData["RuntimeType"]?.ToString(), type, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = typeRuntimes,
                    IsError = false,
                    Message = $"Found {typeRuntimes.Count} runtimes of type '{type}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading runtimes by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.StartAsync(AvatarId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error starting runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.StopAsync(AvatarId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error stopping runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetRuntimeStatus(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.GetStatusAsync(AvatarId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error getting runtime status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneRuntime(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new runtime with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing runtime details and source folder path.</param>
        /// <returns>Result of the runtime creation operation.</returns>
        /// <response code="200">Runtime created successfully</response>
        /// <response code="400">Error creating runtime</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRuntimeWithOptions([FromBody] CreateRuntimeWithOptionsRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error creating runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a runtime from the specified path.
        /// </summary>
        /// <param name="path">The path to load the runtime from.</param>
        /// <returns>Result of the runtime load operation.</returns>
        /// <response code="200">Runtime loaded successfully</response>
        /// <response code="400">Error loading runtime</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadRuntimeFromPath([FromQuery] string path)
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadForSourceOrInstalledFolderAsync(AvatarId, path, HolonType.Runtime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading runtime from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a runtime from published sources.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to load from published.</param>
        /// <returns>Result of the runtime load operation.</returns>
        /// <response code="200">Runtime loaded successfully</response>
        /// <response code="400">Error loading runtime</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadRuntimeFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading runtime from published: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all runtimes for the current avatar.
        /// </summary>
        /// <returns>List of all runtimes for the avatar.</returns>
        /// <response code="200">Runtimes loaded successfully</response>
        /// <response code="400">Error loading runtimes</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllRuntimesForAvatar()
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadAllForAvatarAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading runtimes for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for runtimes based on the provided search criteria.
        /// </summary>
        /// <param name="request">Search request containing search parameters.</param>
        /// <returns>List of runtimes matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchRuntimes([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.SearchAsync<Runtime>(AvatarId, request.SearchTerm, default, null, MetaKeyValuePairMatchMode.All, true, false, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error searching runtimes: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a runtime to the STARNET system with optional cloud upload.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the runtime publish operation.</returns>
        /// <response code="200">Runtime published successfully</response>
        /// <response code="400">Error publishing runtime</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishRuntime(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.PublishAsync(
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
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error publishing runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads a runtime from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to download.</param>
        /// <param name="request">Download request containing destination path and options.</param>
        /// <returns>Result of the runtime download operation.</returns>
        /// <response code="200">Runtime downloaded successfully</response>
        /// <response code="400">Error downloading runtime</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadRuntime(Guid id, [FromBody] DownloadRuntimeRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.DownloadAsync(AvatarId, id, 0, request.DestinationPath, request.Overwrite);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error downloading runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a runtime.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime.</param>
        /// <returns>List of all versions for the runtime.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRuntimeVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error getting runtime versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a runtime.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime.</param>
        /// <param name="version">The version to load.</param>
        /// <returns>Result of the runtime version load operation.</returns>
        /// <response code="200">Runtime version loaded successfully</response>
        /// <response code="400">Error loading runtime version</response>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadRuntimeVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Runtimes.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading runtime version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits a runtime with the provided changes.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to edit.</param>
        /// <param name="request">Edit request containing the changes to apply.</param>
        /// <returns>Result of the runtime edit operation.</returns>
        /// <response code="200">Runtime edited successfully</response>
        /// <response code="400">Error editing runtime</response>
        [HttpPut("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditRuntime(Guid id, [FromBody] EditRuntimeRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error editing runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes a runtime from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to unpublish.</param>
        /// <returns>Result of the runtime unpublish operation.</returns>
        /// <response code="200">Runtime unpublished successfully</response>
        /// <response code="400">Error unpublishing runtime</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.UnpublishAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error unpublishing runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes a runtime to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to republish.</param>
        /// <param name="request">Republish request containing updated parameters.</param>
        /// <returns>Result of the runtime republish operation.</returns>
        /// <response code="200">Runtime republished successfully</response>
        /// <response code="400">Error republishing runtime</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishRuntime(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Runtimes.RepublishAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error republishing runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates a runtime in the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to activate.</param>
        /// <returns>Result of the runtime activation operation.</returns>
        /// <response code="200">Runtime activated successfully</response>
        /// <response code="400">Error activating runtime</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.ActivateAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error activating runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates a runtime in the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the runtime to deactivate.</param>
        /// <returns>Result of the runtime deactivation operation.</returns>
        /// <response code="200">Runtime deactivated successfully</response>
        /// <response code="400">Error deactivating runtime</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateRuntime(Guid id)
        {
            try
            {
                var result = await _starAPI.Runtimes.DeactivateAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error deactivating runtime: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateRuntimeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RuntimeType { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Category { get; set; } = string.Empty;
    }

    public class UpdateRuntimeRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
    }

    public class CreateRuntimeWithOptionsRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HolonType HolonSubType { get; set; } = HolonType.Runtime;
        public string SourceFolderPath { get; set; } = string.Empty;
        public ISTARNETCreateOptions<Runtime, STARNETDNA>? CreateOptions { get; set; }
    }

    public class EditRuntimeRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadRuntimeRequest
    {
        public string DestinationPath { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
    }
}
