using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplatesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        [HttpGet]
        public async Task<IActionResult> GetAllTemplates()
        {
            try
            {
                // Use OAPPTemplates instead of Templates since Templates doesn't exist in STARAPI
                var result = await _starAPI.OAPPTemplates.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading templates: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplate(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error loading template: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
        {
            try
            {
                // Create a new template using the TemplateManager
                var result = await _starAPI.OAPPTemplates.CreateAsync(
                    AvatarId,
                    request.Name,
                    request.Description,
                    request.TemplateType,
                    request.Content,
                    null // createOptions
                );
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error creating template: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
        {
            try
            {
                // Load existing template
                var existingResult = await _starAPI.OAPPTemplates.LoadAsync(AvatarId, id, 0);
                
                if (existingResult.IsError || existingResult.Result == null)
                {
                    return BadRequest(new OASISResult<object>
                    {
                        IsError = true,
                        Message = "Template not found",
                        Exception = existingResult.Exception
                    });
                }

                // Update template properties
                var template = existingResult.Result;
                template.Name = request.Name ?? template.Name;
                template.Description = request.Description ?? template.Description;
                
                // Update the template
                var result = await _starAPI.OAPPTemplates.UpdateAsync(AvatarId, template);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error updating template: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting template: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTemplates([FromQuery] string searchTerm)
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

                // Get all templates and filter by search term
                var allTemplatesResult = await _starAPI.OAPPTemplates.LoadAllAsync(AvatarId, null);
                
                if (allTemplatesResult.IsError || allTemplatesResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load templates for search",
                        Exception = allTemplatesResult.Exception
                    });
                }

                // Filter templates by search term
                var filteredTemplates = allTemplatesResult.Result
                    .Where(template => 
                        template.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        template.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = filteredTemplates,
                    IsError = false,
                    Message = $"Found {filteredTemplates.Count} templates matching '{searchTerm}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error searching templates: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<IActionResult> GetTemplatesByType(string type)
        {
            try
            {
                // Get all templates and filter by type
                var allTemplatesResult = await _starAPI.OAPPTemplates.LoadAllAsync(AvatarId, null);
                
                if (allTemplatesResult.IsError || allTemplatesResult.Result == null)
                {
                    return BadRequest(new OASISResult<IEnumerable<object>>
                    {
                        IsError = true,
                        Message = "Failed to load templates",
                        Exception = allTemplatesResult.Exception
                    });
                }

                // Filter templates by type (assuming type is stored in metadata)
                var typeTemplates = allTemplatesResult.Result
                    .Where(template => 
                        template.MetaData?.ContainsKey("TemplateType") == true &&
                        string.Equals(template.MetaData["TemplateType"]?.ToString(), type, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                return Ok(new OASISResult<IEnumerable<object>>
                {
                    Result = typeTemplates,
                    IsError = false,
                    Message = $"Found {typeTemplates.Count} templates of type '{type}'"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading templates by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneTemplate(Guid id, [FromBody] CloneTemplateRequest request)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning template: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
    }

    public class UpdateTemplateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
        public string? Content { get; set; }
    }

    public class CloneTemplateRequest
    {
        public string NewName { get; set; } = string.Empty;
    }
}
