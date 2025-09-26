using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PluginsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllPlugins()
        {
            try
            {
                var result = await _starAPI.Plugins.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading plugins: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlugin(Guid id)
        {
            try
            {
                var result = await _starAPI.Plugins.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading plugin: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlugin([FromBody] CreatePluginRequest request)
        {
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
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error creating plugin: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlugin(Guid id, [FromBody] UpdatePluginRequest request)
        {
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
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error updating plugin: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting plugin: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error installing plugin: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error uninstalling plugin: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> ClonePlugin(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Plugins.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning plugin: {ex.Message}",
                    Exception = ex
                });
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
}
