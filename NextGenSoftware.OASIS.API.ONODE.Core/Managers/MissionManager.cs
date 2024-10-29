using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    //TODO: Make a QuestBaseManager and TaskBaseManager to extend from so lots of the same generic code is re-used.
    public class MissionManager : PublishManagerBase//, IMissionManager
    {
        public MissionManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }

        #region COSMICManagerBase
        public async Task<OASISResult<IMission>> SaveMissionAsync(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = await SaveHolonAsync<Mission>(mission, avatarId, providerType, "MissionManager.SaveMissionAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IMission> SaveMission(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = SaveHolon<Mission>(mission, avatarId, providerType, "MissionManager.SaveMission");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IMission>> LoadMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> loadResult = await LoadHolonAsync<Mission>(missionId, providerType, "MissionManager.LoadMissionAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public OASISResult<IMission> LoadMission(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> loadResult = LoadHolon<Mission>(missionId, providerType, "MissionManager.LoadMission");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IMission>>> LoadAllMissionsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            OASISResult<IEnumerable<Mission>> loadHolonsResult = await LoadAllHolonsAsync<Mission>(providerType, "MissionManager.LoadAllMissionsAsync", HolonType.Mission);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IMission>> LoadAllMissions(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            OASISResult<IEnumerable<Mission>> loadHolonsResult = LoadAllHolons<Mission>(providerType, "MissionManager.LoadAllMissions", HolonType.Mission);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IMission>>> LoadAllMissionsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            OASISResult<IEnumerable<Mission>> loadHolonsResult = await LoadAllHolonsForAvatarAsync<Mission>(avatarId, providerType, "MissionManager.LoadAllMissionsForAvatarAsync", HolonType.Mission);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IMission>> LoadAllMissionsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            OASISResult<IEnumerable<Mission>> loadHolonsResult = LoadAllHolonsForAvatar<Mission>(avatarId, providerType, "MissionManager.LoadAllMissionsForAvatarAsync", HolonType.Mission);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IMission>> DeleteMissionAsync(Guid inventoryId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> loadHolonsResult = await DeleteHolonAsync<Mission>(inventoryId, softDelete, providerType, "MissionManager.DeleteMissionAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IMission> DeleteMission(Guid inventoryId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> loadHolonsResult = DeleteHolon<Mission>(inventoryId, softDelete, providerType, "MissionManager.DeleteMission");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }
        #endregion

        #region PublishManagerBase
        public async Task<OASISResult<IMission>> PublishMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = await PublishHolonAsync<Mission>(missionId, avatarId, "MissionManager.PublishMissionAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IMission> PublishMission(Guid missionId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = PublishHolon<Mission>(missionId, avatarId, "MissionManager.PublishMission", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IMission>> PublishMissionAsync(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = await PublishHolonAsync<Mission>(mission, avatarId, "MissionManager.PublishMissionAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IMission> PublishMission(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = PublishHolon<Mission>(mission, avatarId, "MissionManager.PublishMission", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IMission>> UnpublishMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = await UnpublishHolonAsync<Mission>(missionId, avatarId, "MissionManager.UnpublishMissionAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IMission> UnpublishMission(Guid missionId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = UnpublishHolon<Mission>(missionId, avatarId, "MissionManager.UnpublishMission", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IMission>> UnpublishMissionAsync(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = await UnpublishHolonAsync<Mission>(mission, avatarId, "MissionManager.UnpublishMissionAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IMission> UnpublishMission(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            OASISResult<Mission> saveResult = UnpublishHolon<Mission>(mission, avatarId, "MissionManager.UnpublishMission", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }
        #endregion

        //public async Task<OASISResult<IMission>> PublishMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType)
        //{
        //    OASISResult<IMission> result = new OASISResult<IMission>();
        //    string errorMessage = "Error occured in MissionManager.PublishMissionAsync. Reason:";

        //    try
        //    {
        //        OASISResult<IMission> loadResult = await LoadMissionAsync(missionId, providerType);

        //        if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
        //        {
        //            loadResult.Result.PublishedOn = DateTime.Now;
        //            loadResult.Result.PublishedByAvatarId = avatarId;

        //            result = await SaveMissionAsync(loadResult.Result, avatarId, providerType);

        //            if (!(result != null && result.Result != null && !result.IsError))
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with MissionManager.UpdateMissionAsync. Reason: {result.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IMission> PublishMission(Guid missionId, Guid avatarId, ProviderType providerType)
        //{
        //    OASISResult<IMission> result = new OASISResult<IMission>();
        //    string errorMessage = "Error occured in MissionManager.PublishMission. Reason:";

        //    try
        //    {
        //        OASISResult<IMission> loadResult = LoadMission(missionId, providerType);

        //        if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
        //        {
        //            loadResult.Result.PublishedOn = DateTime.Now;
        //            loadResult.Result.PublishedByAvatarId = avatarId;

        //            result = SaveMission(loadResult.Result, avatarId, providerType);

        //            if (!(result != null && result.Result != null && !result.IsError))
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with MissionManager.UpdateMission. Reason: {result.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IMission>> UnpublishMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType)
        //{
        //    OASISResult<IMission> result = new OASISResult<IMission>();
        //    string errorMessage = "Error occured in MissionManager.UnpublishMissionAsync. Reason:";

        //    try
        //    {
        //        OASISResult<IMission> loadResult = await LoadMissionAsync(missionId, providerType);

        //        if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
        //        {
        //            loadResult.Result.PublishedOn = DateTime.MinValue;
        //            loadResult.Result.PublishedByAvatarId = Guid.Empty;

        //            result = await SaveMissionAsync(loadResult.Result, avatarId, providerType);

        //            if (!(result != null && result.Result != null && !result.IsError))
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with MissionManager.UpdateMissionAsync. Reason: {result.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IMission> UnpublishMission(Guid missionId, Guid avatarId, ProviderType providerType)
        //{
        //    OASISResult<IMission> result = new OASISResult<IMission>();
        //    string errorMessage = "Error occured in MissionManager.UnpublishMission. Reason:";

        //    try
        //    {
        //        OASISResult<IMission> loadResult = LoadMission(missionId, providerType);

        //        if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
        //        {
        //            loadResult.Result.PublishedOn = DateTime.MinValue;
        //            loadResult.Result.PublishedByAvatarId = Guid.Empty;

        //            result = SaveMission(loadResult.Result, avatarId, providerType);

        //            if (!(result != null && result.Result != null && !result.IsError))
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with MissionManager.UpdateMission. Reason: {result.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IMission>> CompleteMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.CompleteMissionAsync. Reason:";

            try
            {
                OASISResult<IMission> loadResult = await LoadMissionAsync(missionId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    result = await SaveMissionAsync(loadResult.Result, avatarId, providerType);

                    if (!(result != null && result.Result != null && !result.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with MissionManager.UpdateMissionAsync. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IMission> CompleteMission(Guid missionId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.CompleteMission. Reason:";

            try
            {
                OASISResult<IMission> loadResult = LoadMission(missionId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    result = SaveMission(loadResult.Result, avatarId, providerType);

                    if (!(result != null && result.Result != null && !result.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with MissionManager.UpdateMission. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        //TODO: Dont think we need this? When we can just use the other overload that takes a IQuest?
        //public async Task<OASISResult<IMission>> AddQuestToMissionAsync(Guid parentMissionId, Guid questId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IMission> result = new OASISResult<IMission>();
        //    string errorMessage = "Error occured in MissionManager.AddQuestToMissionAsync. Reason:";

        //    try
        //    {
        //        OASISResult<IMission> parentMissionResult = await LoadMissionAsync(parentMissionId, providerType);

        //        if (parentMissionResult != null && parentMissionResult.Result != null && !parentMissionResult.IsError)
        //        {
        //            OASISResult<IQuest> questResult = QuestManager.LoadQuestAsync(questId); 

        //            if (questResult != null && questResult.Result != null && !questResult.IsError)
        //            {
        //                parentMissionResult.Result.Quests.Add(questResult.Result);
        //                result = await UpdateMissionAsync(parentMissionResult.Result, avatarId, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {questResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMissionAsync. Reason: {parentMissionResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IMission>> AddQuestToMissionAsync(Guid parentMissionId, IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.AddQuestToMissionAsync. Reason:";

            try
            {
                OASISResult<IMission> parentMissionResult = await LoadMissionAsync(parentMissionId, providerType);

                if (parentMissionResult != null && parentMissionResult.Result != null && !parentMissionResult.IsError)
                {
                    parentMissionResult.Result.Quests.Add(quest);
                    result = await SaveMissionAsync(parentMissionResult.Result, avatarId, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMissionAsync. Reason: {parentMissionResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IMission>> AddQuestToMission(Guid parentMissionId, IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.AddQuestToMission. Reason:";

            try
            {
                OASISResult<IMission> parentMissionResult = LoadMission(parentMissionId, providerType);

                if (parentMissionResult != null && parentMissionResult.Result != null && !parentMissionResult.IsError)
                {
                    parentMissionResult.Result.Quests.Add(quest);
                    result = SaveMission(parentMissionResult.Result, avatarId, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMission. Reason: {parentMissionResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }



        public async Task<OASISResult<IMission>> RemoveQuestFromMissionAsync(Guid parentMissionId, Guid questId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.RemoveQuestFromMissionAsync. Reason:";

            try
            {
                OASISResult<IMission> parentMissionResult = await LoadMissionAsync(questId, providerType);

                if (parentMissionResult != null && parentMissionResult.Result != null && !parentMissionResult.IsError)
                {
                    IQuest quest = parentMissionResult.Result.Quests.FirstOrDefault(x => x.Id == questId);

                    if (quest != null)
                    {
                        parentMissionResult.Result.Quests.Remove(quest);
                        result = await SaveMissionAsync(parentMissionResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest could be found for the id {questId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMissionAsync. Reason: {parentMissionResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IMission> RemoveQuestFromMission(Guid parentMissionId, Guid questId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.RemoveQuestFromMission. Reason:";

            try
            {
                OASISResult<IMission> parentMissionResult = LoadMission(questId, providerType);

                if (parentMissionResult != null && parentMissionResult.Result != null && !parentMissionResult.IsError)
                {
                    IQuest quest = parentMissionResult.Result.Quests.FirstOrDefault(x => x.Id == questId);

                    if (quest != null)
                    {
                        parentMissionResult.Result.Quests.Remove(quest);
                        result = SaveMission(parentMissionResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest could be found for the id {questId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMission. Reason: {parentMissionResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> GetCurentQuestForMissionAsync(Guid missionId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in MissionManager.GetCurentQuestForMissionAsync. Reason:";

            OASISResult<IMission> loadResult = await LoadMissionAsync(missionId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.Quests != null && loadResult.Result.Quests.Count() > 0)
                    {
                        result.Result = loadResult.Result.Quests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

                        if (result.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest was found that is not completed!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quests were found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The mission was already completed on {loadResult.Result.CompletedOn} by {loadResult.Result.CompletedBy}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMissionAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IQuest> GetCurentQuestForMission(Guid missionId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in MissionManager.GetCurentQuestForMission. Reason:";

            OASISResult<IMission> loadResult = LoadMission(missionId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.Quests != null && loadResult.Result.Quests.Count() > 0)
                    {
                        result.Result = loadResult.Result.Quests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

                        if (result.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest was found that is not completed!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quests were found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The mission was already completed on {loadResult.Result.CompletedOn} by {loadResult.Result.CompletedBy}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with MissionManager.LoadMission. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IList<IMission>>> GetAllCurrentMissionsForAvatarAsync(Guid avatarId)
        {
            OASISResult<IList<IMission>> result = new OASISResult<IList<IMission>>();

            return result;
        }
    }
}
