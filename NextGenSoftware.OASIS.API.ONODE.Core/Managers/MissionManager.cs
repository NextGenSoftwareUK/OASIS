using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    //TODO: Make a QuestBaseManager and TaskBaseManager to extend from so lots of the same generic code is re-used.
    public class MissionManager : OASISManager, IMissionManager
    {
        //QuestManager _questManager = null;
        //NFTManager _nftManager = null;

        ////TODO: May remove NFTManager and QuestManager DI (only used for adding quests to a mission with the questId (they can use the other overload that takes the IQuest)
        //private NFTManager NFTManager
        //{
        //    get
        //    {
        //        if (_nftManager == null)
        //            _nftManager = new NFTManager(AvatarId, OASISDNA);

        //        return _nftManager;
        //    }
        //}

        //private QuestManager QuestManager
        //{
        //    get
        //    {
        //        if (_questManager == null)
        //            _questManager = new QuestManager(AvatarId, NFTManager, OASISDNA);

        //        return _questManager;
        //    }
        //}

        //public MissionManager(Guid avatarId, QuestManager questManager, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        //{
        //    _questManager = questManager;
        //}

        //public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, QuestManager questManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        //{
        //    _questManager = questManager;
        //}

        public MissionManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            //_questManager = questManager;
        }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            //_questManager = questManager;
        }

        public async Task<OASISResult<IMission>> CreateMissionAsync(string name, string description, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.CreateMissionAsync. Reason:";

            try
            {
                IMission mission = new Mission()
                {
                    Name = name,
                    Description = description,
                    StartedBy = avatarId,
                    StartedOn = DateTime.Now,
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.Now
                };

                OASISResult<Mission> saveHolonResult = await Data.SaveHolonAsync<Mission>(mission, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with Data.SaveHolonAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IMission> CreateMission(string name, string description, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.CreateMission. Reason:";

            try
            {
                IMission mission = new Mission()
                {
                    Name = name,
                    Description = description,
                    StartedBy = avatarId,
                    StartedOn = DateTime.Now,
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.Now
                };

                OASISResult<Mission> saveHolonResult = Data.SaveHolon<Mission>(mission, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with Data.SaveHolon. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IMission>> UpdateMissionAsync(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.UpdateMissionAsync. Reason:";

            try
            {
                OASISResult<Mission> saveHolonResult = await mission.SaveAsync<Mission>();

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with mission.SaveAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IMission> UpdateMission(IMission mission, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.UpdateMission. Reason:";

            try
            {
                OASISResult<Mission> saveHolonResult = mission.Save<Mission>();

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the mission with mission.Save. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IMission>> LoadMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.LoadMissionAsync. Reason:";

            try
            {
                OASISResult<Mission> loadHolonResult = await Data.LoadHolonAsync<Mission>(missionId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                {
                    result.Result = loadHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with Data.LoadHolonAsync. Reason: {loadHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IMission> LoadMission(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.LoadMissionAsync. Reason:";

            try
            {
                OASISResult<Mission> loadHolonResult = Data.LoadHolon<Mission>(missionId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                {
                    result.Result = loadHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with Data.LoadHolon. Reason: {loadHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IMission>>> LoadAllMissionsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            string errorMessage = "Error occured in MissionManager.LoadAllMissionsAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Mission>> loadHolonsResult = await Data.LoadAllHolonsAsync<Mission>(HolonType.Mission, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with Data.LoadAllHolonsAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IMission>> LoadAllMissions(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            string errorMessage = "Error occured in MissionManager.LoadAllMissionsAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Mission>> loadHolonsResult = Data.LoadAllHolons<Mission>(HolonType.Mission, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with Data.LoadAllMissions. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IMission>>> LoadAllMissionsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            string errorMessage = "Error occured in MissionManager.LoadAllMissionsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Mission>> loadHolonsResult = await Data.LoadHolonsForParentAsync<Mission>(avatarId, HolonType.Mission, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with Data.LoadHolonsForParentAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IMission>> LoadAllMissionsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMission>> result = new OASISResult<IEnumerable<IMission>>();
            string errorMessage = "Error occured in MissionManager.LoadAllMissionsForAvatar. Reason:";

            try
            {
                OASISResult<IEnumerable<Mission>> loadHolonsResult = Data.LoadHolonsForParent<Mission>(avatarId, HolonType.Mission, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with Data.LoadAllMissionsForAvatar. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IMission>> PublishMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.PublishMissionAsync. Reason:";

            try
            {
                OASISResult<IMission> loadResult = await LoadMissionAsync(missionId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.PublishedOn = DateTime.Now;
                    loadResult.Result.PublishedByAvatarId = avatarId;

                    result = await UpdateMissionAsync(loadResult.Result, avatarId, providerType);

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

        public OASISResult<IMission> PublishMission(Guid missionId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.PublishMission. Reason:";

            try
            {
                OASISResult<IMission> loadResult = LoadMission(missionId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.PublishedOn = DateTime.Now;
                    loadResult.Result.PublishedByAvatarId = avatarId;

                    result = UpdateMission(loadResult.Result, avatarId, providerType);

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

        public async Task<OASISResult<IMission>> UnpublishMissionAsync(Guid missionId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.UnpublishMissionAsync. Reason:";

            try
            {
                OASISResult<IMission> loadResult = await LoadMissionAsync(missionId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.PublishedOn = DateTime.MinValue;
                    loadResult.Result.PublishedByAvatarId = Guid.Empty;

                    result = await UpdateMissionAsync(loadResult.Result, avatarId, providerType);

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

        public OASISResult<IMission> UnpublishMission(Guid missionId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.UnpublishMission. Reason:";

            try
            {
                OASISResult<IMission> loadResult = LoadMission(missionId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.PublishedOn = DateTime.MinValue;
                    loadResult.Result.PublishedByAvatarId = Guid.Empty;

                    result = UpdateMission(loadResult.Result, avatarId, providerType);

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

                    result = await UpdateMissionAsync(loadResult.Result, avatarId, providerType);

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

                    result = UpdateMission(loadResult.Result, avatarId, providerType);

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
                    result = await UpdateMissionAsync(parentMissionResult.Result, avatarId, providerType);
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
                    result = UpdateMission(parentMissionResult.Result, avatarId, providerType);
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
                        result = await UpdateMissionAsync(parentMissionResult.Result, avatarId, providerType);
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
                        result = UpdateMission(parentMissionResult.Result, avatarId, providerType);
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

        public async Task<OASISResult<IMission>> DeleteMissionAsync(Guid missionId, bool softDelete = true, bool deleteSubMissions = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.DeleteMissionAsync. Reason:";

            try
            {
                OASISResult<IHolon> deleteResult = await Data.DeleteHolonAsync(missionId, softDelete, providerType);

                //TODO:Delete sub-missions, hotspots and nfts etc

                if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                    result.Result = (IMission)deleteResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured deleting the mission with Data.DeleteHolonAsync. Reason: {deleteResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IMission> DeleteMission(Guid missionId, bool softDelete = true, bool deleteSubMissions = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IMission> result = new OASISResult<IMission>();
            string errorMessage = "Error occured in MissionManager.DeleteMission. Reason:";

            try
            {
                //TODO:Delete sub-missions, hotspots and nfts etc
                OASISResult<IHolon> deleteResult = Data.DeleteHolon(missionId, softDelete, providerType);

                if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                    result.Result = (IMission)deleteResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured deleting the mission with Data.DeleteHolon. Reason: {deleteResult.Message}");
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
