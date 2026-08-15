using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class QuestManager
    {
        //TODO: Need to show this on STAR CLI ASAP! ;-)
        public async Task<OASISResult<IQuest>> GetCurentSubQuestForQuestAsync(Guid avatarId, Guid questId, ProviderType providerType)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.GetCurentStageForQuestAsync. Reason:";

            OASISResult<Quest> loadResult = await LoadAsync(avatarId, questId, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.Quests != null && loadResult.Result.Quests.Count() > 0)
                    {
                        result.Result = loadResult.Result.Quests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

                        if (result.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} No sub-quest was found that is not completed!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No sub-quests were found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The quest was already completed on {loadResult.Result.CompletedOn} by {loadResult.Result.CompletedBy}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {loadResult.Message}");

            return result;
        }

        //TODO: Need to show this on STAR CLI ASAP! ;-)
        public OASISResult<IQuest> GetCurentSubQuestForQuest(Guid avatarId, Guid questId, ProviderType providerType)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.GetCurentSubQuestForQuest. Reason:";

            OASISResult<Quest> loadResult = Load(avatarId, questId, providerType: providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.Quests != null && loadResult.Result.Quests.Count() > 0)
                    {
                        result.Result = loadResult.Result.Quests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

                        if (result.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} No sub-quest was found that is not completed!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No sub-quests were found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The quest was already completed on {loadResult.Result.CompletedOn} by {loadResult.Result.CompletedBy}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {loadResult.Message}");

            return result;
        }

        //public async Task<OASISResult<int>> GetCurentSubQuestNumberForQuestAsync(Guid questId)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.GetCurentSubQuestNumberForQuestAsync. Reason:";

        //    OASISResult<IQuest> GetCurentSubQuestForQuestAsync(questId);


        //    return result;
        //}

        public OASISResult<IQuest> HighlightCurentStageForQuestOnMap(Guid questId)
        {
            OASISResult<IQuest> questResult = new OASISResult<IQuest>();

            return questResult;
        }

        public OASISResult<IQuest> FindNearestQuestOnMap()
        {
            return new OASISResult<IQuest>();
        }

        //private async Task<OASISResult<IQuest>> CreateQuestInternalAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId = new Guid(), Guid parentQuestId = new Guid(), bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<Quest> createResult = await base.CreateAsync(avatarId, name, description, questType, fullPathToQuest, new Objects.STARNETCreateOptions<Quest, STARNETDNA>()
        //    {
        //        CheckIfSourcePathExists = checkIfSourcePathExists,
        //        STARNETHolon = new Quest
        //        {
        //            QuestType = questType,
        //            ParentMissionId = parentMissionId,
        //            ParentQuestId = parentQuestId
        //        }
        //    }, providerType);
           

        //    //OASISResult<Quest> createResult = await base.CreateAsync(avatarId, name, description, questType, fullPathToQuest, null, null, new Dictionary<string, object>()
        //    //{
        //    //    //We could also pass in metaData this way if we wanted but because we are setting them on the GeoHotSpot object below these will automatically be converted to MetaData on the holon anyway! ;-)
        //    //    //{ "ParentMissionId", parentMissionId.ToString() },
        //    //    //{ "ParentQuestId", parentQuestId.ToString() }
        //    //}, new Quest
        //    //{
        //    //    QuestType = questType,
        //    //    ParentMissionId = parentMissionId,
        //    //    ParentQuestId = parentQuestId
        //    //}, null, checkIfSourcePathExists,
        //    //providerType);

        //    OASISResult<IQuest> result = new OASISResult<IQuest>((IQuest)createResult.Result);
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createResult, result);
        //    return result;
        //}

        //private OASISResult<IQuest> CreateQuestInternal(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId = new Guid(), Guid parentQuestId = new Guid(), bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<Quest> createResult = base.Create(avatarId, name, description, questType, fullPathToQuest, new Dictionary<string, object>()
        //    {
        //        //We could also pass in metaData this way if we wanted but because we are setting them on the GeoHotSpot object below these will automatically be converted to MetaData on the holon anyway! ;-)
        //        //{ "ParentMissionId", parentMissionId.ToString() },
        //        //{ "ParentQuestId", parentQuestId.ToString() }
        //    }, new Quest
        //    {
        //        QuestType = questType,
        //        ParentMissionId = parentMissionId,
        //        ParentQuestId = parentQuestId
        //    }, null, checkIfSourcePathExists,
        //   providerType);

        //    OASISResult<IQuest> result = new OASISResult<IQuest>((IQuest)createResult.Result);
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createResult, result);
        //    return result;
        //}

        private OASISResult<IQuest> UpdateQuest(Guid avatarId, IQuest quest, OASISResult<IQuest> result, string errorMessage, bool updateDNAJSONFile = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Quest> questResult = Update(avatarId, (Quest)quest, updateDNAJSONFile = updateDNAJSONFile, providerType: providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with QuestManager.Update. Reason: {questResult.Message}");
            return result;
        }

        private async Task<OASISResult<IQuest>> UpdateQuestAsync(Guid avatarId, IQuest quest, OASISResult<IQuest> result, string errorMessage, bool updateDNAJSONFile = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Quest> questResult = await UpdateAsync(avatarId, (Quest)quest, updateDNAJSONFile = updateDNAJSONFile, providerType: providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(questResult, result);

            if (questResult != null && questResult.Result != null && !questResult.IsError)
                result.Result = questResult.Result;
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with QuestManager.Update. Reason: {questResult.Message}");

            return result;
        }

        /// <summary>
        /// Returns whether the avatar can start the quest (quest is NotStarted and any MetaData PrerequisiteQuestIds are completed). ParentQuestId is for sub-quests/objectives only, not prerequisites.
        /// </summary>
        public async Task<OASISResult<bool>> CanStartQuestAsync(Guid avatarId, Guid questId)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occurred in QuestManager.CanStartQuestAsync. Reason:";

            try
            {
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    result.Result = false;
                    result.Message = "Quest not found or could not be loaded.";
                    return result;
                }

                var quest = questResult.Result;
                if (quest.Status == QuestStatus.Completed)
                {
                    result.Result = false;
                    result.Message = "Quest is already completed.";
                    return result;
                }
                if (quest.Status == QuestStatus.InProgress)
                {
                    result.Result = false;
                    result.Message = "Quest is already in progress.";
                    return result;
                }

                var prereqIdList = (quest as Quest)?.PrerequisiteQuestIds;
                if (prereqIdList == null && quest.MetaData != null && quest.MetaData.ContainsKey("PrerequisiteQuestIds"))
                {
                    var prereqIds = quest.MetaData["PrerequisiteQuestIds"] as System.Collections.IEnumerable;
                    if (prereqIds != null)
                        prereqIdList = prereqIds.Cast<object>().Select(x => x?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                if (prereqIdList != null && prereqIdList.Count > 0)
                {
                    foreach (var item in prereqIdList)
                    {
                        if (!Guid.TryParse(item, out var prereqId) || prereqId == Guid.Empty) continue;
                        var prereqResult = await LoadAsync(avatarId, prereqId);
                        if (prereqResult.IsError || prereqResult.Result == null || prereqResult.Result.Status != QuestStatus.Completed)
                        {
                            result.Result = false;
                            result.Message = "Prerequisites not met. Complete all required quests first.";
                            return result;
                        }
                    }
                }

                result.Result = true;
                result.Message = "Quest can be started.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Starts a quest for the specified avatar. Validates prerequisites: if the quest has PrerequisiteQuestIds in MetaData, those quests must be completed first. (ParentQuestId is used for sub-quests/objectives; when all are complete the parent is marked complete.)
        /// </summary>
        public async Task<OASISResult<bool>> StartQuestAsync(Guid avatarId, Guid questId, string startNotes = null)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occurred in QuestManager.StartQuestAsync. Reason:";

            try
            {
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found or could not be loaded. Reason: {questResult.Message}");
                    return result;
                }

                var quest = questResult.Result;
                if (quest.Status == QuestStatus.Completed)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest is already completed.");
                    return result;
                }

                var prereqIdListStart = (quest as Quest)?.PrerequisiteQuestIds;
                if (prereqIdListStart == null && quest.MetaData != null && quest.MetaData.ContainsKey("PrerequisiteQuestIds"))
                {
                    var prereqIds = quest.MetaData["PrerequisiteQuestIds"] as System.Collections.IEnumerable;
                    if (prereqIds != null)
                        prereqIdListStart = prereqIds.Cast<object>().Select(x => x?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                if (prereqIdListStart != null && prereqIdListStart.Count > 0)
                {
                    foreach (var item in prereqIdListStart)
                    {
                        if (!Guid.TryParse(item, out var prereqId) || prereqId == Guid.Empty) continue;
                        var prereqResult = await LoadAsync(avatarId, prereqId);
                        if (prereqResult.IsError || prereqResult.Result == null || prereqResult.Result.Status != QuestStatus.Completed)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Prerequisites not met. Complete all required quests first.");
                            return result;
                        }
                    }
                }

                quest.Status = QuestStatus.InProgress;
                quest.StartedBy = avatarId;
                if (quest.StartedOn == DateTime.MinValue)
                    quest.StartedOn = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(startNotes))
                    quest.CompletionNotes = startNotes;

                if (quest.MetaData == null) quest.MetaData = new Dictionary<string, object>();
                quest.MetaData["Status"] = quest.Status.ToString();

                var updateResult = await UpdateAsync(avatarId, quest);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save started quest. Reason: {updateResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Quest started and saved (QuestId={questId}). If status does not update in the client, ensure the storage provider persists (e.g. MongoDB).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Completes a quest objective. Uses Quest.Objectives (Option B) first; falls back to child Quests for backward compatibility.
        /// </summary>
        public async Task<OASISResult<bool>> CompleteQuestObjectiveAsync(Guid avatarId, Guid questId, Guid objectiveId, string gameSource = null, string completionNotes = null)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occurred in QuestManager.CompleteQuestObjectiveAsync. Reason:";

            try
            {
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found or could not be loaded. Reason: {questResult.Message}");
                    return result;
                }

                var quest = questResult.Result;
                if (quest.Objectives == null)
                    quest.Objectives = new List<Objective>();
                quest.Status = quest.Status == QuestStatus.NotStarted ? QuestStatus.InProgress : quest.Status;
                if (quest.StartedOn == DateTime.MinValue)
                    quest.StartedOn = DateTime.UtcNow;
                quest.StartedBy = quest.StartedBy == Guid.Empty ? avatarId : quest.StartedBy;

                // Option B: complete objective in Quest.Objectives
                if (quest.Objectives.Count > 0)
                {
                    var objective = quest.Objectives.FirstOrDefault(x => x.Id == objectiveId);
                    if (objective != null)
                    {
                        objective.IsCompleted = true;
                        objective.CompletedAt = DateTime.UtcNow;
                        objective.CompletedBy = avatarId;

                        var updateResult = await UpdateAsync(avatarId, quest);
                        if (updateResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save objective completion. Reason: {updateResult.Message}");
                            return result;
                        }

                        var allComplete = quest.Objectives.All(x => x.IsCompleted);
                        if (allComplete)
                        {
                            quest.Status = QuestStatus.Completed;
                            quest.CompletedOn = DateTime.UtcNow;
                            quest.CompletedBy = avatarId;
                            if (!string.IsNullOrWhiteSpace(completionNotes))
                                quest.CompletionNotes = completionNotes;
                            await UpdateAsync(avatarId, quest);
                        }

                        result.Result = true;
                        result.Message = allComplete ? "Quest objective completed and quest is now complete." : "Quest objective completed successfully";
                        return result;
                    }
                }

                // Fallback: objectives stored as child Quest holons
                if (quest.Quests == null || quest.Quests.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest has no objectives to complete.");
                    return result;
                }

                var subQuestObjective = quest.Quests.FirstOrDefault(x => x.Id == objectiveId);
                if (subQuestObjective == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Objective {objectiveId} was not found for quest {questId}.");
                    return result;
                }

                subQuestObjective.Status = QuestStatus.Completed;
                subQuestObjective.CompletedOn = DateTime.UtcNow;
                subQuestObjective.CompletedBy = avatarId;
                subQuestObjective.StartedBy = subQuestObjective.StartedBy == Guid.Empty ? avatarId : subQuestObjective.StartedBy;
                if (subQuestObjective.StartedOn == DateTime.MinValue)
                    subQuestObjective.StartedOn = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(completionNotes))
                    subQuestObjective.CompletionNotes = completionNotes;

                if (!string.IsNullOrWhiteSpace(gameSource))
                    subQuestObjective.Requirements = subQuestObjective.Requirements?.Append($"CompletedFrom:{gameSource}").Distinct().ToList() ?? new List<string> { $"CompletedFrom:{gameSource}" };

                if (quest.Quests.All(x => x.Status == QuestStatus.Completed))
                {
                    quest.Status = QuestStatus.Completed;
                    quest.CompletedOn = DateTime.UtcNow;
                    quest.CompletedBy = avatarId;
                    if (!string.IsNullOrWhiteSpace(completionNotes))
                        quest.CompletionNotes = completionNotes;
                }
                else
                {
                    quest.Status = QuestStatus.InProgress;
                }

                if (quest.MetaData == null) quest.MetaData = new Dictionary<string, object>();
                quest.MetaData["Status"] = quest.Status.ToString();

                var updateResultLegacy = await UpdateAsync(avatarId, quest);
                if (updateResultLegacy.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save objective completion. Reason: {updateResultLegacy.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = quest.Status == QuestStatus.Completed
                    ? "Quest objective completed and quest is now complete."
                    : "Quest objective completed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

    }
}
