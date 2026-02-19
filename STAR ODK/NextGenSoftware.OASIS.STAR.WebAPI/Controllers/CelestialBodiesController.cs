using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Celestial Bodies management endpoints for creating, updating, and managing STAR celestial bodies.
    /// Celestial bodies represent planets, stars, moons, and other astronomical objects in the OASIS Omniverse.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CelestialBodiesController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        /// <summary>
        /// Retrieves all celestial bodies in the system.
        /// </summary>
        /// <returns>List of all celestial bodies available in the STAR system.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error retrieving celestial bodies</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllCelestialBodies()
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testBodies = TestDataHelper.GetTestCelestialBodies(5).Cast<STARCelestialBody>().ToList();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARCelestialBody>>(testBodies, "Celestial bodies retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testBodies = TestDataHelper.GetTestCelestialBodies(5).Cast<STARCelestialBody>().ToList();
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<STARCelestialBody>>(testBodies, "Celestial bodies retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<STARCelestialBody>>(ex, "GetAllCelestialBodies");
            }
        }

        /// <summary>
        /// Retrieves a specific celestial body by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to retrieve.</param>
        /// <returns>The requested celestial body details.</returns>
        /// <response code="200">Celestial body retrieved successfully</response>
        /// <response code="400">Error retrieving celestial body</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBody(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body retrieved successfully (using test data)"));
                }
                return HandleException<STARCelestialBody>(ex, "GetCelestialBody");
            }
        }

        /// <summary>
        /// Creates a new celestial body for the authenticated avatar.
        /// </summary>
        /// <param name="celestialBody">The celestial body details to create.</param>
        /// <returns>The created celestial body with assigned ID and metadata.</returns>
        /// <response code="200">Celestial body created successfully</response>
        /// <response code="400">Error creating celestial body</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialBody([FromBody] STARCelestialBody celestialBody)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.UpdateAsync(AvatarId, celestialBody);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "creating celestial body");
            }
        }

        /// <summary>
        /// Updates an existing celestial body by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to update.</param>
        /// <param name="celestialBody">The updated celestial body details.</param>
        /// <returns>The updated celestial body with modified data.</returns>
        /// <response code="200">Celestial body updated successfully</response>
        /// <response code="400">Error updating celestial body</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCelestialBody(Guid id, [FromBody] STARCelestialBody celestialBody)
        {
            try
            {
                celestialBody.Id = id;
                var result = await _starAPI.CelestialBodies.UpdateAsync(AvatarId, celestialBody);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "updating celestial body");
            }
        }

        /// <summary>
        /// Deletes a celestial body by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Celestial body deleted successfully</response>
        /// <response code="400">Error deleting celestial body</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCelestialBody(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting celestial body");
            }
        }

        /// <summary>
        /// Retrieves celestial bodies by a specific type.
        /// </summary>
        /// <param name="type">The celestial body type to filter by.</param>
        /// <returns>List of celestial bodies matching the specified type.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error retrieving celestial bodies by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodiesByType(string type)
        {
            try
            {
                throw new NotImplementedException("LoadAllOfTypeAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies of type {type}: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves celestial bodies within a specific celestial space.
        /// </summary>
        /// <param name="spaceId">The unique identifier of the celestial space.</param>
        /// <returns>List of celestial bodies within the specified space.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error retrieving celestial bodies in space</response>
        [HttpGet("in-space/{spaceId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodiesInSpace(Guid spaceId)
        {
            try
            {
                throw new NotImplementedException("LoadAllInSpaceAsync method not yet implemented");
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies in space: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches celestial bodies by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of celestial bodies matching the search query.</returns>
        /// <response code="200">Celestial bodies retrieved successfully</response>
        /// <response code="400">Error searching celestial bodies</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchCelestialBodies([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredCelestialBodies = result.Result?.Where(cb => 
                    cb.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    cb.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    Result = filteredCelestialBodies,
                    IsError = false,
                    Message = "Celestial bodies retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error searching celestial bodies: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new celestial body with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing celestial body details and source folder path.</param>
        /// <returns>Result of the celestial body creation operation.</returns>
        /// <response code="200">Celestial body created successfully</response>
        /// <response code="400">Error creating celestial body</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCelestialBodyWithOptions([FromBody] CreateCelestialBodyRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body created successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body created successfully (using test data)"));
                }
                return HandleException<STARCelestialBody>(ex, "creating celestial body");
            }
        }

        /// <summary>
        /// Loads a celestial body by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to load.</param>
        /// <param name="version">The version of the celestial body to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested celestial body details.</returns>
        /// <response code="200">Celestial body loaded successfully</response>
        /// <response code="400">Error loading celestial body</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBody(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<STARCelestialBody>(holonType, "holonType");
                if (validationError != null)
                    return validationError;

                var result = await _starAPI.CelestialBodies.LoadAsync(AvatarId, id, version, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body loaded successfully (using test data)"));
                }
                return HandleException<STARCelestialBody>(ex, "loading celestial body");
            }
        }

        /// <summary>
        /// Loads a celestial body from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded celestial body details.</returns>
        /// <response code="200">Celestial body loaded successfully</response>
        /// <response code="400">Error loading celestial body</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBodyFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<STARCelestialBody>(holonType, "holonType");
                if (validationError != null)
                    return validationError;

                var result = await _starAPI.CelestialBodies.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testBody = TestDataHelper.GetTestCelestialBodies(1).FirstOrDefault() as STARCelestialBody;
                    return Ok(TestDataHelper.CreateSuccessResult<STARCelestialBody>(testBody, "Celestial body loaded successfully (using test data)"));
                }
                return HandleException<STARCelestialBody>(ex, "loading celestial body from path");
            }
        }

        /// <summary>
        /// Loads a celestial body from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published celestial body file.</param>
        /// <returns>The loaded celestial body details.</returns>
        /// <response code="200">Celestial body loaded successfully</response>
        /// <response code="400">Error loading celestial body</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBodyFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "loading celestial body from published file");
            }
        }

        /// <summary>
        /// Loads all celestial bodies for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of celestial bodies.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all celestial bodies for the avatar.</returns>
        /// <response code="200">Celestial bodies loaded successfully</response>
        /// <response code="400">Error loading celestial bodies</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllCelestialBodiesForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error loading celestial bodies for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a celestial body to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the celestial body publish operation.</returns>
        /// <response code="200">Celestial body published successfully</response>
        /// <response code="400">Error publishing celestial body</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishCelestialBody(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.PublishAsync(
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
                return HandleException<STARCelestialBody>(ex, "publishing celestial body");
            }
        }

        /// <summary>
        /// Downloads a celestial body from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to download.</param>
        /// <param name="version">The version of the celestial body to download.</param>
        /// <param name="downloadPath">Optional path where the celestial body should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the celestial body download operation.</returns>
        /// <response code="200">Celestial body downloaded successfully</response>
        /// <response code="400">Error downloading celestial body</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedSTARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadCelestialBody(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedSTARCelestialBody>(ex, "downloading celestial body");
            }
        }

        /// <summary>
        /// Gets all versions of a specific celestial body.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to get versions for.</param>
        /// <returns>List of all versions of the specified celestial body.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<STARCelestialBody>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCelestialBodyVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<STARCelestialBody>>
                {
                    IsError = true,
                    Message = $"Error retrieving celestial body versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a celestial body.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested celestial body version details.</returns>
        /// <response code="200">Celestial body version loaded successfully</response>
        /// <response code="400">Error loading celestial body version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadCelestialBodyVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "loading celestial body version");
            }
        }

        /// <summary>
        /// Edits a celestial body with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the celestial body edit operation.</returns>
        /// <response code="200">Celestial body edited successfully</response>
        /// <response code="400">Error editing celestial body</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditCelestialBody(Guid id, [FromBody] EditCelestialBodyRequest request)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "editing celestial body");
            }
        }

        /// <summary>
        /// Unpublishes a celestial body from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to unpublish.</param>
        /// <param name="version">The version of the celestial body to unpublish.</param>
        /// <returns>Result of the celestial body unpublish operation.</returns>
        /// <response code="200">Celestial body unpublished successfully</response>
        /// <response code="400">Error unpublishing celestial body</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishCelestialBody(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "unpublishing celestial body");
            }
        }

        /// <summary>
        /// Republishes a celestial body to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to republish.</param>
        /// <param name="version">The version of the celestial body to republish.</param>
        /// <returns>Result of the celestial body republish operation.</returns>
        /// <response code="200">Celestial body republished successfully</response>
        /// <response code="400">Error republishing celestial body</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishCelestialBody(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "republishing celestial body");
            }
        }

        /// <summary>
        /// Activates a celestial body.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to activate.</param>
        /// <param name="version">The version of the celestial body to activate.</param>
        /// <returns>Result of the celestial body activation operation.</returns>
        /// <response code="200">Celestial body activated successfully</response>
        /// <response code="400">Error activating celestial body</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateCelestialBody(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "activating celestial body");
            }
        }

        /// <summary>
        /// Deactivates a celestial body.
        /// </summary>
        /// <param name="id">The unique identifier of the celestial body to deactivate.</param>
        /// <param name="version">The version of the celestial body to deactivate.</param>
        /// <returns>Result of the celestial body deactivation operation.</returns>
        /// <response code="200">Celestial body deactivated successfully</response>
        /// <response code="400">Error deactivating celestial body</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<STARCelestialBody>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateCelestialBody(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.CelestialBodies.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<STARCelestialBody>(ex, "deactivating celestial body");
            }
        }
    }

    public class CreateCelestialBodyRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.STARCelestialBody;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<STARCelestialBody, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditCelestialBodyRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public class DownloadedSTARCelestialBody
    {
        public STARCelestialBody CelestialBody { get; set; } = new STARCelestialBody();
        public string DownloadPath { get; set; } = "";
        public bool Success { get; set; } = false;
    }

}
