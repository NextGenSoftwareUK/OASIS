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
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class QuestManager : QuestManagerBase<Quest, DownloadedQuest, InstalledQuest, QuestDNA>, IQuestManager
    public partial class QuestManager : QuestManagerBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>, IQuestManager
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

        //public override async Task<OASISResult<Quest>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, ISTARNETCreateOptions<Quest, STARNETDNA> createOptions = null, ProviderType providerType = ProviderType.Default)
        //{


        //    OASISResult<Quest> createResult = await base.CreateAsync(avatarId, name, description, holonSubType, fullPathToSourceFolder, createOptions, providerType);
        //    //{
        //        //CheckIfSourcePathExists = checkIfSourcePathExists,
        //        //STARNETHolon = new Quest
        //        //{
        //        //    QuestType = questType,
        //        //    ParentMissionId = parentMissionId,
        //        //    ParentQuestId = parentQuestId
        //        //}
        //    //}, providerType);



        //    //OASISResult<IQuest> result = new OASISResult<IQuest>((IQuest)createResult.Result);
        //    //OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createResult, result);
        //    //return result;


        //    //return base.CreateAsync(avatarId, name, description, holonSubType, fullPathToSourceFolder, createOptions, providerType);
        //}

        //public async Task<OASISResult<IQuest>> CreateQuestForMissionAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return await CreateQuestInternalAsync(avatarId, name, description, questType, fullPathToQuest, parentMissionId, default, checkIfSourcePathExists, providerType);
        //}

        //public OASISResult<IQuest> CreateQuestForMission(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return CreateQuestInternal(avatarId, name, description, questType, fullPathToQuest, parentMissionId, default, checkIfSourcePathExists, providerType);
        //}

        //public async Task<OASISResult<IQuest>> CreateSubQuestForQuestAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return await CreateQuestInternalAsync(avatarId, name, description, questType, fullPathToQuest, default, parentQuestId, checkIfSourcePathExists, providerType);
        //}

        //public OASISResult<IQuest> CreateSubQuestForQuest(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return CreateQuestInternal(avatarId, name, description, questType, fullPathToQuest, default, parentQuestId, checkIfSourcePathExists, providerType);
        //}

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

        /// <summary>Load all quests for avatar using IQuest path; promotes MetaData to strongly-typed properties (e.g. Status from MetaData["QuestStatus"]).</summary>
        public async Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IQuest>>();
            var baseResult = await LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType).ConfigureAwait(false);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(baseResult, result);
            if (baseResult.IsError || baseResult.Result == null)
                return result;
            var list = baseResult.Result.ToList();
            foreach (var q in list)
                PromoteQuestMetaDataToProperties(q);
            result.Result = list;
            return result;
        }

        /// <summary>Load all quests for avatar using IQuest path; promotes MetaData to strongly-typed properties.</summary>
        public OASISResult<IEnumerable<IQuest>> LoadAllQuestsForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IQuest>>();
            var baseResult = LoadAllForAvatar(avatarId, showAllVersions, version, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(baseResult, result);
            if (baseResult.IsError || baseResult.Result == null)
                return result;
            var list = baseResult.Result.ToList();
            foreach (var q in list)
                PromoteQuestMetaDataToProperties(q);
            result.Result = list;
            return result;
        }

        /// <summary>Promote MetaData to strongly-typed Quest properties. Prefer "Status" (used by HolonManager.MapMetaData); fallback to "QuestStatus" for backwards compatibility.</summary>
        private static void PromoteQuestMetaDataToProperties(Quest q)
        {
            if (q?.MetaData == null) return;
            var key = q.MetaData.ContainsKey("Status") ? "Status" : (q.MetaData.ContainsKey("QuestStatus") ? "QuestStatus" : null);
            if (key == null) return;
            var val = q.MetaData[key];
            if (val == null) return;
            var s = val.ToString();
            if (string.IsNullOrEmpty(s)) return;
            if (System.Enum.TryParse<QuestStatus>(s, true, out var status))
                q.Status = status;
        }

        //public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<IOASISGeoSpatialNFT> nftResult = await NFTManager.LoadGeoNftAsync(geoNFTId, providerType);

        //            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());

        //                //if (parentQuestResult.Result.STARNETDNA.MetaData["GeoNFTs"] != null)
        //                //{
        //                //    Dictionary<string, List<STARNETDependency> geoNFTs = parentQuestResult.Result.STARNETDNA.MetaData["GeoNFTs"] as Dictionary<string, string>;

        //                //    if (geoNFTs != null)
        //                //    {
        //                //        geoNFTs[]
        //                //    }
        //                //}

        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-nft with NFTManager.LoadGeoNftAsync. Reason: {nftResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> AddGeoNFTToQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoNFTToQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<IOASISGeoSpatialNFT> nftResult = NFTManager.LoadGeoNft(geoNFTId, providerType);

        //            if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Add(nftResult.Result);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Add(nftResult.Result.Id.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-nft with NFTManager.LoadGeoNft. Reason: {nftResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

        //            if (geoNFT != null)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoNFT could be found for the id {geoNFTId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> RemoveGeoNFTFromQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoNFTFromQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IOASISGeoSpatialNFT geoNFT = parentQuestResult.Result.GeoSpatialNFTs.FirstOrDefault(x => x.Id == geoNFTId);

        //            if (geoNFT != null)
        //            {
        //                parentQuestResult.Result.GeoSpatialNFTs.Remove(geoNFT);
        //                parentQuestResult.Result.GeoSpatialNFTIds.Remove(geoNFTId.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoNFT could be found for the id {geoNFTId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> AddGeoHotSpotToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<GeoHotSpot> geoHotSpotResult = await Data.LoadHolonAsync<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
        //                parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolonAsync. Reason: {geoHotSpotResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> AddGeoHotSpotToQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.AddGeoHotSpotToQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            OASISResult<GeoHotSpot> geoHotSpotResult = Data.LoadHolon<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);

        //            if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Add(geoHotSpotResult.Result);
        //                parentQuestResult.Result.GeoHotSpotIds.Add(geoHotSpotResult.Result.Id.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the geo-hotspot with Data.LoadHolon. Reason: {geoHotSpotResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> RemoveGeoHotSpotFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuestAsync. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = await LoadAsync(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

        //            if (geoHotSpot != null)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
        //                parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
        //                result = await UpdateQuestAsync(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoHotSpot could be found for the id {geoHotSpotId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        //public OASISResult<IQuest> RemoveGeoHotSpotFromQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    string errorMessage = "Error occured in QuestManager.RemoveGeoHotSpotFromQuest. Reason:";

        //    try
        //    {
        //        OASISResult<Quest> parentQuestResult = Load(avatarId, parentQuestId, providerType: providerType);

        //        if (parentQuestResult != null && parentQuestResult.Result != null && !parentQuestResult.IsError)
        //        {
        //            IGeoHotSpot geoHotSpot = parentQuestResult.Result.GeoHotSpots.FirstOrDefault(x => x.Id == geoHotSpotId);

        //            if (geoHotSpot != null)
        //            {
        //                parentQuestResult.Result.GeoHotSpots.Remove(geoHotSpot);
        //                parentQuestResult.Result.GeoHotSpotIds.Remove(geoHotSpot.ToString());
        //                result = UpdateQuest(avatarId, parentQuestResult.Result, result, errorMessage, providerType: providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} No GeoHotSpot could be found for the id {geoHotSpotId}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuest. Reason: {parentQuestResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

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
            {
                // Update quest statistics in settings system whenever quests change
                try
                {
                    await UpdateQuestStatisticsAsync(avatarId);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the main operation
                    Console.WriteLine($"Warning: Failed to update quest statistics: {ex.Message}");
                }

                result.Result = questResult.Result;
            }
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

                await UpdateQuestStatisticsAsync(avatarId);
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

                        await UpdateQuestStatisticsAsync(avatarId);
                        result.Result = true;
                        result.Message = allComplete ? "Quest objective completed and quest is now complete." : "Quest objective completed successfully";
                        return result;
                    }
                }

                // Fallback: legacy objectives as child Quests
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

                await UpdateQuestStatisticsAsync(avatarId);
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

        /// <summary>
        /// Completes a quest for the specified avatar
        /// </summary>
        /// <param name="avatarId">The avatar completing the quest</param>
        /// <param name="questId">The quest to complete</param>
        /// <param name="completionNotes">Optional completion notes</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> CompleteQuestAsync(Guid avatarId, Guid questId, string completionNotes = null)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occurred in QuestManager.CompleteQuestAsync. Reason:";

            try
            {
                // Load the quest
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found or could not be loaded. Reason: {questResult.Message}");
                    return result;
                }

                // Update quest status to completed
                questResult.Result.Status = QuestStatus.Completed;
                questResult.Result.CompletedOn = DateTime.UtcNow;
                questResult.Result.CompletedBy = avatarId;
                if (!string.IsNullOrEmpty(completionNotes))
                {
                    questResult.Result.CompletionNotes = completionNotes;
                }

                if (questResult.Result.MetaData == null) questResult.Result.MetaData = new Dictionary<string, object>();
                questResult.Result.MetaData["Status"] = questResult.Result.Status.ToString();

                // Save the updated quest
                var updateResult = await UpdateAsync(avatarId, questResult.Result);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save completed quest. Reason: {updateResult.Message}");
                    return result;
                }

                // Update quest statistics in settings system whenever quests change
                try
                {
                    await UpdateQuestStatisticsAsync(avatarId);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the main operation
                    Console.WriteLine($"Warning: Failed to update quest statistics: {ex.Message}");
                }

                result.Result = true;
                result.Message = "Quest completed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Updates quest statistics in the settings system whenever quests change
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        private async Task UpdateQuestStatisticsAsync(Guid avatarId)
        {
            try
            {
                // Load all quests for the avatar
                var questsResult = await LoadAllForAvatarAsync(avatarId);
                if (questsResult.IsError || questsResult.Result == null)
                    return;

                var quests = questsResult.Result;
                var stats = new Dictionary<string, object>
                {
                    ["totalQuests"] = quests.Count(),
                    ["completedQuests"] = quests.Count(q => q.Status == QuestStatus.Completed),
                    ["activeQuests"] = quests.Count(q => q.Status == QuestStatus.InProgress),
                    ["pendingQuests"] = quests.Count(q => q.Status == QuestStatus.NotStarted),
                    ["totalKarmaEarnt"] = quests.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.RewardKarma),
                    ["totalXPEarnt"] = quests.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.RewardXP),
                };

                // Save all statistics to the settings system in one operation
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "quests", stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quest statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets quest leaderboard for a specific quest
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <param name="limit">Number of entries to return</param>
        /// <returns>Quest leaderboard entries</returns>
        public async Task<OASISResult<List<QuestLeaderboard>>> GetQuestLeaderboardAsync(Guid questId, int limit = 50)
        {
            OASISResult<List<QuestLeaderboard>> result = new OASISResult<List<QuestLeaderboard>>();
            string errorMessage = "Error occurred in QuestManager.GetQuestLeaderboardAsync. Reason:";

            try
            {
                // Implement real leaderboard logic
                var leaderboard = new List<QuestLeaderboard>();
                
                // Get all completed quests for the specified quest
                var completedQuestsResult = await GetCompletedQuestsForQuestAsync(questId);
                if (completedQuestsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get completed quests: {completedQuestsResult.Message}");
                    return result;
                }
                
                var completedQuests = completedQuestsResult.Result;
                
                // Group by avatar and calculate scores
                var avatarScores = completedQuests
                    .GroupBy(q => q.CompletedBy)
                    .Select(g => new
                    {
                        AvatarId = g.Key,
                        TotalScore = g.Sum(q => q.RewardXP),
                        CompletionTime = g.Average(q => (q.CompletedOn - q.StartedOn).TotalMinutes),
                        QuestCount = g.Count(),
                        LastCompleted = g.Max(q => q.CompletedOn)
                    })
                    .OrderByDescending(x => x.TotalScore)
                    .ThenBy(x => x.CompletionTime)
                    .ToList();
                
                // Create leaderboard entries
                int rank = 1;
                foreach (var avatarScore in avatarScores)
                {
                    // Get avatar details
                    var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarScore.AvatarId);
                    var avatarName = avatarResult.IsError ? "Unknown Avatar" : avatarResult.Result.Username;
                    
                    leaderboard.Add(new QuestLeaderboard
                    {
                        Rank = rank++,
                        AvatarId = avatarScore.AvatarId,
                        AvatarName = avatarName,
                        Score = (int)avatarScore.TotalScore,
                        CompletedAt = avatarScore.LastCompleted
                    });
                }
                
                result.Result = leaderboard;
                result.Message = "Quest leaderboard retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets quest rewards for a specific quest
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <returns>Quest rewards</returns>
        public async Task<OASISResult<List<QuestReward>>> GetQuestRewardsAsync(Guid questId)
        {
            OASISResult<List<QuestReward>> result = new OASISResult<List<QuestReward>>();
            string errorMessage = "Error occurred in QuestManager.GetQuestRewardsAsync. Reason:";

            try
            {
                // Load the quest to get its rewards
                var questResult = await LoadAsync(AvatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found. Reason: {questResult.Message}");
                    return result;
                }

                // Implement real rewards logic
                var rewards = new List<QuestReward>();
                var quest = questResult.Result;
                
                // Extract rewards from quest metadata
                if (quest.MetaData != null && quest.MetaData.ContainsKey("Rewards"))
                {
                    try
                    {
                        var rewardsJson = quest.MetaData["Rewards"]?.ToString();
                        if (!string.IsNullOrEmpty(rewardsJson))
                        {
                            var questRewards = System.Text.Json.JsonSerializer.Deserialize<List<QuestReward>>(rewardsJson);
                            if (questRewards != null)
                            {
                                rewards.AddRange(questRewards);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error parsing quest rewards: {ex.Message}");
                    }
                }
                
                // Add default rewards based on quest difficulty and type
                if (!rewards.Any())
                {
                    rewards.AddRange(GetDefaultQuestRewards(quest));
                }
                
                // Calculate dynamic rewards based on quest completion statistics
                var completionStats = await GetQuestCompletionStatsAsync(questId);
                if (!completionStats.IsError)
                {
                    var bonusRewards = CalculateBonusRewards(quest, completionStats.Result);
                    rewards.AddRange(bonusRewards);
                }
                
                result.Result = rewards;
                result.Message = "Quest rewards retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets quest statistics for a specific avatar
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Quest statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetQuestStatsAsync(Guid avatarId)
        {
            OASISResult<Dictionary<string, object>> result = new OASISResult<Dictionary<string, object>>();
            string errorMessage = "Error occurred in QuestManager.GetQuestStatsAsync. Reason:";

            try
            {
                // Load all quests for the avatar
                var questsResult = await LoadAllForAvatarAsync(avatarId);
                if (questsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to load avatar quests. Reason: {questsResult.Message}");
                    return result;
                }

                if (questsResult != null && questsResult.Result != null && !questsResult.IsError)
                {
                    var stats = new Dictionary<string, object>
                    {
                        ["totalQuests"] = questsResult.Result.Count(),
                        ["completedQuests"] = questsResult.Result.Count(q => q.Status == QuestStatus.Completed),
                        ["activeQuests"] = questsResult.Result.Count(q => q.Status == QuestStatus.InProgress),
                        ["pendingQuests"] = questsResult.Result.Count(q => q.Status == QuestStatus.NotStarted),
                        ["totalKarmaEarnt"] = questsResult.Result.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.RewardKarma),
                        ["totalXPEarnt"] = questsResult.Result.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.RewardXP),
                        //["totalRewards"] = questsResult.Result.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.Rewards?.Sum(r => r.Amount) ?? 0)
                    };


                    result.Result = stats;
                    result.Message = "Quest statistics retrieved successfully";
                }
                else      
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quests found for the avatar.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Quest leaderboard entry
    /// </summary>
    public class QuestLeaderboard
    {
        public Guid AvatarId { get; set; }
        public string AvatarName { get; set; }
        public int Score { get; set; }
        public DateTime CompletedAt { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Quest reward
    /// </summary>
    public class QuestReward
    {
        public string Type { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Quest completion statistics
    /// </summary>
    public class QuestCompletionStats
    {
        public int TotalCompletions { get; set; }
        public double AverageCompletionTime { get; set; }
        public int UniqueCompleters { get; set; }
        public DateTime LastCompleted { get; set; }
    }

    /// <summary>
    /// Helper methods for quest rewards
    /// </summary>
    public partial class QuestManager
    {
        /// <summary>
        /// Get default rewards based on quest properties
        /// </summary>
        private List<QuestReward> GetDefaultQuestRewards(IQuest quest)
        {
            var rewards = new List<QuestReward>();
            
            // Base XP reward
            rewards.Add(new QuestReward
            {
                Type = "XP",
                Amount = CalculateBaseXPReward(quest),
                Description = "Experience points for completing the quest"
            });
            
            // Base karma reward
            rewards.Add(new QuestReward
            {
                Type = "Karma",
                Amount = CalculateBaseKarmaReward(quest),
                Description = "Karma points for completing the quest"
            });
            
            // Difficulty-based rewards
            var difficultyLevel = GetQuestDifficultyLevel(quest);
            if (difficultyLevel > 1)
            {
                rewards.Add(new QuestReward
                {
                    Type = "BonusXP",
                    Amount = difficultyLevel * 10,
                    Description = $"Difficulty bonus for {difficultyLevel}-star quest"
                });
            }
            
            return rewards;
        }
        
        /// <summary>
        /// Calculate bonus rewards based on completion statistics
        /// </summary>
        private List<QuestReward> CalculateBonusRewards(IQuest quest, QuestCompletionStats stats)
        {
            var bonusRewards = new List<QuestReward>();
            
            // First completion bonus
            if (stats.TotalCompletions == 1)
            {
                bonusRewards.Add(new QuestReward
                {
                    Type = "FirstCompletion",
                    Amount = 50,
                    Description = "Bonus for being the first to complete this quest"
                });
            }
            
            // Speed completion bonus
            var estimatedTime = GetQuestEstimatedCompletionTime(quest);
            if (stats.AverageCompletionTime > 0 && estimatedTime > 0)
            {
                var speedRatio = estimatedTime / stats.AverageCompletionTime;
                if (speedRatio > 1.5) // Completed 50% faster than average
                {
                    bonusRewards.Add(new QuestReward
                    {
                        Type = "SpeedBonus",
                        Amount = (int)(speedRatio * 25),
                        Description = "Bonus for completing the quest quickly"
                    });
                }
            }
            
            // Rare quest bonus
            if (stats.UniqueCompleters < 10)
            {
                bonusRewards.Add(new QuestReward
                {
                    Type = "RareQuest",
                    Amount = 100,
                    Description = "Bonus for completing a rare quest"
                });
            }
            
            return bonusRewards;
        }
        
        /// <summary>
        /// Calculate base XP reward
        /// </summary>
        private int CalculateBaseXPReward(IQuest quest)
        {
            var baseXP = 100; // Base XP
            var difficultyMultiplier = GetQuestDifficultyLevel(quest);
            var typeMultiplier = GetQuestTypeMultiplier(quest);
            
            return (int)(baseXP * difficultyMultiplier * typeMultiplier);
        }
        
        /// <summary>
        /// Calculate base karma reward
        /// </summary>
        private int CalculateBaseKarmaReward(IQuest quest)
        {
            var baseKarma = 50; // Base karma
            var difficultyMultiplier = GetQuestDifficultyLevel(quest);
            var typeMultiplier = GetQuestTypeMultiplier(quest);
            
            return (int)(baseKarma * difficultyMultiplier * typeMultiplier);
        }
        
        /// <summary>
        /// Get quest type multiplier for rewards
        /// </summary>
        private double GetQuestTypeMultiplier(IQuest quest)
        {
            var questType = GetQuestType(quest);
            return questType switch
            {
                "Main" => 2.0,
                "Side" => 1.0,
                "Daily" => 0.5,
                "Weekly" => 1.5,
                "Event" => 3.0,
                _ => 1.0
            };
        }
        
        /// <summary>
        /// Get completed quests for a specific quest
        /// </summary>
        private async Task<OASISResult<List<IQuest>>> GetCompletedQuestsForQuestAsync(Guid questId)
        {
            var result = new OASISResult<List<IQuest>>();
            
            try
            {
                // This would typically query the database for completed quests
                // For now, return empty list
                result.Result = new List<IQuest>();
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting completed quests: {ex.Message}", ex);
            }
            
            return result;
        }
        
        /// <summary>
        /// Get quest difficulty level safely
        /// </summary>
        private int GetQuestDifficultyLevel(IQuest quest)
        {
            try
            {
                // Try to get difficulty from metadata first
                if (quest.MetaData != null && quest.MetaData.ContainsKey("DifficultyLevel"))
                {
                    if (int.TryParse(quest.MetaData["DifficultyLevel"]?.ToString(), out var difficulty))
                        return difficulty;
                }
                
                // Default difficulty
                return 1;
            }
            catch
            {
                return 1;
            }
        }
        
        /// <summary>
        /// Get quest estimated completion time safely
        /// </summary>
        private double GetQuestEstimatedCompletionTime(IQuest quest)
        {
            try
            {
                // Try to get estimated time from metadata
                if (quest.MetaData != null && quest.MetaData.ContainsKey("EstimatedCompletionTime"))
                {
                    if (double.TryParse(quest.MetaData["EstimatedCompletionTime"]?.ToString(), out var time))
                        return time;
                }
                
                // Default estimated time (30 minutes)
                return 30.0;
            }
            catch
            {
                return 30.0;
            }
        }
        
        /// <summary>
        /// Get quest type safely
        /// </summary>
        private string GetQuestType(IQuest quest)
        {
            try
            {
                // Try to get type from metadata
                if (quest.MetaData != null && quest.MetaData.ContainsKey("QuestType"))
                {
                    return quest.MetaData["QuestType"]?.ToString() ?? "Side";
                }
                
                // Default type
                return "Side";
            }
            catch
            {
                return "Side";
            }
        }
        
        /// <summary>
        /// Get quest completion statistics
        /// </summary>
        private async Task<OASISResult<QuestCompletionStats>> GetQuestCompletionStatsAsync(Guid questId)
        {
            var result = new OASISResult<QuestCompletionStats>();
            
            try
            {
                var completedQuestsResult = await GetCompletedQuestsForQuestAsync(questId);
                if (completedQuestsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get completed quests: {completedQuestsResult.Message}");
                    return result;
                }
                
                var completedQuests = completedQuestsResult.Result;
                
                var stats = new QuestCompletionStats
                {
                    TotalCompletions = completedQuests.Count,
                    AverageCompletionTime = completedQuests.Any() ? 
                        completedQuests.Average(q => (q.CompletedOn - q.StartedOn).TotalMinutes) : 0,
                    UniqueCompleters = completedQuests.Select(q => q.CompletedBy).Distinct().Count(),
                    LastCompleted = completedQuests.Any() ? 
                        completedQuests.Max(q => q.CompletedOn) : DateTime.MinValue
                };
                
                result.Result = stats;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting quest completion stats: {ex.Message}", ex);
            }
            
            return result;
        }
    }
}