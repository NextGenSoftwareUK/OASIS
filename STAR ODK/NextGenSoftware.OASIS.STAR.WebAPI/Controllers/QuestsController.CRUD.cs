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
{    public partial class QuestsController
    {
        /// <summary>
        /// Retrieves all quests in the system.
        /// </summary>
        /// <returns>List of all quests available in the STAR system.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests</response>
        [HttpGet]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllIQuests()
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, null);

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testQuests = TestDataHelper.GetTestQuests(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<Quest>>(testQuests, "Quests retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testQuests = TestDataHelper.GetTestQuests(5);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<Quest>>(testQuests, "Quests retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<Quest>>(ex, "GetAllQuests");
            }
        }

        /// <summary>
        /// Retrieves all quests for the current avatar (no status filter).
        /// Returns a flat list of every quest where CreatedByAvatarId matches and Active=1: top-level quests, sub-quests, and objectives (child quests with ParentQuestId set).
        /// Use this for the quest popup; the client filters by status (Not Started, In Progress, Completed) and by ParentQuestId for sub-quests/objectives.
        /// </summary>
        /// <returns>List of all quests for the authenticated avatar (including sub-quests and objectives).</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests</response>
        [HttpGet("all-for-avatar")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllQuestsForAvatar()
        {
            _logger.LogInformation("[Quests] GET all-for-avatar");
            try
            {
                await EnsureStarApiBootedAsync();

                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null)
                    return avatarCheck;

                var avatarId = AvatarId;
                OASISRequestContext.CurrentAvatarId = avatarId;
                OASISRequestContext.CurrentAvatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                EnsureLoggedInAvatar();

                // Use IQuest overload so MetaData is promoted to strongly-typed properties (e.g. Status from MetaData["QuestStatus"])
                var result = await _starAPI.Quests.LoadAllQuestsForAvatarAsync(avatarId);
                if (result.IsError)
                    return BadRequest(result);
                if (result.Result == null || !result.Result.Any())
                {
                    _logger.LogInformation("[Quests] LoadAllQuestsForAvatar returned 0; trying LoadAllAsync fallback.");
                    var fallback = await _starAPI.Quests.LoadAllForAvatarAsync(avatarId);
                    if (fallback.IsError)
                        return BadRequest(fallback);
                    var fallbackList = (fallback.Result ?? Enumerable.Empty<Quest>()).ToList();
                    foreach (var q in fallbackList)
                        NormalizeQuestStatusFromMetaData(q);
                    result = new OASISResult<IEnumerable<IQuest>> { Result = fallbackList, IsError = false, Message = fallback.Message };
                }

                var list = await FilterToLoadableActiveQuestsAsync(avatarId, result.Result ?? Enumerable.Empty<IQuest>());
                var count = list.Count;
                _logger.LogInformation("[Quests] all-for-avatar AvatarId={AvatarId} Count={Count}", avatarId, count);
                var enumerated = list.Take(24).ToList();
                for (var idx = 0; idx < enumerated.Count; idx++)
                    _logger.LogInformation("[Quests]   [{Index}] Id={Id} Name={Name} Status={Status}", idx, enumerated[idx].Id, enumerated[idx].Name ?? "(null)", enumerated[idx].Status.ToString());
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = list,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Same data path as <see cref="GetAllQuestsForAvatar"/> but returns a flat game-friendly DTO (no full holon graph / STARNET children). Use for native clients and games; keep <c>all-for-avatar</c> for tools and graph consumers.
        /// </summary>
        [HttpGet("all-for-avatar/game")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GameQuestSummaryLite>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GameQuestSummaryLite>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllQuestsForAvatarGame()
        {
            _logger.LogInformation("[Quests] GET all-for-avatar/game");
            try
            {
                await EnsureStarApiBootedAsync();

                var avatarCheck = ValidateAvatarId<GameQuestSummaryLite>();
                if (avatarCheck != null)
                    return avatarCheck;

                var avatarId = AvatarId;
                OASISRequestContext.CurrentAvatarId = avatarId;
                OASISRequestContext.CurrentAvatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.LoadAllQuestsForAvatarAsync(avatarId);
                if (result.IsError)
                    return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>> { IsError = true, Message = result.Message, Exception = result.Exception, DetailedMessage = result.DetailedMessage });
                if (result.Result == null || !result.Result.Any())
                {
                    _logger.LogInformation("[Quests] LoadAllQuestsForAvatar returned 0; trying LoadAllAsync fallback (game).");
                    var fallback = await _starAPI.Quests.LoadAllForAvatarAsync(avatarId);
                    if (fallback.IsError)
                        return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>> { IsError = true, Message = fallback.Message, Exception = fallback.Exception, DetailedMessage = fallback.DetailedMessage });
                    var fallbackList = (fallback.Result ?? Enumerable.Empty<Quest>()).ToList();
                    foreach (var q in fallbackList)
                        NormalizeQuestStatusFromMetaData(q);
                    result = new OASISResult<IEnumerable<IQuest>> { Result = fallbackList, IsError = false, Message = fallback.Message };
                }

                var list = await FilterToLoadableActiveQuestsAsync(avatarId, result.Result ?? Enumerable.Empty<IQuest>());
                var lite = list.Select(GameQuestSummaryLiteMapper.ToLite).ToList();
                _logger.LogInformation("[Quests] all-for-avatar/game AvatarId={AvatarId} Count={Count}", avatarId, lite.Count);
                return Ok(new OASISResult<IEnumerable<GameQuestSummaryLite>>
                {
                    Result = lite,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests for avatar: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves a specific quest by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to retrieve.</param>
        /// <returns>The requested quest details.</returns>
        /// <response code="200">Quest retrieved successfully</response>
        /// <response code="400">Error retrieving quest</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetIQuest(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAsync(AvatarId, id, 0);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testQuest = TestDataHelper.GetTestQuest(id);
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest retrieved successfully (using test data)"));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testQuest = TestDataHelper.GetTestQuest(id);
                    return Ok(TestDataHelper.CreateSuccessResult<Quest>(testQuest, "Quest retrieved successfully (using test data)"));
                }
                return HandleException<Quest>(ex, "GetIQuest");
            }
        }

        /// <summary>
        /// Creates a new quest for the authenticated avatar.
        /// </summary>
        /// <param name="quest">The quest details to create.</param>
        /// <returns>The created quest with assigned ID and metadata.</returns>
        /// <response code="200">Quest created successfully</response>
        /// <response code="400">Error creating quest</response>
        [HttpPost]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateIQuest([FromBody] IQuest quest)
        {
            try
            {
                if (quest == null)
                {
                    return BadRequest(new OASISResult<IQuest>
                    {
                        IsError = true,
                        Message = "Quest cannot be null. Please provide a valid Quest object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IQuest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                // Cast to Quest so UpdateAsync uses generic SaveHolonAsync<Quest> and HolonManager mapping runs.
                var result = await _starAPI.Quests.UpdateAsync(AvatarId, (Quest)quest);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IQuest>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IQuest>(ex, "CreateQuest");
            }
        }

        /// <summary>
        /// Updates an existing quest by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to update.</param>
        /// <param name="quest">The updated quest details.</param>
        /// <returns>The updated quest with modified data.</returns>
        /// <response code="200">Quest updated successfully</response>
        /// <response code="400">Error updating quest</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IQuest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateIQuest(Guid id, [FromBody] Quest quest)
        {
            try
            {
                if (quest == null)
                {
                    return BadRequest(new OASISResult<IQuest>
                    {
                        IsError = true,
                        Message = "Quest cannot be null. Please provide a valid Quest object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IQuest>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureStarApiBootedAsync();
                quest.Id = id;
                // UpdateAsync uses generic SaveHolonAsync<Quest> so HolonManager.PrepareHolonForSaving runs.
                var result = await _starAPI.Quests.UpdateAsync(AvatarId, quest);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IQuest>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IQuest>(ex, "updating quest");
            }
        }

        /// <summary>
        /// Deletes a quest by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to delete.</param>
        /// <returns>Confirmation of successful deletion.</returns>
        /// <response code="200">Quest deleted successfully</response>
        /// <response code="400">Error deleting quest</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteIQuest(Guid id)
        {
            try
            {
                var result = await _starAPI.Quests.DeleteAsync(AvatarId, id, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting quest");
            }
        }

        /// <summary>
        /// Retrieves all quests for a specific avatar.
        /// </summary>
        /// <param name="avatarId">The unique identifier of the avatar.</param>
        /// <returns>List of all quests associated with the specified avatar.</returns>
        /// <response code="200">Avatar quests retrieved successfully</response>
        /// <response code="400">Error retrieving avatar quests</response>
        [HttpGet("by-avatar/{avatarId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetIQuestsByAvatar(Guid avatarId)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllForAvatarAsync(avatarId);
                if (result.IsError)
                    return BadRequest(result);

                var list = await FilterToLoadableActiveQuestsAsync(avatarId, (result.Result ?? Enumerable.Empty<Quest>()).Cast<IQuest>());
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = list,
                    IsError = false,
                    Message = "Avatar quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error loading avatar quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Clones an existing quest with a new name.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to clone.</param>
        /// <param name="request">Clone request containing the new name for the cloned quest.</param>
        /// <returns>The newly created cloned quest.</returns>
        /// <response code="200">Quest cloned successfully</response>
        /// <response code="400">Error cloning quest</response>
        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloneQuest(Guid id, [FromBody] CloneRequest request)
        {
            try
            {
                var result = await _starAPI.Quests.CloneAsync(AvatarId, id, request.NewName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "cloning quest");
            }
        }

        /// <summary>
        /// Retrieves quests by a specific type.
        /// </summary>
        /// <param name="type">The quest type to filter by.</param>
        /// <returns>List of quests matching the specified type.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests by type</response>
        [HttpGet("by-type/{type}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestsByType(string type)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var filteredQuests = result.Result?.Where(q => q.QuestType.ToString() == type);
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = filteredQuests,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests by type: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Retrieves quests by status.
        /// </summary>
        /// <param name="status">The quest status to filter by.</param>
        /// <returns>List of quests matching the specified status.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error retrieving quests by status</response>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestsByStatus(string status)
        {
            _logger.LogInformation("[Quests] GET by-status/{Status}", status ?? "(null)");
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return BadRequest(new OASISResult<IEnumerable<Quest>> { IsError = true, Message = "Status is required (e.g. InProgress, NotStarted, Completed)." });

                await EnsureStarApiBootedAsync();

                var avatarCheck = ValidateAvatarId<Quest>();
                if (avatarCheck != null)
                    return avatarCheck;

                var avatarId = AvatarId;
                _logger.LogInformation("[Quests] Request AvatarId={AvatarId} (compare with seed output: 'Avatar ID for quests: <id>')", avatarId);
                OASISRequestContext.CurrentAvatarId = avatarId;
                OASISRequestContext.CurrentAvatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                EnsureLoggedInAvatar();

                /* Load quests for this avatar (by MetaData CreatedByAvatarId + Active); fallback to LoadAllAsync if empty. */
                var result = await _starAPI.Quests.LoadAllForAvatarAsync(avatarId);
                if (result.IsError)
                    return BadRequest(result);
                var fromAvatar = result.Result?.Count() ?? 0;
                if (result.Result == null || !result.Result.Any())
                {
                    _logger.LogInformation("[Quests] LoadAllForAvatar(CreatedByAvatarId={AvatarId}) returned 0; trying LoadAllAsync fallback.", avatarId);
                    result = await _starAPI.Quests.LoadAllAsync(avatarId, 0);
                    if (result.IsError)
                        return BadRequest(result);
                    var fromAll = result.Result?.Count() ?? 0;
                    _logger.LogInformation("[Quests] LoadAllAsync fallback returned {Count} quests (if 0, storage may be empty or use a persistent provider e.g. MongoDB).", fromAll);
                    if (fromAll > 0)
                    {
                        foreach (var q in (result.Result ?? Enumerable.Empty<Quest>()).Take(10))
                            _logger.LogInformation("[Quests]   Quest Id={QuestId} Name={Name} CreatedByAvatarId={CreatedBy}", q?.Id, q?.Name, q?.STARNETDNA?.CreatedByAvatarId ?? default);
                    }
                }
                else
                {
                    _logger.LogInformation("[Quests] LoadAllForAvatar returned {Count} quests.", fromAvatar);
                    foreach (var q in (result.Result ?? Enumerable.Empty<Quest>()).Take(10))
                        _logger.LogInformation("[Quests]   Quest Id={QuestId} Name={Name} CreatedByAvatarId={CreatedBy}", q?.Id, q?.Name, q?.STARNETDNA?.CreatedByAvatarId ?? default);
                }

                var list = result.Result ?? Enumerable.Empty<Quest>();
                var totalLoaded = list.Count();
                var statusTrimmed = status.Trim();
                var filteredQuests = list.Where(q => q != null && string.Equals((q.Status).ToString(), statusTrimmed, StringComparison.OrdinalIgnoreCase)).ToList();
                _logger.LogInformation("[Quests] AvatarId={AvatarId} Loaded={Total} AfterStatusFilter({Status})={Filtered}", avatarId, totalLoaded, statusTrimmed, filteredQuests.Count);
                if (totalLoaded > 0)
                {
                    foreach (var q in filteredQuests.Take(5))
                        _logger.LogInformation("[Quests] Returning quest Id={Id} Name={Name} Status={Status} CreatedByAvatarId={CreatedBy}", q?.Id, q?.Name, q?.Status.ToString(), q?.STARNETDNA?.CreatedByAvatarId ?? default);
                }
                if (totalLoaded > 0 && filteredQuests.Count == 0)
                {
                    _logger.LogInformation("[Quests] (No quests matched status {Status}; showing first 5 loaded for debug:)", statusTrimmed);
                    foreach (var q in list.Take(5))
                        _logger.LogInformation("[Quests]   Quest Id={Id} Name={Name} Status={Status} CreatedByAvatarId={CreatedBy}", q?.Id, q?.Name, q?.Status.ToString(), q?.STARNETDNA?.CreatedByAvatarId ?? default);
                }
                if (totalLoaded == 0)
                {
                    _logger.LogWarning(
                        "[Quests] 0 quests returned. Request AvatarId={AvatarId}. Compare with seed output (Avatar ID for quests: <id>). If different, beam in with the same avatar. Ensure API uses a persistent storage provider (e.g. MongoDB).",
                        avatarId);
                }
                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = filteredQuests ?? new List<Quest>(),
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " Inner: " + ex.InnerException.Message;
                var detailed = ex.StackTrace;
                if (ex.InnerException?.StackTrace != null)
                    detailed += Environment.NewLine + "Inner: " + ex.InnerException.StackTrace;
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests by status: {msg}",
                    DetailedMessage = detailed,
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Same filtering as <see cref="GetQuestsByStatus"/> but returns <see cref="GameQuestSummaryLite"/> rows for small payloads in games.
        /// </summary>
        [HttpGet("by-status/{status}/game")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GameQuestSummaryLite>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<GameQuestSummaryLite>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestsByStatusGame(string status)
        {
            _logger.LogInformation("[Quests] GET by-status/{Status}/game", status ?? "(null)");
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>> { IsError = true, Message = "Status is required (e.g. InProgress, NotStarted, Completed)." });

                await EnsureStarApiBootedAsync();

                var avatarCheck = ValidateAvatarId<GameQuestSummaryLite>();
                if (avatarCheck != null)
                    return avatarCheck;

                var avatarId = AvatarId;
                OASISRequestContext.CurrentAvatarId = avatarId;
                OASISRequestContext.CurrentAvatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.LoadAllForAvatarAsync(avatarId);
                if (result.IsError)
                    return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>> { IsError = true, Message = result.Message, Exception = result.Exception, DetailedMessage = result.DetailedMessage });
                if (result.Result == null || !result.Result.Any())
                {
                    result = await _starAPI.Quests.LoadAllAsync(avatarId, 0);
                    if (result.IsError)
                        return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>> { IsError = true, Message = result.Message, Exception = result.Exception, DetailedMessage = result.DetailedMessage });
                }

                var list = result.Result ?? Enumerable.Empty<Quest>();
                foreach (var q in list)
                    NormalizeQuestStatusFromMetaData(q);

                var statusTrimmed = status.Trim();
                var filtered = list.Where(q => q != null && string.Equals(q.Status.ToString(), statusTrimmed, StringComparison.OrdinalIgnoreCase)).Select(GameQuestSummaryLiteMapper.ToLite).ToList();
                _logger.LogInformation("[Quests] by-status/game AvatarId={AvatarId} Status={Status} Count={Count}", avatarId, statusTrimmed, filtered.Count);
                return Ok(new OASISResult<IEnumerable<GameQuestSummaryLite>>
                {
                    Result = filtered,
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<GameQuestSummaryLite>>
                {
                    IsError = true,
                    Message = $"Error retrieving quests by status: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches quests by name or description.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <returns>List of quests matching the search query.</returns>
        /// <response code="200">Quests retrieved successfully</response>
        /// <response code="400">Error searching quests</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<Quest>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchQuests([FromQuery] string query)
        {
            try
            {
                var result = await _starAPI.Quests.LoadAllAsync(AvatarId, 0);
                if (result.IsError)
                    return BadRequest(result);

                var list = result.Result ?? Enumerable.Empty<Quest>();
                var filteredQuests = list.Where(q =>
                    q?.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    q?.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true).ToList();

                return Ok(new OASISResult<IEnumerable<Quest>>
                {
                    Result = filteredQuests ?? new List<Quest>(),
                    IsError = false,
                    Message = "Quests retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<Quest>>
                {
                    IsError = true,
                    Message = $"Error searching quests: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
    }
}