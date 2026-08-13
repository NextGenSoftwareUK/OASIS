using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public partial class QuestsController
    {
        /// <summary>
        /// Creates a new quest with specified parameters.
        /// </summary>
        /// <param name="request">Create request containing quest details and source folder path.</param>
        /// <returns>Result of the quest creation operation.</returns>
        /// <response code="200">Quest created successfully</response>
        /// <response code="400">Error creating quest</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateQuestWithOptions([FromBody] CreateQuestRequest request)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.CreateAsync(AvatarId, request.Name, request.Description, request.QuestType, request.SourceFolderPath, request.CreateOptions);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testQuest = TestDataHelper.GetTestQuest();
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest created successfully (using test data)"));
                }
                
                if (result.IsError)
                    return BadRequest(result);

                if (result.Result != null && (request.LinkedGeoHotSpotId.HasValue || !string.IsNullOrWhiteSpace(request.ExternalHandoffUri)))
                {
                    result.Result.LinkedGeoHotSpotId = request.LinkedGeoHotSpotId;
                    result.Result.ExternalHandoffUri = request.ExternalHandoffUri?.Trim() ?? string.Empty;
                    var linkUpdate = await _starAPI.Quests.UpdateAsync(AvatarId, result.Result);
                    if (linkUpdate.IsError)
                        return BadRequest(new OASISResult<Quest> { IsError = true, Message = $"Failed to save quest GeoHotSpot/handoff fields: {linkUpdate.Message}" });
                    result = linkUpdate;
                }

                // Add objectives (Option B: IObjective on Quest.Objectives) if provided.
                if (request.Objectives != null && request.Objectives.Count > 0 && result.Result != null)
                {
                    var quest = result.Result;
                    if (quest.Objectives == null)
                        quest.Objectives = new List<Objective>();
                    int order = 0;
                    foreach (var obj in request.Objectives)
                    {
                        var objective = CreateObjectiveFromRequest(obj, order);
                        quest.Objectives.Add((Objective)objective);
                        order++;
                    }
                    var updateResult = await _starAPI.Quests.UpdateAsync(AvatarId, quest);
                    if (updateResult.IsError)
                        return BadRequest(new OASISResult<Quest> { IsError = true, Message = $"Failed to save objectives: {updateResult.Message}" });
                    result = updateResult;
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testQuest = TestDataHelper.GetTestQuest();
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest created successfully (using test data)"));
                }
                return HandleException<Quest>(ex, "creating quest");
            }
        }

        /// <summary>
        /// Adds an objective (sub-quest) to an existing quest.
        /// </summary>
        /// <param name="id">The parent quest ID.</param>
        /// <param name="request">Objective Title, Description, GameSource, Order, and Dictionaries (at least one Need* entry).</param>
        /// <returns>The created sub-quest (objective) with its ID.</returns>
        [HttpPost("{id}/objectives")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddQuestObjective(Guid id, [FromBody] AddQuestObjectiveRequest request)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                if (request == null)
                    return BadRequest(new OASISResult<Quest> { IsError = true, Message = "Request body is required." });

                var loadResult = await _starAPI.Quests.LoadAsync(AvatarId, id, 0);
                if (loadResult.IsError || loadResult.Result == null)
                    return BadRequest(new OASISResult<Quest> { IsError = true, Message = loadResult.Message ?? "Quest not found." });

                var quest = loadResult.Result;
                if (quest.Objectives == null)
                    quest.Objectives = new List<Objective>();

                var objective = CreateObjectiveFromRequest(new QuestObjectiveRequest
                {
                    Title = request.Title,
                    Description = request.Description,
                    GameSource = request.GameSource,
                    Order = request.Order,
                    Dictionaries = request.Dictionaries,
                    LinkedGeoHotSpotId = request.LinkedGeoHotSpotId,
                    ExternalHandoffUri = request.ExternalHandoffUri
                }, quest.Objectives.Count);

                quest.Objectives.Add((Objective)objective);
                var result = await _starAPI.Quests.UpdateAsync(AvatarId, quest);

                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "adding quest objective");
            }
        }

        /// <summary>
        /// Removes an objective (sub-quest) from a quest.
        /// </summary>
        /// <param name="parentId">The parent quest ID.</param>
        /// <param name="objectiveId">The objective (sub-quest) ID to remove.</param>
        [HttpDelete("{parentId}/objectives/{objectiveId}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveQuestObjective(Guid parentId, Guid objectiveId)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var loadResult = await _starAPI.Quests.LoadAsync(AvatarId, parentId, 0);
                if (loadResult.IsError || loadResult.Result == null)
                    return BadRequest(new OASISResult<Quest> { IsError = true, Message = loadResult.Message ?? "Quest not found." });

                var quest = loadResult.Result;
                if (quest.Objectives != null && quest.Objectives.Count > 0)
                {
                    var removed = quest.Objectives.FirstOrDefault(x => x.Id == objectiveId);
                    if (removed != null)
                    {
                        quest.Objectives.Remove(removed);
                        var updateResult = await _starAPI.Quests.UpdateAsync(AvatarId, quest);
                        if (updateResult.IsError)
                            return BadRequest(updateResult);
                        return Ok(updateResult);
                    }
                }

                var result = await _starAPI.Quests.RemoveQuestAsync(AvatarId, parentId, objectiveId, ProviderType.Default);
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "removing quest objective");
            }
        }

        /// <summary>
        /// Adds a sub-quest (full child quest) to an existing quest. Use for nested quests that can have their own objectives; use POST objectives for checklist items.
        /// </summary>
        /// <param name="id">The parent quest ID.</param>
        /// <param name="request">Sub-quest Name, Description, and optional GameSource (child quest row, not Quest.Objectives checklist).</param>
        /// <returns>The created sub-quest with its ID.</returns>
        [HttpPost("{id}/subquests")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddSubQuest(Guid id, [FromBody] AddSubQuestRequest request)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                if (request == null)
                    return BadRequest(new OASISResult<Quest> { IsError = true, Message = "Request body is required." });

                var subQuest = new Quest
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(request.Name) ? (request.Description?.Trim() ?? "Sub-quest") : request.Name,
                    Description = request.Description?.Trim() ?? "",
                    Order = request.Order >= 0 ? request.Order : 0,
                    Status = QuestStatus.NotStarted,
                    Type = QuestType.SideQuest,
                    QuestType = QuestType.SideQuest,
                    Requirements = new List<string>(),
                    GameSource = request.GameSource?.Trim() ?? "",
                    ParentQuestId = id
                };
                subQuest.STARNETDNA = new STARNETDNA
                {
                    Id = subQuest.Id,
                    Name = subQuest.Name,
                    Description = subQuest.Description,
                    Version = "1.0.0",
                    CreatedByAvatarId = AvatarId,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };

                var result = await _starAPI.Quests.AddQuestAsync(AvatarId, id, subQuest, ProviderType.Default);

                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "adding sub-quest");
            }
        }

        /// <summary>
        /// Removes a sub-quest (child quest) from a quest.
        /// </summary>
        /// <param name="parentId">The parent quest ID.</param>
        /// <param name="subQuestId">The sub-quest ID to remove.</param>
        [HttpDelete("{parentId}/subquests/{subQuestId}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveSubQuest(Guid parentId, Guid subQuestId)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.RemoveQuestAsync(AvatarId, parentId, subQuestId, ProviderType.Default);

                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "removing sub-quest");
            }
        }

        /// <summary>
        /// Loads a quest by ID with optional version and holon type.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to load.</param>
        /// <param name="version">The version of the quest to load (0 for latest).</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The requested quest details.</returns>
        /// <response code="200">Quest loaded successfully</response>
        /// <response code="400">Error loading quest</response>
        [HttpGet("{id}/load")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuest(Guid id, [FromQuery] int version = 0, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<Quest>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.Quests.LoadAsync(AvatarId, id, version, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testQuest = TestDataHelper.GetTestQuest();
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testQuest = TestDataHelper.GetTestQuest();
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest loaded successfully (using test data)"));
                }
                return HandleException<Quest>(ex, "loading quest");
            }
        }

        /// <summary>
        /// Loads a quest from source or installed folder path.
        /// </summary>
        /// <param name="path">The source or installed folder path.</param>
        /// <param name="holonType">The type of holon to load.</param>
        /// <returns>The loaded quest details.</returns>
        /// <response code="200">Quest loaded successfully</response>
        /// <response code="400">Error loading quest</response>
        [HttpGet("load-from-path")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestFromPath([FromQuery] string path, [FromQuery] string holonType = "Default")
        {
            try
            {
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<Quest>(holonType, "holonType");
                if (validationError != null)
                    return validationError;
                var result = await _starAPI.Quests.LoadForSourceOrInstalledFolderAsync(AvatarId, path, holonTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testQuest = TestDataHelper.GetTestQuest();
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest loaded successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testQuest = TestDataHelper.GetTestQuest();
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest loaded successfully (using test data)"));
                }
                return HandleException<Quest>(ex, "loading quest from path");
            }
        }

        /// <summary>
        /// Loads a quest from a published file.
        /// </summary>
        /// <param name="publishedFilePath">The path to the published quest file.</param>
        /// <returns>The loaded quest details.</returns>
        /// <response code="200">Quest loaded successfully</response>
        /// <response code="400">Error loading quest</response>
        [HttpGet("load-from-published")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestFromPublished([FromQuery] string publishedFilePath)
        {
            try
            {
                var result = await _starAPI.Quests.LoadForPublishedFileAsync(AvatarId, publishedFilePath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "loading quest from published file");
            }
        }

        /// <summary>
        /// Loads all quests for the authenticated avatar.
        /// </summary>
        /// <param name="showAllVersions">Whether to show all versions of quests.</param>
        /// <param name="version">Specific version to load (0 for latest).</param>
        /// <returns>List of all quests for the avatar.</returns>
        /// <response code="200">Quests loaded successfully</response>
        /// <response code="400">Error loading quests</response>
        [HttpGet("load-all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadAllQuestsForAvatar([FromQuery] bool showAllVersions = false, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllForAvatarAsync(AvatarId, showAllVersions, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error loading quests for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Publishes a quest to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to publish.</param>
        /// <param name="request">Publish request containing source path, launch target, and publish options.</param>
        /// <returns>Result of the quest publish operation.</returns>
        /// <response code="200">Quest published successfully</response>
        /// <response code="400">Error publishing quest</response>
        [HttpPost("{id}/publish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishQuest(Guid id, [FromBody] PublishRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.PublishAsync(
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
                return HandleException<Quest>(ex, "publishing quest");
            }
        }

        /// <summary>
        /// Downloads a quest from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to download.</param>
        /// <param name="version">The version of the quest to download.</param>
        /// <param name="downloadPath">Optional path where the quest should be downloaded.</param>
        /// <param name="reInstall">Whether to reinstall if already installed.</param>
        /// <returns>Result of the quest download operation.</returns>
        /// <response code="200">Quest downloaded successfully</response>
        /// <response code="400">Error downloading quest</response>
        [HttpPost("{id}/download")]
        [ProducesResponseType(typeof(OASISResult<DownloadedQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<DownloadedQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadQuest(Guid id, [FromQuery] int version = 0, [FromQuery] string downloadPath = "", [FromQuery] bool reInstall = false)
        {
            try
            {
                var result = await _starAPI.Quests.DownloadAsync(AvatarId, id, version, downloadPath, reInstall);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<DownloadedQuest>(ex, "downloading quest");
            }
        }

        /// <summary>
        /// Gets all versions of a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to get versions for.</param>
        /// <returns>List of all versions of the specified quest.</returns>
        /// <response code="200">Versions retrieved successfully</response>
        /// <response code="400">Error retrieving versions</response>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestVersions(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.LoadVersionsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest versions: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Loads a specific version of a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <param name="version">The version string to load.</param>
        /// <returns>The requested quest version details.</returns>
        /// <response code="200">Quest version loaded successfully</response>
        /// <response code="400">Error loading quest version</response>
        [HttpGet("{id}/version/{version}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoadQuestVersion(Guid id, string version)
        {
            try
            {
                var result = await _starAPI.Quests.LoadVersionAsync(id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "loading quest version");
            }
        }

        /// <summary>
        /// Edits a quest with new DNA configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to edit.</param>
        /// <param name="request">Edit request containing new DNA configuration.</param>
        /// <returns>Result of the quest edit operation.</returns>
        /// <response code="200">Quest edited successfully</response>
        /// <response code="400">Error editing quest</response>
        [HttpPost("{id}/edit")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditQuest(Guid id, [FromBody] EditQuestRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.EditAsync(id, request.NewDNA, AvatarId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "editing quest");
            }
        }

        /// <summary>
        /// Unpublishes a quest from the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to unpublish.</param>
        /// <param name="version">The version of the quest to unpublish.</param>
        /// <returns>Result of the quest unpublish operation.</returns>
        /// <response code="200">Quest unpublished successfully</response>
        /// <response code="400">Error unpublishing quest</response>
        [HttpPost("{id}/unpublish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnpublishQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.UnpublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "unpublishing quest");
            }
        }

        /// <summary>
        /// Republishes a quest to the STARNET system.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to republish.</param>
        /// <param name="version">The version of the quest to republish.</param>
        /// <returns>Result of the quest republish operation.</returns>
        /// <response code="200">Quest republished successfully</response>
        /// <response code="400">Error republishing quest</response>
        [HttpPost("{id}/republish")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RepublishQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.RepublishAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "republishing quest");
            }
        }

        /// <summary>
        /// Activates a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to activate.</param>
        /// <param name="version">The version of the quest to activate.</param>
        /// <returns>Result of the quest activation operation.</returns>
        /// <response code="200">Quest activated successfully</response>
        /// <response code="400">Error activating quest</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.ActivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "activating quest");
            }
        }

        /// <summary>
        /// Deactivates a quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to deactivate.</param>
        /// <param name="version">The version of the quest to deactivate.</param>
        /// <returns>Result of the quest deactivation operation.</returns>
        /// <response code="200">Quest deactivated successfully</response>
        /// <response code="400">Error deactivating quest</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateQuest(Guid id, [FromQuery] int version = 0)
        {
            try
            {
                var result = await _starAPI.Quests.DeactivateAsync(AvatarId, id, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "deactivating quest");
            }
        }

        /// <summary>
        /// Checks whether the authenticated avatar can start the quest (quest is NotStarted and prerequisites are met).
        /// Use for the quest popup to enable/disable the Start button. Returns Result=true when the quest can be started, false otherwise with Message explaining why.
        /// </summary>
        [HttpGet("{id}/can-start")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CanStartQuest(Guid id)
        {
            try
            {
                var avatarCheck = ValidateAvatarId<bool>();
                if (avatarCheck != null) return avatarCheck;
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.CanStartQuestAsync(AvatarId, id);
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "checking if quest can be started");
            }
        }

        /// <summary>
        /// Starts a quest for the authenticated avatar. Prerequisites are validated: if the quest has PrerequisiteQuestIds in MetaData, those quests must be completed first. (ParentQuestId is for sub-quests/objectives; when all objectives are complete the parent quest is marked complete.)
        /// </summary>
        [HttpPost("{id}/start")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartQuest(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] string? startNotes = null)
        {
            _logger.LogInformation("[Quests] StartQuest: id={QuestId} AvatarId={AvatarId}", id, AvatarId);
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.StartQuestAsync(AvatarId, id, startNotes);
                _logger.LogInformation("[Quests] StartQuest: result IsError={IsError} Message={Message}", result.IsError, result.Message ?? "(null)");
                if (result.IsError)
                    return BadRequest(result);
                _logger.LogInformation("[Quests] StartQuest: quest start saved for QuestId={QuestId}. If client still shows NotStarted after reopening popup, ensure API uses a persistent storage provider (e.g. MongoDB).", id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Quests] StartQuest: exception QuestId={QuestId} AvatarId={AvatarId}", id, AvatarId);
                return HandleException<bool>(ex, "starting quest");
            }
        }
    }
}
