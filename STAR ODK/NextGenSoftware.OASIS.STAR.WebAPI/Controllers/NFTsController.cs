using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
// duplicate using removed

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// NFTs management endpoints for creating, updating, and managing STAR NFTs.
    /// NFTs represent non-fungible tokens and digital assets within the STAR universe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NFTsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all NFTs in the system.
        /// </summary>
        /// <returns>List of all NFTs available in the STAR system.</returns>
        /// <response code="200">NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving NFTs</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllNFTs()
        {
            try
            {
                var result = await _starAPI.NFTs.LoadAllAsync(AvatarId, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARNFT>>
                {
                    IsError = true,
                    Message = $"Error loading NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to retrieve.</param>
        /// <returns>The requested NFT details.</returns>
        /// <response code="200">NFT retrieved successfully</response>
        /// <response code="400">Error retrieving NFT</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.NFTs.LoadAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new NFT for the authenticated avatar.
        /// </summary>
        /// <param name="nft">The NFT details to create.</param>
        /// <returns>The created NFT with assigned ID and metadata.</returns>
        /// <response code="200">NFT created successfully</response>
        /// <response code="400">Error creating NFT</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateNFT([FromBody] STARNFT nft)
        {
            try
            {
                var result = await _starAPI.NFTs.UpdateAsync(AvatarId, nft);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error creating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNFT(Guid id, [FromBody] STARNFT nft)
        {
            try
            {
                nft.Id = id;
                var result = await _starAPI.NFTs.UpdateAsync(AvatarId, nft);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error updating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.NFTs.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<IActionResult> CloneNFT(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.NFTs.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<object>
                {
                    IsError = true,
                    Message = $"Error cloning NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new NFT with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing NFT details and source folder path.</param>
        /// <returns>Result of the NFT creation operation.</returns>
        /// <response code="200">NFT created successfully</response>
        /// <response code="400">Error creating NFT</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateNFTWithOptions([FromBody] CreateNFTRequest request)
        {
            try
            {
                // CreateOptions can be null - the CreateAsync method should handle it
                var result = await _starAPI.NFTs.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions ?? null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error creating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an NFT by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to load.</param>
        /// <param name="version">The version of the NFT to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested NFT details.</returns>
        /// <response code="200">NFT loaded successfully</response>
        /// <response code="400">Error loading NFT</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadNFT(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.NFTs.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an NFT from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded NFT details.</returns>
        /// <response code="200">NFT loaded successfully</response>
        /// <response code="400">Error loading NFT</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadNFTFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(holonType);
                var result = await _starAPI.NFTs.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT from path: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads an NFT from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published NFT file.</param>
        /// <returns>The loaded NFT details.</returns>
        /// <response code="200">NFT loaded successfully</response>
        /// <response code="400">Error loading NFT</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadNFTFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.NFTs.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT from published file: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads all NFTs for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of NFTs.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all NFTs for the avatar.</returns>
        /// <response code="200">NFTs loaded successfully</response>
        /// <response code="400">Error loading NFTs</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllNFTsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.NFTs.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARNFT>>
                {
                    IsError = true,
                    Message = $"Error loading NFTs for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches for NFTs by name or description.
        /// </summary>
        /// <param name="searchTerm">The search term to look for in NFT names and descriptions.</param>
        /// <param name="searchOnlyForCurrentAvatar">Whether to search only for current avatar's NFTs.</param>
        /// <param name="showAllVersions">Whether to show all versions of matching NFTs.</param>
        /// <param name="version">Specific version to search for (0 for latest).</param>
        /// <returns>List of NFTs matching the search criteria.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchNFTs([FromQuery] string searchTerm, [FromQuery] bool searchOnlyForCurrentAvatar = true, [FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.NFTs.SearchAsync<STARNFT>(AvatarId, searchTerm, default(Guid), null, MetaKeyValuePairMatchMode.All, searchOnlyForCurrentAvatar, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARNFT>>
                {
                    IsError = true,
                    Message = $"Error searching NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes an NFT to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the NFT publish operation.</returns>
        /// <response code="200">NFT published successfully</response>
        /// <response code="400">Error publishing NFT</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishNFT(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.NFTs.PublishAsync(
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
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error publishing NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Downloads an NFT from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to download.</param>
        /// <param name="version">The version of the NFT to download.</param>
        /// <param name="downloadPath">Optional path where the NFT should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the NFT download operation.</returns>
        /// <response code="200">NFT downloaded successfully</response>
        /// <response code="400">Error downloading NFT</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadNFT(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.NFTs.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<DownloadedSTARNFT>
                {
                    IsError = true,
                    Message = $"Error downloading NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all versions of a specific NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to get versions for.</param>
        /// <returns>List of all versions of the specified NFT.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNFTVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.NFTs.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARNFT>>
                {
                    IsError = true,
                    Message = $"Error retrieving NFT versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of an NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested NFT version details.</returns>
        /// <response code="200">NFT version loaded successfully</response>
        /// <response code="400">Error loading NFT version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadNFTVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.NFTs.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error loading NFT version: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Edits an NFT with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the NFT edit operation.</returns>
        /// <response code="200">NFT edited successfully</response>
        /// <response code="400">Error editing NFT</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditNFT(Guid id, [FromBody] EditNFTRequest request)
        {
            try
            {
                var result = await _starAPI.NFTs.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error editing NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Unpublishes an NFT from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to unpublish.</param>
        /// <param name="version">The version of the NFT to unpublish.</param>
        /// <returns>Result of the NFT unpublish operation.</returns>
        /// <response code="200">NFT unpublished successfully</response>
        /// <response code="400">Error unpublishing NFT</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.NFTs.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error unpublishing NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Republishes an NFT to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to republish.</param>
        /// <param name="version">The version of the NFT to republish.</param>
        /// <returns>Result of the NFT republish operation.</returns>
        /// <response code="200">NFT republished successfully</response>
        /// <response code="400">Error republishing NFT</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.NFTs.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error republishing NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Activates an NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to activate.</param>
        /// <param name="version">The version of the NFT to activate.</param>
        /// <returns>Result of the NFT activation operation.</returns>
        /// <response code="200">NFT activated successfully</response>
        /// <response code="400">Error activating NFT</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.NFTs.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error activating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Deactivates an NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the NFT to deactivate.</param>
        /// <param name="version">The version of the NFT to deactivate.</param>
        /// <returns>Result of the NFT deactivation operation.</returns>
        /// <response code="200">NFT deactivated successfully</response>
        /// <response code="400">Error deactivating NFT</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.NFTs.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<STARNFT>
                {
                    IsError = true,
                    Message = $"Error deactivating NFT: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateNFTRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Web5NFT;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<STARNFT, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditNFTRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadedSTARNFT
    {
        public STARNFT NFT { get; set; } = new STARNFT();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}


