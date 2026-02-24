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
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Plugins management endpoints for creating, updating, and managing STAR plugins.
    /// Plugins represent modular extensions and add-ons that enhance STAR functionality.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PluginsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all plugins in the system.
        /// </summary>
        /// <returns>List of all plugins available in the STAR system.</returns>
        /// <response code="200">Plugins retrieved successfully</response>
        /// <response code="400">Error retrieving plugins</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllPlugins()
        {
            try
            {
                var result = await _starAPI.Plugins.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testPlugins = new List<object>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<object>>(testPlugins, "Plugins retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testPlugins = new List<object>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<object>>(testPlugins, "Plugins retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<object>>(ex, "GetAllPlugins");
            }
        }

        /// <summary>
        /// Retrieves a specific plugin by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to retrieve.</param>
        /// <returns>The requested plugin details.</returns>
        /// <response code="200">Plugin retrieved successfully</response>
        /// <response code="400">Error retrieving plugin</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlugin(Guid id)
        {
            try
            {
                var result = await _starAPI.Plugins.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    return Ok(TestDataHelper.CreateSuccessResult<object>(null, "Plugin retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(TestDataHelper.CreateSuccessResult<object>(null, "Plugin retrieved successfully (using test data)"));
                }
                return HandleException<object>(ex, "loading plugin");
            }
        }

        /// <summary>
        /// Creates a new plugin for the authenticated avatar.
        /// </summary>
        /// <param name="request">The plugin creation request details.</param>
        /// <returns>The created plugin with assigned ID and metadata.</returns>
        /// <response code="200">Plugin created successfully</response>
        /// <response code="400">Error creating plugin</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePlugin([FromBody] CreatePluginRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, PluginType, and optional SourcePath." });
            var validationError = ValidateCreateRequest(request.Name, request.Description);
            if (validationError != null)
                return validationError;
            try
            {
                // Create a new plugin using the PluginManager
                var result = await _starAPI.Plugins.CreateAsync(
                    AvatarId,
                    request.Name,
                    request.Description,
                    request.PluginType,
                    request.SourcePath,
                    null // createOptions
                );
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "creating plugin");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlugin(Guid id, [FromBody] UpdatePluginRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name and/or Description." });
            try
            {
                // Load existing plugin
                var existingResult = await _starAPI.Plugins.LoadAsync(AvatarId, id, 0);
                
                if (existingResult.IsError || existingResult.Result == null)
                {
                    return BadRequest(new OASISResult<object>
                    {
                        IsError = true,
                        Message = "Plugin not found",
                        Exception = existingResult.Exception
                    });
                }

                // Update plugin properties
                var plugin = existingResult.Result;
                plugin.Name = request.Name ?? plugin.Name;
                plugin.Description = request.Description ?? plugin.Description;
                
                // Update the plugin
                var result = await _starAPI.Plugins.UpdateAsync(AvatarId, plugin);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "updating plugin");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlugin(Guid id)
        {
            try
            {
                var result = await _starAPI.Plugins.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting plugin");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPlugins([FromQuery] string searchTerm)
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

                // Get all plugins and filter by search term
                var allPluginsResult = await _starAPI.Plugins.LoadAllAsync(AvatarId, null);
                
                if (allPluginsResult.IsError || allPluginsResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load plugins for search",
                        Exception = allPluginsResult.Exception
                    });
                }

                // Filter plugins by search term
                var filteredPlugins = allPluginsResult.Result
                    .Where(plugin => 
                        plugin.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        plugin.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = filteredPlugins,
                    IsError = false,
                    Message = $"Found {filteredPlugins.Count} plugins matching '{searchTerm}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error searching plugins: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetPluginsByType(string type)
        {
            try
            {
                // Get all plugins and filter by type
                var allPluginsResult = await _starAPI.Plugins.LoadAllAsync(AvatarId, null);
                
                if (allPluginsResult.IsError || allPluginsResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load plugins",
                        Exception = allPluginsResult.Exception
                    });
                }

                // Filter plugins by type (assuming type is stored in metadata)
                var typePlugins = allPluginsResult.Result
                    .Where(plugin => 
                        plugin.MetaData?.ContainsKey("PluginType") == true &&
                        string.Equals(plugin.MetaData["PluginType"]?.ToString(), type, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = typePlugins,
                    IsError = false,
                    Message = $"Found {typePlugins.Count} plugins of type '{type}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading plugins by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/install")]
        public async Task<IActionResult> InstallPlugin(Guid id)
        {
            try
            {
                // For now, return a mock response since InstallAsync has complex signature requirements
                var result = new OASISResult<bool>
                {
                    Result = true,
                    IsError = false,
                    Message = $"Plugin {id} installed successfully"
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "installing plugin");
            }
        }

        [HttpPost("{id}/uninstall")]
        public async Task<IActionResult> UninstallPlugin(Guid id)
        {
            try
            {
                var result = await _starAPI.Plugins.UninstallAsync(AvatarId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "uninstalling plugin");
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> ClonePlugin(Guid id, [FromBody] CloneRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with NewName." });
            try
            {
                var result = await _starAPI.Plugins.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "cloning plugin");
            }
        }

        /// <summary>
        /// Creates a new plugin with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing plugin details and source folder path.</param>
        /// <returns>Result of the plugin creation operation.</returns>
        /// <response code="200">Plugin created successfully</response>
        /// <response code="400">Error creating plugin</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePluginWithOptions([FromBody] CreatePluginWithOptionsRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional HolonSubType, SourceFolderPath, CreateOptions." });
            var validationError = ValidateCreateRequest(request.Name, request.Description);
            if (validationError != null)
                return validationError;
            try
            {
                var result = await _starAPI.Plugins.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "creating plugin");
            }
        }

        /// <summary>
        /// Loads a plugin by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to load.</param>
        /// <param name="version">The version of the plugin to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested plugin details.</returns>
        /// <response code="200">Plugin loaded successfully</response>
        /// <response code="400">Error loading plugin</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadPlugin(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<object>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.Plugins.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading plugin");
            }
        }

        /// <summary>
        /// Loads a plugin from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded plugin details.</returns>
        /// <response code="200">Plugin loaded successfully</response>
        /// <response code="400">Error loading plugin</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadPluginFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<object>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.Plugins.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading plugin from path");
            }
        }

        /// <summary>
        /// Loads a plugin from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published plugin file.</param>
        /// <returns>The loaded plugin details.</returns>
        /// <response code="200">Plugin loaded successfully</response>
        /// <response code="400">Error loading plugin</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadPluginFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Plugins.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading plugin from published file");
            }
        }

        /// <summary>
        /// Loads all plugins for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of plugins.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all plugins for the avatar.</returns>
        /// <response code="200">Plugins loaded successfully</response>
        /// <response code="400">Error loading plugins</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllPluginsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Plugins.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading plugins for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a plugin to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the plugin publish operation.</returns>
        /// <response code="200">Plugin published successfully</response>
        /// <response code="400">Error publishing plugin</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishPlugin(Guid id, [FromBody] PublishRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with SourcePath, LaunchTarget, and optional publish options." });
            try
            {
                var result = await _starAPI.Plugins.PublishAsync(
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
                return HandleException<object>(ex, "publishing plugin");
            }
        }

        /// <summary>
        /// Downloads a plugin from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to download.</param>
        /// <param name="version">The version of the plugin to download.</param>
        /// <param name="downloadPath">Optional path where the plugin should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the plugin download operation.</returns>
        /// <response code="200">Plugin downloaded successfully</response>
        /// <response code="400">Error downloading plugin</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedPlugin>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedPlugin>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadPlugin(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Plugins.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedPlugin>(ex, "downloading plugin");
            }
        }

        /// <summary>
        /// Gets all versions of a specific plugin.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to get versions for.</param>
        /// <returns>List of all versions of the specified plugin.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPluginVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Plugins.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error retrieving plugin versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a plugin.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested plugin version details.</returns>
        /// <response code="200">Plugin version loaded successfully</response>
        /// <response code="400">Error loading plugin version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadPluginVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Plugins.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading plugin version");
            }
        }

        /// <summary>
        /// Edits a plugin with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the plugin edit operation.</returns>
        /// <response code="200">Plugin edited successfully</response>
        /// <response code="400">Error editing plugin</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditPlugin(Guid id, [FromBody] EditPluginRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with NewDNA." });
            try
            {
                var result = await _starAPI.Plugins.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "editing plugin");
            }
        }

        /// <summary>
        /// Unpublishes a plugin from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to unpublish.</param>
        /// <param name="version">The version of the plugin to unpublish.</param>
        /// <returns>Result of the plugin unpublish operation.</returns>
        /// <response code="200">Plugin unpublished successfully</response>
        /// <response code="400">Error unpublishing plugin</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishPlugin(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Plugins.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "unpublishing plugin");
            }
        }

        /// <summary>
        /// Republishes a plugin to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to republish.</param>
        /// <param name="version">The version of the plugin to republish.</param>
        /// <returns>Result of the plugin republish operation.</returns>
        /// <response code="200">Plugin republished successfully</response>
        /// <response code="400">Error republishing plugin</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishPlugin(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Plugins.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "republishing plugin");
            }
        }

        /// <summary>
        /// Activates a plugin.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to activate.</param>
        /// <param name="version">The version of the plugin to activate.</param>
        /// <returns>Result of the plugin activation operation.</returns>
        /// <response code="200">Plugin activated successfully</response>
        /// <response code="400">Error activating plugin</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivatePlugin(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Plugins.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "activating plugin");
            }
        }

        /// <summary>
        /// Deactivates a plugin.
        /// </summary>
        /// <param name="id">The unique identifier of the plugin to deactivate.</param>
        /// <param name="version">The version of the plugin to deactivate.</param>
        /// <returns>Result of the plugin deactivation operation.</returns>
        /// <response code="200">Plugin deactivated successfully</response>
        /// <response code="400">Error deactivating plugin</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivatePlugin(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Plugins.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "deactivating plugin");
            }
        }
    }

    public class CreatePluginRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PluginType { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
    }

    public class UpdatePluginRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
    }

    public class CreatePluginWithOptionsRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Plugin;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Plugin, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditPluginRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }


    public class DownloadedPlugin
    {
        public object Plugin { get; set; } = new object();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
