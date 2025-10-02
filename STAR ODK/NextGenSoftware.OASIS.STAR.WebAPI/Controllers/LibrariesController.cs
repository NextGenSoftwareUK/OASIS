using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Libraries management endpoints for creating, updating, and managing STAR libraries.
    /// Libraries represent collections of code, templates, and reusable components within the STAR ecosystem.
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading libraries: {ex.Message}",
                    Exception = ex
                });
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading library: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error creating library: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error updating library: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting library: {ex.Message}",
                    Exception = ex
                });
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
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning library: {ex.Message}",
                    Exception = ex
                });
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
}
