using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.STAR.DNA;
using EOSNewYork.EOSCore.Response.API;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class QuestManager : QuestManagerBase<Quest, DownloadedQuest, InstalledQuest, QuestDNA>, IQuestManager
    public class QuestManager : QuestManagerBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>, IQuestManager
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

        public QuestManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(QuestType),
            HolonType.Quest,
            HolonType.InstalledQuest,
            "Quest",
            //"QuestId",
            "STARNETHolonId",
            "QuestName",
            "QuestType",
            "oquest",
            "oasis_quests",
            "QuestDNA.json",
            "QuestDNAJSON")
        { }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(QuestType),
            HolonType.Quest,
            HolonType.InstalledQuest,
            "Quest",
            //"QuestId",
            "STARNETHolonId",
            "QuestName",
            "QuestType",
            "oquest",
            "oasis_quests",
            "QuestDNA.json",
            "QuestDNAJSON")
        { }

        public async Task<OASISResult<IQuest>> CreateQuestForMissionAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            return await CreateQuestInternalAsync(avatarId, name, description, questType, fullPathToQuest, parentMissionId, default, checkIfSourcePathExists, providerType);
        }

        public OASISResult<IQuest> CreateQuestForMission(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            return CreateQuestInternal(avatarId, name, description, questType, fullPathToQuest, parentMissionId, default, checkIfSourcePathExists, providerType);
        }

        public async Task<OASISResult<IQuest>> CreateSubQuestForQuestAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            return await CreateQuestInternalAsync(avatarId, name, description, questType, fullPathToQuest, default, parentQuestId, checkIfSourcePathExists, providerType);
        }

        public OASISResult<IQuest> CreateSubQuestForQuest(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            return CreateQuestInternal(avatarId, name, description, questType, fullPathToQuest, default, parentQuestId, checkIfSourcePathExists, providerType);
        }

        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IQuest>> result = new OASISResult<IEnumerable<IQuest>>();
            string errorMessage = "Error occured in QuestManager.LoadAllQuestsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Quest>> loadHolonsResult = await Data.LoadHolonsByMetaDataAsync<Quest>("ParentMissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

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
                OASISResult<IEnumerable<Quest>> loadHolonsResult = Data.LoadHolonsByMetaData<Quest>("ParentMissionId", missionId.ToString(), HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

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

        public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuestAsync. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<IOASISGeoSpatialNFT> nftResult = await NFTManager.LoadGeoNftAsync(geoNFTId, providerType);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
                        parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());

                        //if (parentQuestResult.Result.STARNETDNA.MetaData["GeoNFTs"] != null)
                        //{
                        //    Dictionary<string, List<STARNETDependency> geoNFTs = parentQuestResult.Result.STARNETDNA.MetaData["GeoNFTs"] as Dictionary<string, string>;

                        //    if (geoNFTs != null)
                        //    {
                        //        geoNFTs[]
                        //    }
                        //}

                        result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public OASISResult<IQuest> AddGeoNFTToQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuest. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<IOASISGeoSpatialNFT> nftResult = NFTManager.LoadGeoNft(geoNFTId, providerType);

                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
                        parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());
                        result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public async Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuestAsync. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

                    if (geoNFT != null)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
                        parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
                        result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public OASISResult<IQuest> RemoveGeoNFTFromQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuest. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

                    if (geoNFT != null)
                    {
                        parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
                        parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
                        result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public async Task<OASISResult<IQuest>> AddGeoHotSpotToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuestAsync. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<GeoHotSpot> geoHotSpotResult = await Data.LoadHolonAsync<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
                    {
                        parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
                        parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
                        result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public OASISResult<IQuest> AddGeoHotSpotToQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuest. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    OASISResult<GeoHotSpot> geoHotSpotResult = Data.LoadHolon<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

                    if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
                    {
                        parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
                        parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
                        result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public async Task<OASISResult<IQuest>> RemoveGeoHotSpotFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuestAsync. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

                    if (geoHotSpot != null)
                    {
                        parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
                        parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
                        result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        public OASISResult<IQuest> RemoveGeoHotSpotFromQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuest. Reason:";

            try
            {
                OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

                if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
                {
                    IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

                    if (geoHotSpot != null)
                    {
                        parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
                        parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
                        result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
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

        private async Task<OASISResult<IQuest>> CreateQuestInternalAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId = new Guid(), Guid parentQuestId = new Guid(), bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Quest> createResult = await base.CreateAsync(avatarId, name, description, questType, fullPathToQuest, null, new Dictionary<string, object>()
            {
                //We could also pass in metaData this way if we wanted but because we are setting them on the GeoHotSpot object below these will automatically be converted to MetaData on the holon anyway! ;-)
                //{ "ParentMissionId", parentMissionId.ToString() },
                //{ "ParentQuestId", parentQuestId.ToString() }
            }, new Quest
            {
                QuestType = questType,
                ParentMissionId = parentMissionId,
                ParentQuestId = parentQuestId
            }, null, checkIfSourcePathExists,
            providerType);

            OASISResult<IQuest> result = new OASISResult<IQuest>((IQuest)createResult.Result);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createResult, result);
            return result;
        }

        private OASISResult<IQuest> CreateQuestInternal(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId = new Guid(), Guid parentQuestId = new Guid(), bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Quest> createResult = base.Create(avatarId, name, description, questType, fullPathToQuest, new Dictionary<string, object>()
            {
                //We could also pass in metaData this way if we wanted but because we are setting them on the GeoHotSpot object below these will automatically be converted to MetaData on the holon anyway! ;-)
                //{ "ParentMissionId", parentMissionId.ToString() },
                //{ "ParentQuestId", parentQuestId.ToString() }
            }, new Quest
            {
                QuestType = questType,
                ParentMissionId = parentMissionId,
                ParentQuestId = parentQuestId
            }, null, checkIfSourcePathExists,
           providerType);

            OASISResult<IQuest> result = new OASISResult<IQuest>((IQuest)createResult.Result);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createResult, result);
            return result;
        }

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
    }
}