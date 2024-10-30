using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public interface IQuestManager
    {
        OASISResult<IQuest> AddGeoHotSpotToQuest(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> AddGeoHotSpotToQuestAsync(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> AddGeoNFTToQuest(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> AddSubQuestToQuest(Guid parentQuestId, string name, string description, QuestType questType, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> AddSubQuestToQuestAsync(Guid parentQuestId, string name, string description, QuestType questType, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> CompleteQuest(Guid questId, Guid avatarId, ProviderType providerType);
        Task<OASISResult<IQuest>> CompleteQuestAsync(Guid questId, Guid avatarId, ProviderType providerType);
        OASISResult<IQuest> CreateQuest(string name, string description, QuestType questType, Guid avatarId, Guid missionId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> CreateQuestAsync(string name, string description, QuestType questType, Guid avatarId, Guid missionId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> CreateQuestInternal(string name, string description, QuestType questType, Guid avatarId, Guid parentMissionId = default, Guid parentQuestId = default, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> CreateQuestInternalAsync(string name, string description, QuestType questType, Guid avatarId, Guid parentMissionId = default, Guid parentQuestId = default, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> DeleteQuest(Guid questId, bool softDelete = true, bool deleteSubQuests = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> DeleteQuestAsync(Guid questId, bool softDelete = true, bool deleteSubQuests = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> FindNearestQuestOnMap();
        OASISResult<IQuest> GetCurentSubQuestForQuest(Guid questId);
        Task<OASISResult<IQuest>> GetCurentSubQuestForQuestAsync(Guid questId);
        OASISResult<IQuest> HighlightCurentStageForQuestOnMap(Guid questId);
        OASISResult<IEnumerable<IQuest>> LoadAllQuests(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsAsync(ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IQuest>> LoadAllQuestsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IQuest>> LoadAllQuestsForMission(Guid missionId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForMissionAsync(Guid missionId, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IEnumerable<IQuest>>> LoadAllQuestsForOAPPAsync(Guid OAPPId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> LoadQuest(Guid questId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> LoadQuestAsync(Guid questId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> RemoveGeoHotSpotFromQuest(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> RemoveGeoHotSpotFromQuestAsync(Guid parentQuestId, Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> RemoveGeoNFTFromQuest(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(Guid parentQuestId, Guid geoNFTId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> RemoveSubQuestFromQuest(Guid parentQuestId, Guid subQuestId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> RemoveSubQuestFromQuestAsync(Guid parentQuestId, Guid subQuestId, Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IQuest> UpdateQuest(IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IQuest>> UpdateQuestAsync(IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default);
    }
}