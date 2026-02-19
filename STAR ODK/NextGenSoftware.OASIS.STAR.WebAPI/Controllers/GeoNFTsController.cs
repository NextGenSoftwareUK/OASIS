using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Geo-NFTs management endpoints for creating, updating, and managing STAR Geo-NFTs.
    /// Geo-NFTs represent location-based non-fungible tokens within the OASIS Omniverse/Our World.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GeoNFTsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all geo NFTs in the system.
        /// </summary>
        /// <returns>List of all geo NFTs available in the STAR system.</returns>
        /// <response code="200">Geo NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving geo NFTs</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllGeoNFTs()
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllAsync(AvatarId, 0);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testGeoNFTs = new List<STARGeoNFT>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARGeoNFT>>(testGeoNFTs, "Geo NFTs retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testGeoNFTs = new List<STARGeoNFT>();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARGeoNFT>>(testGeoNFTs, "Geo NFTs retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<STARGeoNFT>>(ex, "GetAllGeoNFTs");
            }
        }

        /// <summary>
        /// Retrieves a specific geo NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to retrieve.</param>
        /// <returns>The requested geo NFT details.</returns>
        /// <response code="200">Geo NFT retrieved successfully</response>
        /// <response code="400">Error retrieving geo NFT</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    return Ok(TestDataHelper.CreateSuccessResult<STARGeoNFT>(null, "Geo NFT retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(TestDataHelper.CreateSuccessResult<STARGeoNFT>(null, "Geo NFT retrieved successfully (using test data)"));
                }
                return HandleException<STARGeoNFT>(ex, "GetGeoNFT");
            }
        }

        /// <summary>
        /// Creates a new geo NFT for the authenticated avatar.
        /// </summary>
        /// <param name="geoNFT">The geo NFT details to create.</param>
        /// <returns>The created geo NFT with assigned ID and metadata.</returns>
        /// <response code="200">Geo NFT created successfully</response>
        /// <response code="400">Error creating geo NFT</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGeoNFT([FromBody] STARGeoNFT geoNFT)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.UpdateAsync(AvatarId, geoNFT);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "creating geo NFT");
            }
        }

        /// <summary>
        /// Updates an existing geo NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to update.</param>
        /// <param name="geoNFT">The updated geo NFT details.</param>
        /// <returns>The updated geo NFT with modified data.</returns>
        /// <response code="200">Geo NFT updated successfully</response>
        /// <response code="400">Error updating geo NFT</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGeoNFT(Guid id, [FromBody] STARGeoNFT geoNFT)
        {
            try
            {
                geoNFT.Id = id;
                var result = await _starAPI.GeoNFTs.UpdateAsync(AvatarId, geoNFT);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "updating geo NFT");
            }
        }

        /// <summary>
        /// Deletes a geo NFT by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Geo NFT deleted successfully</response>
        /// <response code="400">Error deleting geo NFT</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGeoNFT(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting geo NFT");
            }
        }

        /// <summary>
        /// Retrieves geo NFTs within a specified radius of a geographic location.
        /// </summary>
        /// <param name="latitude">The latitude coordinate for the search center.</param>
        /// <param name="longitude">The longitude coordinate for the search center.</param>
        /// <param name="radiusKm">The search radius in kilometers (default: 10.0).</param>
        /// <returns>List of geo NFTs within the specified radius.</returns>
        /// <response code="200">Nearby geo NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving nearby geo NFTs</response>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNearbyGeoNFTs([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            try
            {
                throw new NotImplementedException("LoadAllNearAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading nearby geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves geo NFTs associated with a specific avatar.
        /// </summary>
        /// <param name="avatarId">The unique identifier of the avatar.</param>
        /// <returns>List of geo NFTs associated with the specified avatar.</returns>
        /// <response code="200">Avatar geo NFTs retrieved successfully</response>
        /// <response code="400">Error retrieving avatar geo NFTs</response>
        [HttpGet("by-avatar/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoNFTsByAvatar(Guid avatarId)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllAsync(avatarId, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading avatar geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches geo NFTs by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of geo NFTs matching the search query.</returns>
        /// <response code="200">Geo NFTs retrieved successfully</response>
        /// <response code="400">Error searching geo NFTs</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchGeoNFTs([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredGeoNFTs = result.Result?.Where(gnft => 
                    gnft.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    gnft.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    Result = filteredGeoNFTs,
                    IsError = false,
                    Message = "Geo NFTs retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error searching geo NFTs: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new geo NFT with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing geo NFT details and source folder path.</param>
        /// <returns>Result of the geo NFT creation operation.</returns>
        /// <response code="200">Geo NFT created successfully</response>
        /// <response code="400">Error creating geo NFT</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGeoNFTWithOptions([FromBody] CreateGeoNFTRequest request)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "creating geo NFT");
            }
        }

        /// <summary>
        /// Loads a geo NFT by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to load.</param>
        /// <param name="version">The version of the geo NFT to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested geo NFT details.</returns>
        /// <response code="200">Geo NFT loaded successfully</response>
        /// <response code="400">Error loading geo NFT</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoNFT(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<IHolon>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.GeoNFTs.LoadAsync(AvatarId, id, version, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "loading geo NFT");
            }
        }

        /// <summary>
        /// Loads a geo NFT from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded geo NFT details.</returns>
        /// <response code="200">Geo NFT loaded successfully</response>
        /// <response code="400">Error loading geo NFT</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoNFTFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<IHolon>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.GeoNFTs.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "loading geo NFT from path");
            }
        }

        /// <summary>
        /// Loads a geo NFT from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published geo NFT file.</param>
        /// <returns>The loaded geo NFT details.</returns>
        /// <response code="200">Geo NFT loaded successfully</response>
        /// <response code="400">Error loading geo NFT</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoNFTFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "loading geo NFT from published file");
            }
        }

        /// <summary>
        /// Loads all geo NFTs for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of geo NFTs.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all geo NFTs for the avatar.</returns>
        /// <response code="200">Geo NFTs loaded successfully</response>
        /// <response code="400">Error loading geo NFTs</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllGeoNFTsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error loading geo NFTs for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a geo NFT to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the geo NFT publish operation.</returns>
        /// <response code="200">Geo NFT published successfully</response>
        /// <response code="400">Error publishing geo NFT</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishGeoNFT(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.PublishAsync(
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
                return HandleException<STARGeoNFT>(ex, "publishing geo NFT");
            }
        }

        /// <summary>
        /// Downloads a geo NFT from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to download.</param>
        /// <param name="version">The version of the geo NFT to download.</param>
        /// <param name="downloadPath">Optional path where the geo NFT should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the geo NFT download operation.</returns>
        /// <response code="200">Geo NFT downloaded successfully</response>
        /// <response code="400">Error downloading geo NFT</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadGeoNFT(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedSTARGeoNFT>(ex, "downloading geo NFT");
            }
        }

        /// <summary>
        /// Gets all versions of a specific geo NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to get versions for.</param>
        /// <returns>List of all versions of the specified geo NFT.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARGeoNFT>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeoNFTVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARGeoNFT>>
                {
                    IsError = true,
                    Message = $"Error retrieving geo NFT versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a geo NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested geo NFT version details.</returns>
        /// <response code="200">Geo NFT version loaded successfully</response>
        /// <response code="400">Error loading geo NFT version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadGeoNFTVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "loading geo NFT version");
            }
        }

        /// <summary>
        /// Edits a geo NFT with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the geo NFT edit operation.</returns>
        /// <response code="200">Geo NFT edited successfully</response>
        /// <response code="400">Error editing geo NFT</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditGeoNFT(Guid id, [FromBody] EditGeoNFTRequest request)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "editing geo NFT");
            }
        }

        /// <summary>
        /// Unpublishes a geo NFT from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to unpublish.</param>
        /// <param name="version">The version of the geo NFT to unpublish.</param>
        /// <returns>Result of the geo NFT unpublish operation.</returns>
        /// <response code="200">Geo NFT unpublished successfully</response>
        /// <response code="400">Error unpublishing geo NFT</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishGeoNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "unpublishing geo NFT");
            }
        }

        /// <summary>
        /// Republishes a geo NFT to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to republish.</param>
        /// <param name="version">The version of the geo NFT to republish.</param>
        /// <returns>Result of the geo NFT republish operation.</returns>
        /// <response code="200">Geo NFT republished successfully</response>
        /// <response code="400">Error republishing geo NFT</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishGeoNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "republishing geo NFT");
            }
        }

        /// <summary>
        /// Activates a geo NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to activate.</param>
        /// <param name="version">The version of the geo NFT to activate.</param>
        /// <returns>Result of the geo NFT activation operation.</returns>
        /// <response code="200">Geo NFT activated successfully</response>
        /// <response code="400">Error activating geo NFT</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateGeoNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "activating geo NFT");
            }
        }

        /// <summary>
        /// Deactivates a geo NFT.
        /// </summary>
        /// <param name="id">The unique identifier of the geo NFT to deactivate.</param>
        /// <param name="version">The version of the geo NFT to deactivate.</param>
        /// <returns>Result of the geo NFT deactivation operation.</returns>
        /// <response code="200">Geo NFT deactivated successfully</response>
        /// <response code="400">Error deactivating geo NFT</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARGeoNFT>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateGeoNFT(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.GeoNFTs.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARGeoNFT>(ex, "deactivating geo NFT");
            }
        }
    }

    public class CreateGeoNFTRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Web5GeoNFT;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<STARGeoNFT, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditGeoNFTRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }


    public class DownloadedSTARGeoNFT
    {
        public STARGeoNFT GeoNFT { get; set; } = new STARGeoNFT();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
