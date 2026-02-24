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
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Templates management endpoints for creating, updating, and managing STAR templates.
    /// Templates represent reusable code patterns, configurations, and project structures within the STAR ecosystem.
    /// </summary>
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
                var result = await _starAPI.OAPPTemplates.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testTemplates = new List<object>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<object>>(testTemplates, "Templates retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testTemplates = new List<object>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<object>>(testTemplates, "Templates retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<object>>(ex, "GetAllTemplates");
            }
        }

        /// <summary>
        /// Retrieves a specific template by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the template to retrieve.</param>
        /// <returns>The requested template details.</returns>
        /// <response code="200">Template retrieved successfully</response>
        /// <response code="400">Error retrieving template</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTemplate(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    return Ok(TestDataHelper.CreateSuccessResult<object>(null, "Template retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(TestDataHelper.CreateSuccessResult<object>(null, "Template retrieved successfully (using test data)"));
                }
                return HandleException<object>(ex, "GetTemplate");
            }
        }

        /// <summary>
        /// Creates a new template for the authenticated avatar.
        /// </summary>
        /// <param name="request">The template creation request details.</param>
        /// <returns>The created template with assigned ID and metadata.</returns>
        /// <response code="200">Template created successfully</response>
        /// <response code="400">Error creating template</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, TemplateType, and optional Content." });
            var validationError = ValidateCreateRequest(request.Name, request.Description);
            if (validationError != null)
                return validationError;
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
                return HandleException<object>(ex, "creating template");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name and/or Description." });
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
                return HandleException<object>(ex, "updating template");
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
                return HandleException<bool>(ex, "deleting template");
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
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with NewName." });
            try
            {
                var result = await _starAPI.OAPPTemplates.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "cloning template");
            }
        }

        /// <summary>
        /// Creates a new template with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing template details and source folder path.</param>
        /// <returns>Result of the template creation operation.</returns>
        /// <response code="200">Template created successfully</response>
        /// <response code="400">Error creating template</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTemplateWithOptions([FromBody] CreateTemplateWithOptionsRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional HolonSubType, SourceFolderPath, CreateOptions." });
            var validationError = ValidateCreateRequest(request.Name, request.Description);
            if (validationError != null)
                return validationError;
            try
            {
                var result = await _starAPI.OAPPTemplates.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "creating template");
            }
        }

        /// <summary>
        /// Loads a template from the specified path.
        /// </summary>
        /// <param name="path">The path to load the template from.</param>
        /// <returns>Result of the template load operation.</returns>
        /// <response code="200">Template loaded successfully</response>
        /// <response code="400">Error loading template</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadTemplateFromPath([FromQuery] string path)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadForSourceOrInstalledFolderAsync(AvatarId, path, HolonType.OAPPTemplate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading template from path");
            }
        }

        /// <summary>
        /// Loads a template from published sources.
        /// </summary>
        /// <param name="id">The unique identifier of the template to load from published.</param>
        /// <returns>Result of the template load operation.</returns>
        /// <response code="200">Template loaded successfully</response>
        /// <response code="400">Error loading template</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadTemplateFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading template from published");
            }
        }

        /// <summary>
        /// Loads all templates for the current avatar.
        /// </summary>
        /// <returns>List of all templates for the avatar.</returns>
        /// <response code="200">Templates loaded successfully</response>
        /// <response code="400">Error loading templates</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllTemplatesForAvatar()
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadAllForAvatarAsync(AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error loading templates for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for templates based on the provided search criteria.
        /// </summary>
        /// <param name="request">Search request containing search parameters.</param>
        /// <returns>List of templates matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchTemplates([FromBody] SearchRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<IEnumerable<object>> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with SearchTerm." });
            try
            {
                var result = await _starAPI.OAPPTemplates.SearchAsync<OAPPTemplate>(AvatarId, request.SearchTerm, default, null, MetaKeyValuePairMatchMode.All, true, false, 0);
                return Ok(result);
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

        /// <summary>
        /// Publishes a template to the STARNET system with optional cloud upload.
        /// </summary>
        /// <param name="id">The unique identifier of the template to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the template publish operation.</returns>
        /// <response code="200">Template published successfully</response>
        /// <response code="400">Error publishing template</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishTemplate(Guid id, [FromBody] PublishRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with SourcePath, LaunchTarget, and optional publish options." });
            try
            {
                var result = await _starAPI.OAPPTemplates.PublishAsync(
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
                return HandleException<object>(ex, "publishing template");
            }
        }

        /// <summary>
        /// Downloads a template from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the template to download.</param>
        /// <param name="request">Download request containing destination path and options.</param>
        /// <returns>Result of the template download operation.</returns>
        /// <response code="200">Template downloaded successfully</response>
        /// <response code="400">Error downloading template</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadTemplate(Guid id, [FromBody] DownloadTemplateRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with DestinationPath and optional Overwrite." });
            try
            {
                var result = await _starAPI.OAPPTemplates.DownloadAsync(AvatarId, id, 0, request.DestinationPath, request.Overwrite);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "downloading template");
            }
        }

        /// <summary>
        /// Gets all versions of a template.
        /// </summary>
        /// <param name="id">The unique identifier of the template.</param>
        /// <returns>List of all versions for the template.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTemplateVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<object>>
                {
                    IsError = true,
                    Message = $"Error getting template versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a template.
        /// </summary>
        /// <param name="id">The unique identifier of the template.</param>
        /// <param name="version">The version to load.</param>
        /// <returns>Result of the template version load operation.</returns>
        /// <response code="200">Template version loaded successfully</response>
        /// <response code="400">Error loading template version</response>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadTemplateVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "loading template version");
            }
        }

        /// <summary>
        /// Edits a template with the provided changes.
        /// </summary>
        /// <param name="id">The unique identifier of the template to edit.</param>
        /// <param name="request">Edit request containing the changes to apply.</param>
        /// <returns>Result of the template edit operation.</returns>
        /// <response code="200">Template edited successfully</response>
        /// <response code="400">Error editing template</response>
        [HttpPut("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditTemplate(Guid id, [FromBody] EditTemplateRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with NewDNA." });
            try
            {
                var result = await _starAPI.OAPPTemplates.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "editing template");
            }
        }

        /// <summary>
        /// Unpublishes a template from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the template to unpublish.</param>
        /// <returns>Result of the template unpublish operation.</returns>
        /// <response code="200">Template unpublished successfully</response>
        /// <response code="400">Error unpublishing template</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishTemplate(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.UnpublishAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "unpublishing template");
            }
        }

        /// <summary>
        /// Republishes a template to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the template to republish.</param>
        /// <param name="request">Republish request containing updated parameters.</param>
        /// <returns>Result of the template republish operation.</returns>
        /// <response code="200">Template republished successfully</response>
        /// <response code="400">Error republishing template</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishTemplate(Guid id, [FromBody] PublishRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The request body is required. Please provide a valid JSON body for republish options." });
            try
            {
                var result = await _starAPI.OAPPTemplates.RepublishAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "republishing template");
            }
        }

        /// <summary>
        /// Activates a template in the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the template to activate.</param>
        /// <returns>Result of the template activation operation.</returns>
        /// <response code="200">Template activated successfully</response>
        /// <response code="400">Error activating template</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateTemplate(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.ActivateAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "activating template");
            }
        }

        /// <summary>
        /// Deactivates a template in the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the template to deactivate.</param>
        /// <returns>Result of the template deactivation operation.</returns>
        /// <response code="200">Template deactivated successfully</response>
        /// <response code="400">Error deactivating template</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateTemplate(Guid id)
        {
            try
            {
                var result = await _starAPI.OAPPTemplates.DeactivateAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "deactivating template");
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

    public class CreateTemplateWithOptionsRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HolonType HolonSubType { get; set; } = HolonType.OAPPTemplate;
        public string SourceFolderPath { get; set; } = string.Empty;
        public ISTARNETCreateOptions<OAPPTemplate, STARNETDNA>? CreateOptions { get; set; }
    }

    public class EditTemplateRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadTemplateRequest
    {
        public string DestinationPath { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = false;
    }
}
