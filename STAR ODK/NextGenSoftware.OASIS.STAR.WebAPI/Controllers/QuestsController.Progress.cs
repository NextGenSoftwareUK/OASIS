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

        /// <summary>
        /// Completes an objective (sub-quest) for a quest.
        /// </summary>
        /// <summary>Apply in-game progress (monster kills, XP, item pickups, keys, level elapsed seconds) to the tracked quest. Updates objective progress and % complete; completes objectives/quest when thresholds are met.</summary>
        [HttpPost("{id}/progress")]
        [ProducesResponseType(typeof(OASISResult<QuestProgressApplyResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<QuestProgressApplyResult>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApplyQuestProgress(Guid id, [FromBody] QuestProgressRequest request)
        {
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();
                if (request == null)
                    return BadRequest(new OASISResult<QuestProgressApplyResult> { IsError = true, Message = "Body required." });
                var delta = new QuestProgressDelta
                {
                    ActiveObjectiveId = request.ActiveObjectiveId,
                    MonstersKilledDelta = request.MonstersKilledDelta,
                    XpEarnedDelta = request.XpEarnedDelta,
                    KeysCollectedDelta = request.KeysCollectedDelta,
                    ArmorCollectedDelta = request.ArmorCollectedDelta,
                    HealthCollectedDelta = request.HealthCollectedDelta,
                    WeaponsCollectedDelta = request.WeaponsCollectedDelta,
                    PowerupsCollectedDelta = request.PowerupsCollectedDelta,
                    AmmoCollectedDelta = request.AmmoCollectedDelta,
                    ItemCollectedName = request.ItemCollectedName ?? string.Empty,
                    GenericItemPickup = request.GenericItemPickup,
                    LevelTimeSeconds = request.LevelTimeSeconds,
                    MonsterKilledClassname = request.MonsterKilledClassname ?? string.Empty
                };
                var result = await _starAPI.Quests.ApplyQuestProgressAsync(AvatarId, id, request.GameSource ?? "ODOOM", delta);
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<QuestProgressApplyResult> { IsError = true, Message = ex.Message });
            }
        }

        /// <summary>Returns CrossGameEventsOnActivate for the first incomplete objective. Call once after StartQuest to get opening audio/narration/video events.</summary>
        [HttpGet("{id}/first-objective-events")]
        [ProducesResponseType(typeof(OASISResult<List<CrossGameEvent>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<List<CrossGameEvent>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFirstObjectiveActivationEvents(Guid id)
        {
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();
                var result = await _starAPI.Quests.GetFirstObjectiveActivationEventsAsync(AvatarId, id);
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<CrossGameEvent>>(ex, "getting first objective activation events");
            }
        }

        /// <summary>Clears all progress dictionaries on the quest and its embedded objectives (kills, collected counts, XP, etc.) to 0; leaves Need* requirements unchanged. If the quest was Completed, status becomes InProgress.</summary>
        [HttpPost("{id}/progress/reset")]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Quest>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetObjectiveProgress(Guid id)
        {
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();
                var result = await _starAPI.Quests.ResetObjectiveProgressAsync(AvatarId, id);
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<Quest>(ex, "resetting objective progress");
            }
        }

        /// <summary>Complete a quest objective using string identifiers in the JSON body (GUIDs or client slugs such as cross_dimensional_keycard_hunt / doom_red_keycard). Native games use this path; route parameters remain Guid-only.</summary>
        [HttpPost("objectives/complete")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteQuestObjectiveByIdentifiers([FromBody] CompleteQuestObjectiveIdentifiersRequest request)
        {
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                if (request == null || string.IsNullOrWhiteSpace(request.QuestId) || string.IsNullOrWhiteSpace(request.ObjectiveId))
                {
                    return BadRequest(new OASISResult<bool> { IsError = true, Message = "questId and objectiveId are required in the JSON body." });
                }

                var avatarId = AvatarId;
                var qToken = request.QuestId.Trim();
                var oToken = request.ObjectiveId.Trim();
                var gs = string.IsNullOrWhiteSpace(request.GameSource) ? null : request.GameSource.Trim();
                var notes = string.IsNullOrWhiteSpace(request.CompletionNotes) ? null : request.CompletionNotes.Trim();

                if (Guid.TryParse(qToken, out var questGuid) && Guid.TryParse(oToken, out var objectiveGuid))
                {
                    var direct = await _starAPI.Quests.CompleteQuestObjectiveAsync(avatarId, questGuid, objectiveGuid, gs, notes);
                    if (direct.IsError)
                        return BadRequest(direct);
                    return Ok(direct);
                }

                var loadResult = await _starAPI.Quests.LoadAllQuestsForAvatarAsync(avatarId);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    return BadRequest(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = loadResult.IsError ? loadResult.Message : "Could not load quests for avatar."
                    });
                }

                var questList = loadResult.Result.ToList();
                var quest = ResolveQuestByClientToken(questList, qToken);
                if (quest == null)
                {
                    return BadRequest(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Quest not found for identifier '{qToken}'."
                    });
                }

                var resolvedObjectiveId = ResolveObjectiveClientToken(quest, oToken, gs);
                if (!resolvedObjectiveId.HasValue)
                {
                    return BadRequest(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Objective not found for identifier '{oToken}' on quest '{quest.Name}' ({quest.Id})."
                    });
                }

                var result = await _starAPI.Quests.CompleteQuestObjectiveAsync(avatarId, quest.Id, resolvedObjectiveId.Value, gs, notes);
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "completing quest objective (by identifiers)");
            }
        }

        [HttpPost("{id}/objectives/{objectiveId}/complete")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteQuestObjective(Guid id, Guid objectiveId, [FromBody] CompleteQuestObjectiveRequest request = null)
        {
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.CompleteQuestObjectiveAsync(
                    AvatarId,
                    id,
                    objectiveId,
                    request?.GameSource,
                    request?.CompletionNotes);

                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "completing quest objective");
            }
        }

        /// <summary>
        /// Completes a quest for the authenticated avatar.
        /// </summary>
        /// <param name="id">The unique identifier of the quest to complete.</param>
        /// <param name="completionNotes">Optional completion notes.</param>
        /// <returns>Result of the quest completion operation.</returns>
        /// <response code="200">Quest completed successfully</response>
        /// <response code="400">Error completing quest</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteQuest(Guid id, [FromBody] string completionNotes = null)
        {
            try
            {
                await EnsureStarApiBootedAsync();
                EnsureLoggedInAvatar();

                var result = await _starAPI.Quests.CompleteQuestAsync(AvatarId, id, completionNotes);
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "completing quest");
            }
        }

        /// <summary>
        /// Gets quest leaderboard for a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <param name="limit">Number of entries to return (default: 50).</param>
        /// <returns>Quest leaderboard entries.</returns>
        /// <response code="200">Leaderboard retrieved successfully</response>
        /// <response code="400">Error retrieving leaderboard</response>
        [HttpGet("{id}/leaderboard")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestLeaderboard>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestLeaderboard>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestLeaderboard(Guid id, [FromQuery] int limit = 50)
        {
            try
            {
                var managerResult = await _starAPI.Quests.GetQuestLeaderboardAsync(id, limit);
                var result = new OASISResult<IEnumerable<QuestLeaderboard>>
                {
                    Result = managerResult.Result,
                    IsError = managerResult.IsError,
                    Message = managerResult.Message,
                    ErrorCode = managerResult.ErrorCode,
                    Exception = managerResult.Exception
                };

                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<QuestLeaderboard>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest leaderboard: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets quest rewards for a specific quest.
        /// </summary>
        /// <param name="id">The unique identifier of the quest.</param>
        /// <returns>Quest rewards.</returns>
        /// <response code="200">Rewards retrieved successfully</response>
        /// <response code="400">Error retrieving rewards</response>
        [HttpGet("{id}/rewards")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestReward>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<QuestReward>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestRewards(Guid id)
        {
            try
            {
                var managerResult = await _starAPI.Quests.GetQuestRewardsAsync(id);
                var result = new OASISResult<IEnumerable<QuestReward>>
                {
                    Result = managerResult.Result,
                    IsError = managerResult.IsError,
                    Message = managerResult.Message,
                    ErrorCode = managerResult.ErrorCode,
                    Exception = managerResult.Exception
                };

                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<QuestReward>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest rewards: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets quest statistics for the authenticated avatar.
        /// </summary>
        /// <returns>Quest statistics.</returns>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="400">Error retrieving statistics</response>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<Dictionary<string, object>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQuestStats()
        {
            try
            {
                var result = await _starAPI.Quests.GetQuestStatsAsync(AvatarId);
                if (result.IsError)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving quest statistics: {ex.Message}",
                    Exception = ex
                });
            }
        }

        private static IQuest? ResolveQuestByClientToken(IReadOnlyList<IQuest> quests, string token)
        {
            if (quests == null || string.IsNullOrWhiteSpace(token))
                return null;
            var t = token.Trim();
            if (Guid.TryParse(t, out var g))
            {
                var byId = quests.FirstOrDefault(q => q.Id == g);
                if (byId != null)
                    return byId;
            }

            var ordered = quests.OrderBy(q => q.ParentQuestId == Guid.Empty ? 0 : 1).ThenBy(q => q.Name ?? "");
            foreach (var q in ordered)
            {
                if (!string.IsNullOrEmpty(q.Name) && string.Equals(q.Name, t, StringComparison.OrdinalIgnoreCase))
                    return q;
            }

            foreach (var q in ordered)
            {
                if (q.MetaData == null)
                    continue;
                foreach (var metaKey in new[] { "Slug", "ExternalId", "QuestSlug", "slug", "id" })
                {
                    if (!q.MetaData.TryGetValue(metaKey, out var mv) || mv == null)
                        continue;
                    if (string.Equals(mv.ToString(), t, StringComparison.OrdinalIgnoreCase))
                        return q;
                }
            }

            var spaced = t.Replace('_', ' ');
            return ordered.FirstOrDefault(q => !string.IsNullOrEmpty(q.Name) &&
                (q.Name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 q.Name.IndexOf(spaced, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static Guid? ResolveObjectiveClientToken(IQuest quest, string token, string? gameSource)
        {
            if (quest == null || string.IsNullOrWhiteSpace(token))
                return null;
            var t = token.Trim();

            if (Guid.TryParse(t, out var og))
            {
                if (quest.Objectives?.Any(o => o.Id == og) == true)
                    return og;
                if (quest.Quests?.Any(sq => sq.Id == og) == true)
                    return og;
            }

            if (quest.Objectives != null)
            {
                foreach (var io in quest.Objectives)
                {
                    if (io is Objective ob && ObjectiveMatchesClientToken(ob, t, gameSource))
                        return ob.Id;
                }
            }

            if (quest.Quests != null)
            {
                foreach (var sq in quest.Quests)
                {
                    if (LegacySubQuestMatchesClientToken(sq, t, gameSource))
                        return sq.Id;
                }
            }

            return null;
        }

        private static bool ObjectiveMatchesClientToken(Objective ob, string token, string? gameSource)
        {
            if (string.Equals(ob.Id.ToString("D"), token, StringComparison.OrdinalIgnoreCase))
                return true;

            if (SlugMatchesObjectiveDictionaries(ob.NeedToCollectKeys, token, gameSource))
                return true;
            if (SlugMatchesObjectiveDictionaries(ob.NeedToCollectItems, token, gameSource))
                return true;

            if (!string.IsNullOrEmpty(ob.ProgressSummary))
            {
                if (ob.ProgressSummary.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                var spaced = token.Replace('_', ' ');
                if (ob.ProgressSummary.IndexOf(spaced, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool SlugMatchesObjectiveDictionaries(IDictionary<string, IList<string>>? dict, string token, string? gameSource)
        {
            if (dict == null)
                return false;
            foreach (var kv in dict)
            {
                if (!SourceKeyMatchesGame(kv.Key, gameSource))
                    continue;
                if (kv.Value == null)
                    continue;
                foreach (var v in kv.Value)
                {
                    if (string.IsNullOrEmpty(v))
                        continue;
                    if (string.Equals(v, token, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (v.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    if (token.Length >= 4 && token.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        private static bool SourceKeyMatchesGame(string? sourceKey, string? gameSource)
        {
            if (string.IsNullOrWhiteSpace(gameSource))
                return true;
            var g = gameSource.Trim();
            var k = sourceKey ?? "";
            if (string.Equals(k, g, StringComparison.OrdinalIgnoreCase))
                return true;
            if (k.IndexOf(g, StringComparison.OrdinalIgnoreCase) >= 0 || g.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            var gUp = g.ToUpperInvariant();
            var kUp = k.ToUpperInvariant();
            if (gUp.Contains("DOOM", StringComparison.Ordinal) && kUp.Contains("DOOM", StringComparison.Ordinal))
                return true;
            if (gUp.Contains("QUAKE", StringComparison.Ordinal) && kUp.Contains("QUAKE", StringComparison.Ordinal))
                return true;
            return false;
        }

        private static bool LegacySubQuestMatchesClientToken(IQuest sq, string token, string? gameSource)
        {
            if (string.Equals(sq.Id.ToString("D"), token, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(sq.Name) && string.Equals(sq.Name, token, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(gameSource) && !string.IsNullOrEmpty(sq.GameSource) &&
                SourceKeyMatchesGame(sq.GameSource, gameSource) &&
                !string.IsNullOrEmpty(sq.Name) &&
                sq.Name.IndexOf(token.Replace('_', ' '), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (sq.MetaData == null)
                return false;
            foreach (var metaKey in new[] { "Slug", "ExternalId", "ObjectiveSlug", "slug", "id" })
            {
                if (!sq.MetaData.TryGetValue(metaKey, out var mv) || mv == null)
                    continue;
                if (string.Equals(mv.ToString(), token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Creates an Objective (Option B) from a create/add objective request: Title, Description, optional Need*/progress dictionaries, optional <see cref="QuestObjectiveRequest.LinkedGeoHotSpotId"/> / <see cref="QuestObjectiveRequest.ExternalHandoffUri"/>.</summary>
        private static IObjective CreateObjectiveFromRequest(QuestObjectiveRequest request, int order)
        {
            var title = request.Title?.Trim() ?? string.Empty;
            var description = request.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Objective title is required.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Objective description is required.");
            if (!HasObjectiveRequirement(request))
                throw new ArgumentException("Objective requires at least one Need* dictionary definition, LinkedGeoHotSpotId, or ExternalHandoffUri.");

            var objective = new Objective
            {
                Id = Guid.NewGuid(),
                Order = request.Order >= 0 ? request.Order : order,
                Title = title,
                Description = description,
                IsCompleted = request.IsCompleted,
                CompletedAt = request.CompletedAt,
                CompletedBy = request.CompletedBy,
                LinkedGeoHotSpotId = request.LinkedGeoHotSpotId,
                ExternalHandoffUri = request.ExternalHandoffUri?.Trim() ?? string.Empty
            };
            if (request.Dictionaries != null)
                CopyDictionariesToObjective(request.Dictionaries, objective);
            return objective;
        }

        private static bool HasObjectiveRequirement(QuestObjectiveRequest request)
        {
            if (request.LinkedGeoHotSpotId.HasValue)
                return true;
            if (!string.IsNullOrWhiteSpace(request.ExternalHandoffUri))
                return true;
            return request.Dictionaries != null && HasAtLeastOneNeedDefinition(request.Dictionaries);
        }

        private static bool HasAtLeastOneNeedDefinition(QuestObjectiveDictionariesRequest dictionaries) =>
            dictionaries.NeedToCollectArmor?.Count > 0 ||
            dictionaries.NeedToCollectAmmo?.Count > 0 ||
            dictionaries.NeedToCollectHealth?.Count > 0 ||
            dictionaries.NeedToCollectWeapons?.Count > 0 ||
            dictionaries.NeedToCollectPowerups?.Count > 0 ||
            dictionaries.NeedToCollectItems?.Count > 0 ||
            dictionaries.NeedToCollectKeys?.Count > 0 ||
            dictionaries.NeedToKillMonsters?.Count > 0 ||
            dictionaries.NeedToCompleteInMins?.Count > 0 ||
            dictionaries.NeedToEarnKarma?.Count > 0 ||
            dictionaries.NeedToEarnXP?.Count > 0 ||
            dictionaries.NeedToGoToGeoHotSpots?.Count > 0 ||
            dictionaries.NeedToCompleteLevel?.Count > 0 ||
            dictionaries.NeedToUseWeapons?.Count > 0 ||
            dictionaries.NeedToUsePowerups?.Count > 0 ||
            dictionaries.NeedToVisitLocations?.Count > 0 ||
            dictionaries.NeedToSurviveMins?.Count > 0;

        private static void CopyDictionariesToObjective(QuestObjectiveDictionariesRequest from, Objective to)
        {
            if (from.NeedToCollectArmor != null) foreach (var kv in from.NeedToCollectArmor) to.NeedToCollectArmor[kv.Key] = kv.Value;
            if (from.NeedToCollectAmmo != null) foreach (var kv in from.NeedToCollectAmmo) to.NeedToCollectAmmo[kv.Key] = kv.Value;
            if (from.NeedToCollectHealth != null) foreach (var kv in from.NeedToCollectHealth) to.NeedToCollectHealth[kv.Key] = kv.Value;
            if (from.NeedToCollectWeapons != null) foreach (var kv in from.NeedToCollectWeapons) to.NeedToCollectWeapons[kv.Key] = kv.Value;
            if (from.NeedToCollectPowerups != null) foreach (var kv in from.NeedToCollectPowerups) to.NeedToCollectPowerups[kv.Key] = kv.Value;
            if (from.NeedToCollectItems != null) foreach (var kv in from.NeedToCollectItems) to.NeedToCollectItems[kv.Key] = kv.Value;
            if (from.NeedToCollectKeys != null) foreach (var kv in from.NeedToCollectKeys) to.NeedToCollectKeys[kv.Key] = kv.Value;
            if (from.NeedToKillMonsters != null) foreach (var kv in from.NeedToKillMonsters) to.NeedToKillMonsters[kv.Key] = kv.Value;
            if (from.NeedToCompleteInMins != null) foreach (var kv in from.NeedToCompleteInMins) to.NeedToCompleteInMins[kv.Key] = kv.Value;
            if (from.NeedToEarnKarma != null) foreach (var kv in from.NeedToEarnKarma) to.NeedToEarnKarma[kv.Key] = kv.Value;
            if (from.NeedToEarnXP != null) foreach (var kv in from.NeedToEarnXP) to.NeedToEarnXP[kv.Key] = kv.Value;
            if (from.NeedToGoToGeoHotSpots != null) foreach (var kv in from.NeedToGoToGeoHotSpots) to.NeedToGoToGeoHotSpots[kv.Key] = kv.Value;
            if (from.NeedToCompleteLevel != null) foreach (var kv in from.NeedToCompleteLevel) to.NeedToCompleteLevel[kv.Key] = kv.Value;
            if (from.NeedToUseWeapons != null) foreach (var kv in from.NeedToUseWeapons) to.NeedToUseWeapons[kv.Key] = kv.Value;
            if (from.NeedToUsePowerups != null) foreach (var kv in from.NeedToUsePowerups) to.NeedToUsePowerups[kv.Key] = kv.Value;
            if (from.NeedToVisitLocations != null) foreach (var kv in from.NeedToVisitLocations) to.NeedToVisitLocations[kv.Key] = kv.Value;
            if (from.NeedToSurviveMins != null) foreach (var kv in from.NeedToSurviveMins) to.NeedToSurviveMins[kv.Key] = kv.Value;
            if (from.ArmorCollected != null) foreach (var kv in from.ArmorCollected) to.ArmorCollected[kv.Key] = kv.Value;
            if (from.AmmoCollected != null) foreach (var kv in from.AmmoCollected) to.AmmoCollected[kv.Key] = kv.Value;
            if (from.HealthCollected != null) foreach (var kv in from.HealthCollected) to.HealthCollected[kv.Key] = kv.Value;
            if (from.WeaponsCollected != null) foreach (var kv in from.WeaponsCollected) to.WeaponsCollected[kv.Key] = kv.Value;
            if (from.PowerupsCollected != null) foreach (var kv in from.PowerupsCollected) to.PowerupsCollected[kv.Key] = kv.Value;
            if (from.ItemsCollected != null) foreach (var kv in from.ItemsCollected) to.ItemsCollected[kv.Key] = kv.Value;
            if (from.KeysCollected != null) foreach (var kv in from.KeysCollected) to.KeysCollected[kv.Key] = kv.Value;
            if (from.MonstersKilled != null) foreach (var kv in from.MonstersKilled) to.MonstersKilled[kv.Key] = kv.Value;
            if (from.TimeStarted != null) foreach (var kv in from.TimeStarted) to.TimeStarted[kv.Key] = kv.Value;
            if (from.TimeEnded != null) foreach (var kv in from.TimeEnded) to.TimeEnded[kv.Key] = kv.Value;
            if (from.TimeTaken != null) foreach (var kv in from.TimeTaken) to.TimeTaken[kv.Key] = kv.Value;
            if (from.KarmaEarnt != null) foreach (var kv in from.KarmaEarnt) to.KarmaEarnt[kv.Key] = kv.Value;
            if (from.XPEarnt != null) foreach (var kv in from.XPEarnt) to.XPEarnt[kv.Key] = kv.Value;
            if (from.GeoHotSpotsArrived != null) foreach (var kv in from.GeoHotSpotsArrived) to.GeoHotSpotsArrived[kv.Key] = kv.Value;
            if (from.LevelsCompleted != null) foreach (var kv in from.LevelsCompleted) to.LevelsCompleted[kv.Key] = kv.Value;
        }
    }
}