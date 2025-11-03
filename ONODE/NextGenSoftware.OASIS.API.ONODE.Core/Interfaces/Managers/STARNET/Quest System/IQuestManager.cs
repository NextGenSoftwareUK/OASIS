using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    //public interface IQuestManager : ISTARNETManagerBase<Quest, DownloadedQuest, InstalledQuest, QuestDNA>
    public interface IQuestManager : ISTARNETManagerBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>
    {
        //OASISResult<IQuest> AddGeoHotSpotToQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IQuest>> AddGeoHotSpotToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default);
        //OASISResult<IQuest> AddGeoNFTToQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> CreateQuestForMission(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> CreateQuestForMissionAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> CreateSubQuestForQuest(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> CreateSubQuestForQuestAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> FindNearestQuestOnMap();
        OASISResult<IQuest> GetCurentSubQuestForQuest(Guid avatarId, Guid questId, ProviderType providerType);
        Task<OASISResult<IQuest>> GetCurentSubQuestForQuestAsync(Guid avatarId, Guid questId, ProviderType providerType);
        OASISResult<IQuest> HighlightCurentStageForQuestOnMap(Guid questId);
        OASISResult<IEnumerable<IQuest>> LoadAllQuestsForMission(Guid missionId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default);
        //OASISResult<IQuest> RemoveGeoHotSpotFromQuest(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IQuest>> RemoveGeoHotSpotFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoHotSpotId, ProviderType providerType = ProviderType.Default);
        //OASISResult<IQuest> RemoveGeoNFTFromQuest(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(Guid avatarId, Guid parentQuestId, Guid geoNFTId, ProviderType providerType = ProviderType.Default);
    }
}