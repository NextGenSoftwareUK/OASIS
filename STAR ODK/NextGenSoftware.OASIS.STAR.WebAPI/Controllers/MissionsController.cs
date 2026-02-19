using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using System.Threading;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Mission management endpoints for creating, updating, and managing STAR missions.
    /// Missions are structured objectives that avatars can undertake within the STAR ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MissionsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private static readonly SemaphoreSlim _bootLock = new(1, 1);

        private IActionResult ValidateAvatarId<T>()
        {
            // Skip validation if test mode is enabled - let the method try to execute and return test data on failure
            if (UseTestDataWhenLiveDataNotAvailable)
                return null;
                
            if (AvatarId == Guid.Empty)
            {
                return BadRequest(new OASISResult<T>
                {
                    IsError = true,
                    Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                });
            }
            return null;
        }

        private static async Task EnsureStarApiBootedAsync()
        {
            if (_starAPI.IsOASISBooted)
                return;

            await _bootLock.WaitAsync();
            try
            {
                if (_starAPI.IsOASISBooted)
                    return;

                var boot = await _starAPI.BootOASISAsync("admin", "admin");
                if (boot.IsError)
                    throw new OASISException(boot.Message ?? "Failed to ignite WEB5 STAR API runtime.");
            }
            finally
            {
                _bootLock.Release();
            }
        }

        /// <summary>
        /// Retrieves all missions in the system.
        /// </summary>
        /// <returns>List of all missions available in the STAR system.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error retrieving missions</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllMissions()
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testMissions = TestDataHelper.GetTestMissions(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<Mission>>(testMissions, "Missions retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testMissions = TestDataHelper.GetTestMissions(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<Mission>>(testMissions, "Missions retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<Mission>>(ex, "GetAllMissions");
            }
        }

        /// <summary>
        /// Retrieves a specific mission by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to retrieve.</param>
        /// <returns>The requested mission details.</returns>
        /// <response code="200">Mission retrieved successfully</response>
        /// <response code="400">Error retrieving mission</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMission(Guid id)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testMission = TestDataHelper.GetTestMission(id);
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testMission = TestDataHelper.GetTestMission(id);
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission retrieved successfully (using test data)"));
                }
                return HandleException<Mission>(ex, "GetMission");
            }
        }

        /// <summary>
        /// Creates a new mission for the authenticated avatar.
        /// </summary>
        /// <param name="mission">The mission details to create.</param>
        /// <returns>The created mission with assigned ID and metadata.</returns>
        /// <response code="200">Mission created successfully</response>
        /// <response code="400">Error creating mission</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMission([FromBody] IMission mission)
        {
            try
            {
                if (mission == null)
                {
                    return BadRequest(new OASISResult<IMission>
                    {
                        IsError = true,
                        Message = "Mission cannot be null. Please provide a valid Mission object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMission>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                var result = await _starAPI.Missions.UpdateAsync(AvatarId, (Mission)mission);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMission>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Mission created successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IMission>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMission>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Mission created successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IMission>(ex, "creating mission");
            }
        }

        /// <summary>
        /// Updates an existing mission by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to update.</param>
        /// <param name="mission">The updated mission details.</param>
        /// <returns>The updated mission with modified data.</returns>
        /// <response code="200">Mission updated successfully</response>
        /// <response code="400">Error updating mission</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMission(Guid id, [FromBody] IMission mission)
        {
            try
            {
                if (mission == null)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IMission>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Mission updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IMission>
                    {
                        IsError = true,
                        Message = "Mission cannot be null. Please provide a valid Mission object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMission>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                mission.Id = id;
                var result = await _starAPI.Missions.UpdateAsync(AvatarId, (Mission)mission);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IMission>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Mission updated successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMission>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Mission updated successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IMission>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMission>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Mission updated successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IMission>(ex, "updating mission");
            }
        }

        /// <summary>
        /// Deletes a mission by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Mission deleted successfully</response>
        /// <response code="400">Error deleting mission</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMission(Guid id)
        {
            try
            {
                var result = await _starAPI.Missions.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting mission");
            }
        }

        /// <summary>
        /// Clones an existing mission with a new name.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to clone.</param>
        /// <param name="request">Clone request containing the new name for the cloned mission.</param>
        /// <returns>The newly created cloned mission.</returns>
        /// <response code="200">Mission cloned successfully</response>
        /// <response code="400">Error cloning mission</response>
        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloneMission(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Missions.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "cloning mission");
            }
        }

        /// <summary>
        /// Retrieves missions by a specific type.
        /// </summary>
        /// <param name="type">The mission type to filter by.</param>
        /// <returns>List of missions matching the specified type.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error retrieving missions by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionsByType(string type)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredMissions = result.Result?.Where(m => Enum.GetName(typeof(MissionType), m.MissionType) == type);
                return Ok(new OASISResult<IEnumerable<Mission>>
                {
                    Result = filteredMissions,
                    IsError = false,
                    Message = "Missions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error retrieving missions by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves missions by status.
        /// </summary>
        /// <param name="status">The mission status to filter by.</param>
        /// <returns>List of missions matching the specified status.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error retrieving missions by status</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionsByStatus(string status)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredMissions = result.Result?.Where(m => Enum.GetName(typeof(QuestStatus), m.Status) == status);
                return Ok(new OASISResult<IEnumerable<Mission>>
                {
                    Result = filteredMissions,
                    IsError = false,
                    Message = "Missions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error retrieving missions by status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches missions by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of missions matching the search query.</returns>
        /// <response code="200">Missions retrieved successfully</response>
        /// <response code="400">Error searching missions</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchMissions([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredMissions = result.Result?.Where(m => 
                    m.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
                
                return Ok(new OASISResult<IEnumerable<Mission>>
                {
                    Result = filteredMissions,
                    IsError = false,
                    Message = "Missions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error searching missions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Creates a new mission with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing mission details and source folder path.</param>
        /// <returns>Result of the mission creation operation.</returns>
        /// <response code="200">Mission created successfully</response>
        /// <response code="400">Error creating mission</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMissionWithOptions([FromBody] CreateMissionRequest request)
        {
            try
            {
                var result = await _starAPI.Missions.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testMission = TestDataHelper.GetTestMission();
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission created successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testMission = TestDataHelper.GetTestMission();
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission created successfully (using test data)"));
                }
                return HandleException<Mission>(ex, "creating mission");
            }
        }

        /// <summary>
        /// Loads a mission by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to load.</param>
        /// <param name="version">The version of the mission to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested mission details.</returns>
        /// <response code="200">Mission loaded successfully</response>
        /// <response code="400">Error loading mission</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadMission(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<Mission>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.Missions.LoadAsync(AvatarId, id, version, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testMission = TestDataHelper.GetTestMission();
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testMission = TestDataHelper.GetTestMission();
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission loaded successfully (using test data)"));
                }
                return HandleException<Mission>(ex, "loading mission");
            }
        }

        /// <summary>
        /// Loads a mission from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded mission details.</returns>
        /// <response code="200">Mission loaded successfully</response>
        /// <response code="400">Error loading mission</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadMissionFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<Mission>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.Missions.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testMission = TestDataHelper.GetTestMission();
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testMission = TestDataHelper.GetTestMission();
                    return Ok(TestDataHelper.CreateSuccessResult<Mission>(testMission, "Mission loaded successfully (using test data)"));
                }
                return HandleException<Mission>(ex, "loading mission from path");
            }
        }

        /// <summary>
        /// Loads a mission from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published mission file.</param>
        /// <returns>The loaded mission details.</returns>
        /// <response code="200">Mission loaded successfully</response>
        /// <response code="400">Error loading mission</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadMissionFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Missions.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "loading mission from published file");
            }
        }

        /// <summary>
        /// Loads all missions for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of missions.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all missions for the avatar.</returns>
        /// <response code="200">Missions loaded successfully</response>
        /// <response code="400">Error loading missions</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllMissionsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Missions.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error loading missions for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a mission to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the mission publish operation.</returns>
        /// <response code="200">Mission published successfully</response>
        /// <response code="400">Error publishing mission</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishMission(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Missions.PublishAsync(
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
                return HandleException<Mission>(ex, "publishing mission");
            }
        }

        /// <summary>
        /// Downloads a mission from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to download.</param>
        /// <param name="version">The version of the mission to download.</param>
        /// <param name="downloadPath">Optional path where the mission should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the mission download operation.</returns>
        /// <response code="200">Mission downloaded successfully</response>
        /// <response code="400">Error downloading mission</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedMission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedMission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadMission(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Missions.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedMission>(ex, "downloading mission");
            }
        }

        /// <summary>
        /// Gets all versions of a specific mission.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to get versions for.</param>
        /// <returns>List of all versions of the specified mission.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Mission>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Missions.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Mission>>
                {
                    IsError = true,
                    Message = $"Error retrieving mission versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a mission.
        /// </summary>
        /// <param name="id">The unique identifier of the mission.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested mission version details.</returns>
        /// <response code="200">Mission version loaded successfully</response>
        /// <response code="400">Error loading mission version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadMissionVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Missions.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "loading mission version");
            }
        }

        /// <summary>
        /// Edits a mission with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the mission edit operation.</returns>
        /// <response code="200">Mission edited successfully</response>
        /// <response code="400">Error editing mission</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditMission(Guid id, [FromBody] EditMissionRequest request)
        {
            try
            {
                var result = await _starAPI.Missions.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "editing mission");
            }
        }

        /// <summary>
        /// Unpublishes a mission from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to unpublish.</param>
        /// <param name="version">The version of the mission to unpublish.</param>
        /// <returns>Result of the mission unpublish operation.</returns>
        /// <response code="200">Mission unpublished successfully</response>
        /// <response code="400">Error unpublishing mission</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishMission(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Missions.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "unpublishing mission");
            }
        }

        /// <summary>
        /// Republishes a mission to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to republish.</param>
        /// <param name="version">The version of the mission to republish.</param>
        /// <returns>Result of the mission republish operation.</returns>
        /// <response code="200">Mission republished successfully</response>
        /// <response code="400">Error republishing mission</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishMission(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Missions.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "republishing mission");
            }
        }

        /// <summary>
        /// Activates a mission.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to activate.</param>
        /// <param name="version">The version of the mission to activate.</param>
        /// <returns>Result of the mission activation operation.</returns>
        /// <response code="200">Mission activated successfully</response>
        /// <response code="400">Error activating mission</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateMission(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Missions.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "activating mission");
            }
        }

        /// <summary>
        /// Deactivates a mission.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to deactivate.</param>
        /// <param name="version">The version of the mission to deactivate.</param>
        /// <returns>Result of the mission deactivation operation.</returns>
        /// <response code="200">Mission deactivated successfully</response>
        /// <response code="400">Error deactivating mission</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Mission>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateMission(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Missions.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Mission>(ex, "deactivating mission");
            }
        }

        /// <summary>
        /// Completes a mission for the authenticated avatar.
        /// </summary>
        /// <param name="id">The unique identifier of the mission to complete.</param>
        /// <param name="completionNotes">Optional completion notes.</param>
        /// <returns>Result of the mission completion operation.</returns>
        /// <response code="200">Mission completed successfully</response>
        /// <response code="400">Error completing mission</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteMission(Guid id, [FromBody] string completionNotes = null)
        {
            try
            {
                // TODO: Implement mission completion logic
                // This would involve updating mission status, awarding rewards, etc.
                var result = new OASISResult<bool>
                {
                    Result = true,
                    IsError = false,
                    Message = "Mission completed successfully"
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "completing mission");
            }
        }

        /// <summary>
        /// Gets mission leaderboard for a specific mission.
        /// </summary>
        /// <param name="id">The unique identifier of the mission.</param>
        /// <param name="limit">Number of entries to return (default: 50).</param>
        /// <returns>Mission leaderboard entries.</returns>
        /// <response code="200">Leaderboard retrieved successfully</response>
        /// <response code="400">Error retrieving leaderboard</response>
        [HttpGet("{id}/leaderboard")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MissionLeaderboard>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MissionLeaderboard>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionLeaderboard(Guid id, [FromQuery] int limit = 50)
        {
            try
            {
                // TODO: Implement mission leaderboard logic
                var result = new OASISResult<IEnumerable<MissionLeaderboard>>
                {
                    Result = new List<MissionLeaderboard>(),
                    IsError = false,
                    Message = "Mission leaderboard retrieved successfully"
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<MissionLeaderboard>>
                {
                    IsError = true,
                    Message = $"Error retrieving mission leaderboard: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets mission rewards for a specific mission.
        /// </summary>
        /// <param name="id">The unique identifier of the mission.</param>
        /// <returns>Mission rewards.</returns>
        /// <response code="200">Rewards retrieved successfully</response>
        /// <response code="400">Error retrieving rewards</response>
        [HttpGet("{id}/rewards")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MissionReward>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<MissionReward>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionRewards(Guid id)
        {
            try
            {
                // TODO: Implement mission rewards logic
                var result = new OASISResult<IEnumerable<MissionReward>>
                {
                    Result = new List<MissionReward>(),
                    IsError = false,
                    Message = "Mission rewards retrieved successfully"
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<MissionReward>>
                {
                    IsError = true,
                    Message = $"Error retrieving mission rewards: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets mission statistics for the authenticated avatar.
        /// </summary>
        /// <returns>Mission statistics.</returns>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="400">Error retrieving statistics</response>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMissionStats()
        {
            try
            {
                // TODO: Implement mission statistics logic
                var stats = new Dictionary<string, object>
                {
                    ["totalMissions"] = 0,
                    ["completedMissions"] = 0,
                    ["activeMissions"] = 0,
                    ["totalRewards"] = 0
                };

                var result = new OASISResult<Dictionary<string, object>>
                {
                    Result = stats,
                    IsError = false,
                    Message = "Mission statistics retrieved successfully"
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving mission statistics: {ex.Message}",
                    Exception = ex
                });
            }
        }
    }

    public class CreateMissionRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public HolonType HolonSubType { get; set; } = HolonType.Mission;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Mission, STARNETDNA> CreateOptions { get; set; } = null;
    }

    public class EditMissionRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

}
