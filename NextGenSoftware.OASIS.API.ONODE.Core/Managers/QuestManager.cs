using System;
using System.Collections.Generic;
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
    public class QuestManager : OASISManager, IQuestManager
    {
        public QuestManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public async Task<OASISResult<IQuest>> CreateQuestAsync(string name, string description, QuestType questType, Guid avatarId, Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.CreateQuestAsync. Reason:";

            try
            {
                IQuest quest = new Quest()
                {
                    MissionId = missionId,
                    Name = name,
                    Description = description,
                    QuestType = questType,
                    StartedBy = avatarId,
                    StartedOn = DateTime.Now,
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.Now
                };

                OASISResult<Quest> saveHolonResult = await Data.SaveHolonAsync<Quest>(quest, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Data.SaveHolonAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex) 
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> CreateQuest(string name, string description, QuestType questType, Guid avatarId, Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.CreateQuest. Reason:";

            try
            {
                IQuest quest = new Quest()
                {
                    //ParentHolonId = missionId, //If this is not set then this will normally default to the avatarId.
                    MissionId = missionId,
                    Name = name,
                    Description = description,
                    QuestType = questType,
                    StartedBy = avatarId,
                    StartedOn = DateTime.Now,
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.Now
                };

                OASISResult<Quest> saveHolonResult = Data.SaveHolon<Quest>(quest, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Data.SaveHolon. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> UpdateQuestAsync(IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.UpdateQuestAsync. Reason:";

            try
            {
                //TODO: Double check this is done automatically (it is in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                quest.ModifiedByAvatarId = avatarId;
                quest.ModifiedDate = DateTime.Now;

                OASISResult<Quest> saveHolonResult = await Data.SaveHolonAsync<Quest>(quest, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Data.SaveHolonAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> UpdateQuest(IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.UpdateQuest. Reason:";

            try
            {
                //TODO: Double check this is done automatically (it is in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                quest.ModifiedByAvatarId = avatarId;
                quest.ModifiedDate = DateTime.Now;

                OASISResult<Quest> saveHolonResult = Data.SaveHolon<Quest>(quest, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Data.SaveHolon. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> LoadQuestAsync(Guid questId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.LoadQuestAsync. Reason:";

            try
            {
                OASISResult<Quest> loadHolonResult = await Data.LoadHolonAsync<Quest>(questId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                {
                    result.Result = loadHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonAsync. Reason: {loadHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> LoadQuest(Guid questId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.LoadQuestAsync. Reason:";

            try
            {
                OASISResult<Quest> loadHolonResult = Data.LoadHolon<Quest>(questId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                {
                    result.Result = loadHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolon. Reason: {loadHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadAllHolonsAsync<Quest>(HolonType.Quest, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadAllHolonsAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IQuest>> LoadAllQuests(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = Data.LoadAllHolons<Quest>(HolonType.Quest, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadAllQuests. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadHolonsForParentAsync<Quest>(avatarId, HolonType.Quest, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonsForParentAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IQuest>> LoadAllQuestsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatar. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = Data.LoadHolonsForParent<Quest>(avatarId, HolonType.Quest, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadAllQuestsForAvatar. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadHolonsForParentByMetaDataAsync<Quest>("MissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonsForParentByMetaDataAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IQuest>> LoadAllQuestsForMission(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = Data.LoadHolonsForParentByMetaData<Quest>("MissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonsForParentByMetaDataAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForOAPPAsync(Guid OAPPId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadHolonsForParentAsync<Quest>(avatarId, HolonType.Quest, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with Data.LoadHolonsForParentAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> CompleteQuestAsync(Guid questId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.UpdateQuestAsync. Reason:";

            try
            {
                //TODO: Double check this is done automatically (it is in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                quest.ModifiedByAvatarId = avatarId;
                quest.ModifiedDate = DateTime.Now;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(quest, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = (IQuest)saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Data.SaveHolonAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> DeleteQuest(Guid questId)
        {
            OASISResult<IQuest> questResult = new OASISResult<IQuest>();

            return questResult;
        }

        public OASISResult<IQuest> HighlightQuestOnMap(Guid questId)
        {
            OASISResult<IQuest> questResult = new OASISResult<IQuest>();

            return questResult;
        }

        public OASISResult<IQuest> FindNearestQuestOnMap()
        {
            return new OASISResult<IQuest>();
        }

        public OASISResult<IEnumerable<IQuest>> GetAllCurrentQuestsForAvatar(Guid avatarId)
        {
            return new OASISResult<IEnumerable<IQuest>>();
        }
    }
}
