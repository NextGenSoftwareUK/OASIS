using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class QuestManager : OASISManager, IQuestManager
    {
        NFTManager _nftManager = null;

        private NFTManager NFTManager
        {
            get
            {
                if (_nftManager == null)
                    _nftManager = new NFTManager(AvatarId, OASISDNA);

                return _nftManager;
            }
        }

        public QuestManager(Guid avatarId, NFTManager nftManager = null, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, NFTManager nftManager = null, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public async Task<OASISResult<IQuest>> CreateQuestAsync(string name, string description, QuestType questType, Guid avatarId, Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            return await CreateQuestInternalAsync(name, description, questType, avatarId, missionId, default, providerType);
        }

        public OASISResult<IQuest> CreateQuest(string name, string description, QuestType questType, Guid avatarId, Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            return CreateQuestInternal(name, description, questType, avatarId, missionId, default, providerType);
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
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadHolonsForParentByMetaDataAsync<Quest>("ParentMissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

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
                OASISResult<IEnumerable<Quest>> loadHolonsResult = Data.LoadHolonsForParentByMetaData<Quest>("ParentMissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

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

        public async Task<OASISResult<IQuest>> CompleteQuestAsync(Guid questId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.CompleteQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> loadResult = await LoadQuestAsync(questId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    result = await UpdateQuestAsync(loadResult.Result, avatarId, providerType);

                    if (!(result != null && result.Result != null && !result.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with QuestManager.UpdateQuestAsync. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> CompleteQuest(Guid questId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.CompleteQuest. Reason:";

            try
            {
                OASISResult<IQuest> loadResult = LoadQuest(questId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    result = UpdateQuest(loadResult.Result, avatarId, providerType);

                    if (!(result != null && result.Result != null && !result.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with QuestManager.UpdateQuest. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> AddSubQuestToQuestAsync(Guid parentQuestId, string name, string description, QuestType questType, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddSubQuestToQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = await LoadQuestAsync(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<IQuest> subQuestResult = await CreateQuestInternalAsync(name, description, questType, avatarId, default, parentQuestId, providerType);

                    if (subQuestResult !=  null && subQuestResult.Result != null && !subQuestResult.IsError)
                    {
                        parentQuestResult.Result.SubQuests.Add(subQuestResult.Result);
                        result = await UpdateQuestAsync(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured creating the sub-quest with QuestManager.CreateQuestInternalAsync. Reason: {subQuestResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> AddSubQuestToQuest(Guid parentQuestId, string name, string description, QuestType questType, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddSubQuestToQuest. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = LoadQuest(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<IQuest> subQuestResult = CreateQuestInternal(name, description, questType, avatarId, default, parentQuestId, providerType);

                    if (subQuestResult != null && subQuestResult.Result != null && !subQuestResult.IsError)
                    {
                        parentQuestResult.Result.SubQuests.Add(subQuestResult.Result);
                        result = UpdateQuest(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured creating the sub-quest with QuestManager.CreateQuestInternal. Reason: {subQuestResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> RemoveSubQuestFromQuestAsync(Guid parentQuestId, Guid subQuestId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveSubQuestFromQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = await LoadQuestAsync(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IQuest subQuest = parentQuestResult.Result.SubQuests.FirstOrDefault(x => x.Id == subQuestId);

                    if (subQuest != null) 
                    {
                        parentQuestResult.Result.SubQuests.Remove(subQuest);
                        result = await UpdateQuestAsync(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No sub-quest could be found for the id {subQuestId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> RemoveSubQuestFromQuest(Guid parentQuestId, Guid subQuestId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveSubQuestFromQuest. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = LoadQuest(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IQuest subQuest = parentQuestResult.Result.SubQuests.FirstOrDefault(x => x.Id == subQuestId);

                    if (subQuest != null)
                    {
                        parentQuestResult.Result.SubQuests.Remove(subQuest);
                        result = UpdateQuest(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No sub-quest could be found for the id {subQuestId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = await LoadQuestAsync(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<IOASISGeoSpatialNFT> nftResult = await NFTManager.LoadGeoNftAsync(geoNFTId, providerType);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
                        parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());
                        result = await UpdateQuestAsync(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-nft with NFTManager.LoadGeoNftAsync. Reason: {nftResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> AddGeoNFTToQuest(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuest. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = LoadQuest(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    //OASISResult<OASISGeoSpatialNFT> nftResult = Data.LoadHolon<OASISGeoSpatialNFT>(geoNFTId); //TODO: May one day make NFTs work like Quests, HotSpots, etc so they extend Holon.
                    OASISResult<IOASISGeoSpatialNFT> nftResult = NFTManager.LoadGeoNft(geoNFTId, providerType);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
                        parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());
                        result = UpdateQuest(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-nft with NFTManager.LoadGeoNft. Reason: {nftResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = await LoadQuestAsync(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

                    if (geoNFT != null)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
                        parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
                        result = await UpdateQuestAsync(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoNFT could be found for the id {geoNFTId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> RemoveGeoNFTFromQuest(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuest. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = LoadQuest(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

                    if (geoNFT != null)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
                        parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
                        result = UpdateQuest(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoNFT could be found for the id {geoNFTId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> AddGeoHotSpotToQuestAsync(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = await LoadQuestAsync(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<GeoHotSpot> geoHotSpotResult = await Data.LoadHolonAsync<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
                    {
                        parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
                        parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
                        result = await UpdateQuestAsync(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolonAsync. Reason: {geoHotSpotResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> AddGeoHotSpotToQuest(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuest. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = LoadQuest(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<GeoHotSpot> geoHotSpotResult = Data.LoadHolon<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
                    {
                        parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
                        parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
                        result = UpdateQuest(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {geoHotSpotResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> RemoveGeoHotSpotFromQuestAsync(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuestAsync. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = await LoadQuestAsync(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

                    if (geoHotSpot != null)
                    {
                        parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
                        parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
                        result = await UpdateQuestAsync(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoHotSpot could be found for the id {geoHotSpotId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> RemoveGeoHotSpotFromQuest(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuest. Reason:";

            try
            {
                OASISResult<IQuest> parentQuestResult = LoadQuest(parentQuestId, providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

                    if (geoHotSpot != null)
                    {
                        parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
                        parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
                        result = UpdateQuest(parentQuestResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoHotSpot could be found for the id {geoHotSpotId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> DeleteQuestAsync(Guid questId, bool softDelete = true, bool deleteSubQuests = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.DeleteQuestAsync. Reason:";

            try
            {
                OASISResult<IHolon> deleteResult = await Data.DeleteHolonAsync(questId, softDelete, providerType);
                
                //TODO:Delete sub-quests, hotspots and nfts etc

                if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                    result.Result = (IQuest)deleteResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured deleting the quest with Data.DeleteHolonAsync. Reason: {deleteResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IQuest> DeleteQuest(Guid questId, bool softDelete = true, bool deleteSubQuests = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.DeleteQuest. Reason:";

            try
            {
                //TODO:Delete sub-quests, hotspots and nfts etc
                OASISResult<IHolon> deleteResult = Data.DeleteHolon(questId, softDelete, providerType);

                if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                    result.Result = (IQuest)deleteResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured deleting the quest with Data.DeleteHolon. Reason: {deleteResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> GetCurentSubQuestForQuestAsync(Guid questId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.GetCurentStageForQuestAsync. Reason:";

            OASISResult<IQuest> loadResult = await LoadQuestAsync(questId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.SubQuests != null && loadResult.Result.SubQuests.Count() > 0)
                    {
                        result.Result = loadResult.Result.SubQuests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

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

        public OASISResult<IQuest> GetCurentSubQuestForQuest(Guid questId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.GetCurentSubQuestForQuest. Reason:";

            OASISResult<IQuest> loadResult = LoadQuest(questId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.SubQuests != null && loadResult.Result.SubQuests.Count() > 0)
                    {
                        result.Result = loadResult.Result.SubQuests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

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

        public async Task<OASISResult<IQuest>> CreateQuestInternalAsync(string name, string description, QuestType questType, Guid avatarId, Guid parentMissionId = new Guid(), Guid parentQuestId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.CreateQuestInternalAsync. Reason:";

            try
            {
                IQuest quest = new Quest()
                {
                    ParentMissionId = parentMissionId,
                    ParentQuestId = parentQuestId,
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

        public OASISResult<IQuest> CreateQuestInternal(string name, string description, QuestType questType, Guid avatarId, Guid parentMissionId = new Guid(), Guid parentQuestId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.CreateQuestInternal. Reason:";

            try
            {
                IQuest quest = new Quest()
                {
                    //ParentHolonId = missionId, //If this is not set then this will normally default to the avatarId.
                    ParentMissionId = parentMissionId,
                    ParentQuestId = parentQuestId,
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
    }
}
