using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

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
}
