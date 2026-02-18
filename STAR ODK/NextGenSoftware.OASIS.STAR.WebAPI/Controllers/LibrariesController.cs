using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Libraries management endpoints for creating, updating, and managing STAR libraries.
    /// Libraries represent collections of code, and reusable components within the STARNET ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LibrariesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all libraries in the system.
        /// </summary>
        /// <returns>List of all libraries available in the STAR system.</returns>
        /// <response code="200">Libraries retrieved successfully</response>
        /// <response code="400">Error retrieving libraries</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllLibraries()
        {
            try
            {
                var result = await _starAPI.Libraries.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testLibraries = new List<object>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<object>>(testLibraries, "Libraries retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testLibraries = new List<object>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<object>>(testLibraries, "Libraries retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<object>>(ex, "GetAllLibraries");
            }
        }

        /// <summary>
        /// Retrieves a specific library by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the library to retrieve.</param>
        /// <returns>The requested library details.</returns>
        /// <response code="200">Library retrieved successfully</response>
        /// <response code="400">Error retrieving library</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLibrary(Guid id)
        {
            try
            {
                var result = await _starAPI.Libraries.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    return Ok(TestDataHelper.CreateSuccessResult<object>(null, "Library retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(TestDataHelper.CreateSuccessResult<object>(null, "Library retrieved successfully (using test data)"));
                }
                return HandleException<object>(ex, "GetLibrary");
            }
        }

        /// <summary>
        /// Creates a new library for the authenticated avatar.
        /// </summary>
        /// <param name="request">The library creation request details.</param>
        /// <returns>The created library with assigned ID and metadata.</returns>
        /// <response code="200">Library created successfully</response>
        /// <response code="400">Error creating library</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLibrary([FromBody] CreateLibraryRequest request)
        {
            try
            {
                // Create a new library using the LibraryManager
                var result = await _starAPI.Libraries.CreateAsync(
                    AvatarId,
                    request.Name,
                    request.Description,
                    typeof(object), // Library type
                    request.SourcePath,
                    null // createOptions
                );
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "creating library");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLibrary(Guid id, [FromBody] UpdateLibraryRequest request)
        {
            try
                {
                // Load existing library
                var existingResult = await _starAPI.Libraries.LoadAsync(AvatarId, id, 0);
                
                if (existingResult.IsError || existingResult.Result == null)
                {
                    return BadRequest(new OASISResult<object>
                    {
                        IsError = true,
                        Message = "Library not found",
                        Exception = existingResult.Exception
                    });
                }

                // Update library properties
                var library = existingResult.Result;
                library.Name = request.Name ?? library.Name;
                library.Description = request.Description ?? library.Description;
                
                // Update the library
                var result = await _starAPI.Libraries.UpdateAsync(AvatarId, library);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "updating library");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLibrary(Guid id)
        {
            try
            {
                var result = await _starAPI.Libraries.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting library");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchLibraries([FromQuery] string searchTerm)
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

                // Get all libraries and filter by search term
                var allLibrariesResult = await _starAPI.Libraries.LoadAllAsync(AvatarId, null);
                
                if (allLibrariesResult.IsError || allLibrariesResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load libraries for search",
                        Exception = allLibrariesResult.Exception
                    });
                }

                // Filter libraries by search term
                var filteredLibraries = allLibrariesResult.Result
                    .Where(lib => 
                        lib.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        lib.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = filteredLibraries,
                    IsError = false,
                    Message = $"Found {filteredLibraries.Count} libraries matching '{searchTerm}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error searching libraries: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-category/{category}")]
        public async Task<IActionResult> GetLibrariesByCategory(string category)
        {
            try
            {
                // Get all libraries and filter by category
                var allLibrariesResult = await _starAPI.Libraries.LoadAllAsync(AvatarId, null);
                
                if (allLibrariesResult.IsError || allLibrariesResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load libraries",
                        Exception = allLibrariesResult.Exception
                    });
                }

                // Filter libraries by category (assuming category is stored in metadata)
                var categoryLibraries = allLibrariesResult.Result
                    .Where(lib => 
                        lib.MetaData?.ContainsKey("Category") == true &&
                        string.Equals(lib.MetaData["Category"]?.ToString(), category, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = categoryLibraries,
                    IsError = false,
                    Message = $"Found {categoryLibraries.Count} libraries in category '{category}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading libraries by category: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneLibrary(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Libraries.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "cloning library");
            }
        }

        /// <summary>
        /// Creates a new library with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing library details and source folder path.</param>
        /// <returns>Result of the library creation operation.</returns>
        /// <response code="200">Library created successfully</response>
        /// <response code="400">Error creating library</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLibraryWithOptions([FromBody] CreateLibraryWithOptionsRequest request)
        {
            try
            {
                var result = await _starAPI.Libraries.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "creating library");
            }
        }

        /// <summary>
        /// Loads a library by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the library to load.</param>
        /// <param name="version">The version of the library to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested library details.</returns>
        /// <response code="200">Library loaded successfully</response>
        /// <response code="400">Error loading library</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadLibrary(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Libraries.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "GetLibrary");
            }
        }

        /// <summary>
        /// Loads a library from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded library details.</returns>
        /// <response code="200">Library loaded successfully</response>
        /// <response code="400">Error loading library</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadLibraryFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.Libraries.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading library from path");
            }
        }

        /// <summary>
        /// Loads a library from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published library file.</param>
        /// <returns>The loaded library details.</returns>
        /// <response code="200">Library loaded successfully</response>
        /// <response code="400">Error loading library</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadLibraryFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Libraries.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading library from published file");
            }
        }

        /// <summary>
        /// Loads all libraries for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of libraries.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all libraries for the avatar.</returns>
        /// <response code="200">Libraries loaded successfully</response>
        /// <response code="400">Error loading libraries</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllLibrariesForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Libraries.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading libraries for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a library to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the library to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the library publish operation.</returns>
        /// <response code="200">Library published successfully</response>
        /// <response code="400">Error publishing library</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishLibrary(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Libraries.PublishAsync(
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
                return HandleException<object>(ex, "publishing library");
            }
        }

        /// <summary>
        /// Downloads a library from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the library to download.</param>
        /// <param name="version">The version of the library to download.</param>
        /// <param name="downloadPath">Optional path where the library should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the library download operation.</returns>
        /// <response code="200">Library downloaded successfully</response>
        /// <response code="400">Error downloading library</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadLibrary(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Libraries.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "downloading library");
            }
        }

        /// <summary>
        /// Gets all versions of a specific library.
        /// </summary>
        /// <param name="id">The unique identifier of the library to get versions for.</param>
        /// <returns>List of all versions of the specified library.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLibraryVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Libraries.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error retrieving library versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a library.
        /// </summary>
        /// <param name="id">The unique identifier of the library.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested library version details.</returns>
        /// <response code="200">Library version loaded successfully</response>
        /// <response code="400">Error loading library version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadLibraryVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Libraries.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading library version");
            }
        }

        /// <summary>
        /// Edits a library with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the library to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the library edit operation.</returns>
        /// <response code="200">Library edited successfully</response>
        /// <response code="400">Error editing library</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditLibrary(Guid id, [FromBody] EditLibraryRequest request)
        {
            try
            {
                var result = await _starAPI.Libraries.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "editing library");
            }
        }

        /// <summary>
        /// Unpublishes a library from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the library to unpublish.</param>
        /// <param name="version">The version of the library to unpublish.</param>
        /// <returns>Result of the library unpublish operation.</returns>
        /// <response code="200">Library unpublished successfully</response>
        /// <response code="400">Error unpublishing library</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishLibrary(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Libraries.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "unpublishing library");
            }
        }

        /// <summary>
        /// Republishes a library to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the library to republish.</param>
        /// <param name="version">The version of the library to republish.</param>
        /// <returns>Result of the library republish operation.</returns>
        /// <response code="200">Library republished successfully</response>
        /// <response code="400">Error republishing library</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishLibrary(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Libraries.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "republishing library");
            }
        }

        /// <summary>
        /// Activates a library.
        /// </summary>
        /// <param name="id">The unique identifier of the library to activate.</param>
        /// <param name="version">The version of the library to activate.</param>
        /// <returns>Result of the library activation operation.</returns>
        /// <response code="200">Library activated successfully</response>
        /// <response code="400">Error activating library</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateLibrary(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Libraries.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "activating library");
            }
        }

        /// <summary>
        /// Deactivates a library.
        /// </summary>
        /// <param name="id">The unique identifier of the library to deactivate.</param>
        /// <param name="version">The version of the library to deactivate.</param>
        /// <returns>Result of the library deactivation operation.</returns>
        /// <response code="200">Library deactivated successfully</response>
        /// <response code="400">Error deactivating library</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateLibrary(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Libraries.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "deactivating library");
            }
        }
    }

    public class CreateLibraryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
    }

    public class UpdateLibraryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
    }

    public class CreateLibraryWithOptionsRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Library;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Library, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditLibraryRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

}
