using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Inventory Items management endpoints for creating, updating, and managing STAR inventory items.
    /// Inventory items represent collectible items, resources, and assets that avatars can own and trade (such as add-ons, armor, weapsons etc for your Avatar &amp; much more!).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryItemsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all inventory items in the system.
        /// </summary>
        /// <returns>List of all inventory items available in the STAR system.</returns>
        /// <response code="200">Inventory items retrieved successfully</response>
        /// <response code="400">Error retrieving inventory items</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllInventoryItems()
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAllAsync(AvatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading inventory items: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific inventory item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to retrieve.</param>
        /// <returns>The requested inventory item details.</returns>
        /// <response code="200">Inventory item retrieved successfully</response>
        /// <response code="400">Error retrieving inventory item</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventoryItem(Guid id)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error loading inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new inventory item for the authenticated avatar.
        /// </summary>
        /// <param name="item">The inventory item details to create.</param>
        /// <returns>The created inventory item with assigned ID and metadata.</returns>
        /// <response code="200">Inventory item created successfully</response>
        /// <response code="400">Error creating inventory item</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateInventoryItem([FromBody] InventoryItem item)
        {
            try
            {
                var result = await _starAPI.InventoryItems.UpdateAsync(AvatarId, (InventoryItem)item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error creating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventoryItem(Guid id, [FromBody] InventoryItem item)
        {
            try
            {
                item.Id = id;
                var result = await _starAPI.InventoryItems.UpdateAsync(AvatarId, (InventoryItem)item);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error updating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryItem(Guid id)
        {
            try
            {
                var result = await _starAPI.InventoryItems.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpGet("by-avatar/{avatarId}")]
        public async Task<IActionResult> GetInventoryItemsByAvatar(Guid avatarId)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAllAsync(avatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading avatar inventory items: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new inventory item with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing inventory item details and source folder path.</param>
        /// <returns>Result of the inventory item creation operation.</returns>
        /// <response code="200">Inventory item created successfully</response>
        /// <response code="400">Error creating inventory item</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateInventoryItemWithOptions([FromBody] CreateInventoryItemRequest request)
        {
            try
            {
                var result = await _starAPI.InventoryItems.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error creating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an inventory item by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to load.</param>
        /// <param name="version">The version of the inventory item to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested inventory item details.</returns>
        /// <response code="200">Inventory item loaded successfully</response>
        /// <response code="400">Error loading inventory item</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadInventoryItem(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.InventoryItems.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error loading inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an inventory item from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded inventory item details.</returns>
        /// <response code="200">Inventory item loaded successfully</response>
        /// <response code="400">Error loading inventory item</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadInventoryItemFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.InventoryItems.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error loading inventory item from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an inventory item from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published inventory item file.</param>
        /// <returns>The loaded inventory item details.</returns>
        /// <response code="200">Inventory item loaded successfully</response>
        /// <response code="400">Error loading inventory item</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadInventoryItemFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error loading inventory item from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all inventory items for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of inventory items.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all inventory items for the avatar.</returns>
        /// <response code="200">Inventory items loaded successfully</response>
        /// <response code="400">Error loading inventory items</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllInventoryItemsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading inventory items for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes an inventory item to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the inventory item publish operation.</returns>
        /// <response code="200">Inventory item published successfully</response>
        /// <response code="400">Error publishing inventory item</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishInventoryItem(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.InventoryItems.PublishAsync(
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
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error publishing inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads an inventory item from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to download.</param>
        /// <param name="version">The version of the inventory item to download.</param>
        /// <param name="downloadPath">Optional path where the inventory item should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the inventory item download operation.</returns>
        /// <response code="200">Inventory item downloaded successfully</response>
        /// <response code="400">Error downloading inventory item</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedInventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadInventoryItem(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.InventoryItems.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DownloadedInventoryItem>
                {
                    IsError = true,
                    Message = $"Error downloading inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific inventory item.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to get versions for.</param>
        /// <returns>List of all versions of the specified inventory item.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInventoryItemVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error retrieving inventory item versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of an inventory item.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested inventory item version details.</returns>
        /// <response code="200">Inventory item version loaded successfully</response>
        /// <response code="400">Error loading inventory item version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadInventoryItemVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.InventoryItems.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error loading inventory item version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits an inventory item with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the inventory item edit operation.</returns>
        /// <response code="200">Inventory item edited successfully</response>
        /// <response code="400">Error editing inventory item</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditInventoryItem(Guid id, [FromBody] EditInventoryItemRequest request)
        {
            try
            {
                var result = await _starAPI.InventoryItems.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error editing inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes an inventory item from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to unpublish.</param>
        /// <param name="version">The version of the inventory item to unpublish.</param>
        /// <returns>Result of the inventory item unpublish operation.</returns>
        /// <response code="200">Inventory item unpublished successfully</response>
        /// <response code="400">Error unpublishing inventory item</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishInventoryItem(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.InventoryItems.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error unpublishing inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes an inventory item to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to republish.</param>
        /// <param name="version">The version of the inventory item to republish.</param>
        /// <returns>Result of the inventory item republish operation.</returns>
        /// <response code="200">Inventory item republished successfully</response>
        /// <response code="400">Error republishing inventory item</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishInventoryItem(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.InventoryItems.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error republishing inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates an inventory item.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to activate.</param>
        /// <param name="version">The version of the inventory item to activate.</param>
        /// <returns>Result of the inventory item activation operation.</returns>
        /// <response code="200">Inventory item activated successfully</response>
        /// <response code="400">Error activating inventory item</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateInventoryItem(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.InventoryItems.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error activating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates an inventory item.
        /// </summary>
        /// <param name="id">The unique identifier of the inventory item to deactivate.</param>
        /// <param name="version">The version of the inventory item to deactivate.</param>
        /// <returns>Result of the inventory item deactivation operation.</returns>
        /// <response code="200">Inventory item deactivated successfully</response>
        /// <response code="400">Error deactivating inventory item</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<InventoryItem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateInventoryItem(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.InventoryItems.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<InventoryItem>
                {
                    IsError = true,
                    Message = $"Error deactivating inventory item: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for inventory items based on the provided search criteria.
        /// </summary>
        /// <param name="request">Search request containing search parameters.</param>
        /// <returns>List of inventory items matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<InventoryItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchInventoryItems([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _starAPI.InventoryItems.SearchAsync<InventoryItem>(AvatarId, request.SearchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, true, false, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<InventoryItem>>
                {
                    IsError = true,
                    Message = $"Error searching inventory items: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateInventoryItemRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.InventoryItem;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<InventoryItem, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditInventoryItemRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadedInventoryItem
    {
        public InventoryItem InventoryItem { get; set; } = new InventoryItem();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
