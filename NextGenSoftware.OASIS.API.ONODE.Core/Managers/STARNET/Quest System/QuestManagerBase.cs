using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public abstract class QuestManagerBase<T1, T2, T3, T4> : TaskManagerBase<T1, T2, T3, T4>, IQuestManagerBase<T1> 
        where T1 : IQuestBase, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public QuestManagerBase(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId, STARDNA, OASISDNA) { }
        public QuestManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, STARDNA, OASISDNA) { }
        public QuestManagerBase(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null, Type STARHolonSubType = null, HolonType STARHolonType = HolonType.STARNETHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(avatarId, STARDNA, OASISDNA, STARHolonSubType, STARHolonType, STARInstalledHolonType, STARHolonUIName, STARHolonIdName, STARHolonNameName, STARHolonTypeName, STARHolonFileExtention, STARHolonGoogleBucket, STARHolonDNAFileName, STARHolonDNAJSONName) { }
        public QuestManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null, Type STARHolonSubType = null, HolonType STARHolonType = HolonType.STARNETHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(OASISStorageProvider, avatarId, STARDNA, OASISDNA, STARHolonSubType, STARHolonType, STARInstalledHolonType, STARHolonUIName, STARHolonIdName, STARHolonNameName, STARHolonTypeName, STARHolonFileExtention, STARHolonGoogleBucket, STARHolonDNAFileName, STARHolonDNAJSONName) { }

        public async Task<OASISResult<T1>> AddQuestAsync(Guid avatarId, Guid parentId, IQuest quest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in QuestManagerBase.AddQuestAsync. Reason:";

            try
            {
                OASISResult<T1> parentChapterResult = await LoadAsync(avatarId, parentId, providerType: providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    parentChapterResult.Result.Quests.Add(quest);
                    OASISResult<T1> chapterResult = await UpdateAsync(avatarId, parentChapterResult.Result, providerType: providerType);
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(chapterResult, result);

                    if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                    {
                        quest.ParentHolonId = parentId;
                        OASISResult<T1> questSaveResult = await quest.SaveAsync<T1>();

                        if (questSaveResult != null && questSaveResult.Result != null && !questSaveResult.IsError)
                            result.Result = questSaveResult.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Quest.SaveAsync. Reason: {questSaveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with QuestManagerBase.UpdateAsync. Reason: {chapterResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with QuestManagerBase.LoadChapterAsync. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> AddQuest(Guid avatarId, Guid parentId, IQuest quest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in QuestManagerBase.AddQuest. Reason:";

            try
            {
                OASISResult<T1> parentChapterResult = Load(avatarId, parentId, providerType: providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    parentChapterResult.Result.Quests.Add(quest);
                    OASISResult<T1> chapterResult = Update(avatarId, parentChapterResult.Result, providerType: providerType);
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(chapterResult, result);

                    if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                    {
                        quest.ParentHolonId = parentId;
                        OASISResult<T1> questSaveResult = quest.Save<T1>();

                        if (questSaveResult != null && questSaveResult.Result != null && !questSaveResult.IsError)
                            result.Result = questSaveResult.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Quest.Save. Reason: {questSaveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with QuestManagerBase.UpdateAsync. Reason: {chapterResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with QuestManagerBase.LoadChapter. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T1>> RemoveQuestAsync(Guid avatarId, Guid parentChapterId, Guid questId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in QuestManagerBase.RemoveQuestAsync. Reason:";

            try
            {
                OASISResult<T1> parentChapterResult = await LoadAsync(avatarId, questId, providerType: providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    IQuest quest = parentChapterResult.Result.Quests.FirstOrDefault(x => x.Id == questId);

                    if (quest != null)
                    {
                        parentChapterResult.Result.Quests.Remove(quest);
                        OASISResult<T1> chapterResult = await UpdateAsync(avatarId, parentChapterResult.Result, providerType: providerType);
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(chapterResult, result);

                        if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                        {
                            quest.ParentHolonId = avatarId;
                            OASISResult<T1> questSaveResult = quest.Save<T1>();

                            if (questSaveResult != null && questSaveResult.Result != null && !questSaveResult.IsError)
                                result.Result = questSaveResult.Result;
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Quest.Save. Reason: {questSaveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with QuestManagerBase.UpdateAsync. Reason: {chapterResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest could be found for the id {questId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with QuestManagerBase.LoadChapterAsync. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> RemoveQuest(Guid avatarId, Guid parentChapterId, Guid questId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in QuestManagerBase.RemoveQuestFromChapter. Reason:";

            try
            {
                OASISResult<T1> parentChapterResult = Load(avatarId, questId, providerType: providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    IQuest quest = parentChapterResult.Result.Quests.FirstOrDefault(x => x.Id == questId);

                    if (quest != null)
                    {
                        parentChapterResult.Result.Quests.Remove(quest);
                        OASISResult<T1> chapterResult = Update(avatarId, parentChapterResult.Result, providerType: providerType);
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(chapterResult, result);

                        if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                        {
                            quest.ParentHolonId = avatarId;
                            OASISResult<T1> questSaveResult = quest.Save<T1>();

                            if (questSaveResult != null && questSaveResult.Result != null && !questSaveResult.IsError)
                                result.Result = questSaveResult.Result;
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the quest with Quest.Save. Reason: {questSaveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with QuestManagerBase.Update. Reason: {chapterResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest could be found for the id {questId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with QuestManagerBase.LoadChapter. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> GetCurentQuestAsync(Guid avatarId, Guid chapterId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManagerBase.GetCurentQuestAsync. Reason:";

            OASISResult<T1> loadResult = await LoadAsync(avatarId, chapterId, version, providerType: providerType);

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
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The chapter was already completed on {loadResult.Result.CompletedOn} by {loadResult.Result.CompletedBy}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with QuestManagerBase.LoadChapterAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IQuest> GetCurentQuest(Guid avatarId, Guid chapterId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in QuestManagerBase.GetCurentQuest. Reason:";

            OASISResult<T1> loadResult = Load(avatarId, chapterId, version, providerType: providerType);

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
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The chapter was already completed on {loadResult.Result.CompletedOn} by {loadResult.Result.CompletedBy}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with QuestManagerBase.LoadChapter. Reason: {loadResult.Message}");

            return result;
        }
    }
}