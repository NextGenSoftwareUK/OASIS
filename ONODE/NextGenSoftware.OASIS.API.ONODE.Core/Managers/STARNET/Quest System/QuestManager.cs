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
{    //public class QuestManager : QuestManagerBase<Quest, DownloadedQuest, InstalledQuest, QuestDNA>, IQuestManager
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

    }
}