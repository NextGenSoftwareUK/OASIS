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
    public class ChapterManager : OASISManager//, IChapterManager
    {
        //QuestManager _questManager = null;
        //NFTManager _nftManager = null;

        ////TODO: May remove NFTManager and QuestManager DI (only used for adding quests to a chapter with the questId (they can use the other overload that takes the IQuest)
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

        //public ChapterManager(Guid avatarId, QuestManager questManager, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        //{
        //    _questManager = questManager;
        //}

        //public ChapterManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, QuestManager questManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        //{
        //    _questManager = questManager;
        //}

        public ChapterManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            //_questManager = questManager;
        }

        public ChapterManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            //_questManager = questManager;
        }

        public async Task<OASISResult<IChapter>> CreateChapterAsync(string name, string description, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.CreateChapterAsync. Reason:";

            try
            {
                IChapter chapter = new Chapter()
                {
                    Name = name,
                    Description = description,
                    StartedBy = avatarId,
                    StartedOn = DateTime.Now,
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.Now
                };

                OASISResult<Chapter> saveHolonResult = await Data.SaveHolonAsync<Chapter>(chapter, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with Data.SaveHolonAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IChapter> CreateChapter(string name, string description, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.CreateChapter. Reason:";

            try
            {
                IChapter chapter = new Chapter()
                {
                    Name = name,
                    Description = description,
                    StartedBy = avatarId,
                    StartedOn = DateTime.Now,
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.Now
                };

                OASISResult<Chapter> saveHolonResult = Data.SaveHolon<Chapter>(chapter, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with Data.SaveHolon. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IChapter>> UpdateChapterAsync(IChapter chapter, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.UpdateChapterAsync. Reason:";

            try
            {
                OASISResult<Chapter> saveHolonResult = await chapter.SaveAsync<Chapter>();

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with chapter.SaveAsync. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IChapter> UpdateChapter(IChapter chapter, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.UpdateChapter. Reason:";

            try
            {
                OASISResult<Chapter> saveHolonResult = chapter.Save<Chapter>();

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = saveHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with chapter.Save. Reason: {saveHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IChapter>> LoadChapterAsync(Guid chapterId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.LoadChapterAsync. Reason:";

            try
            {
                OASISResult<Chapter> loadHolonResult = await Data.LoadHolonAsync<Chapter>(chapterId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                {
                    result.Result = loadHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with Data.LoadHolonAsync. Reason: {loadHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IChapter> LoadChapter(Guid chapterId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.LoadChapterAsync. Reason:";

            try
            {
                OASISResult<Chapter> loadHolonResult = Data.LoadHolon<Chapter>(chapterId, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                {
                    result.Result = loadHolonResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with Data.LoadHolon. Reason: {loadHolonResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IChapter>>> LoadAllChaptersAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IChapter>> result = new OASISResult<IEnumerable<IChapter>>();
            string errorMessage = "Error occured in ChapterManager.LoadAllChaptersAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Chapter>> loadHolonsResult = await Data.LoadAllHolonsAsync<Chapter>(HolonType.Chapter, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with Data.LoadAllHolonsAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IChapter>> LoadAllChapters(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IChapter>> result = new OASISResult<IEnumerable<IChapter>>();
            string errorMessage = "Error occured in ChapterManager.LoadAllChaptersAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Chapter>> loadHolonsResult = Data.LoadAllHolons<Chapter>(HolonType.Chapter, true, true, 0, true, false, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with Data.LoadAllChapters. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IChapter>>> LoadAllChaptersForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IChapter>> result = new OASISResult<IEnumerable<IChapter>>();
            string errorMessage = "Error occured in ChapterManager.LoadAllChaptersForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<Chapter>> loadHolonsResult = await Data.LoadHolonsForParentAsync<Chapter>(avatarId, HolonType.Chapter, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with Data.LoadHolonsForParentAsync. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IChapter>> LoadAllChaptersForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IChapter>> result = new OASISResult<IEnumerable<IChapter>>();
            string errorMessage = "Error occured in ChapterManager.LoadAllChaptersForAvatar. Reason:";

            try
            {
                OASISResult<IEnumerable<Chapter>> loadHolonsResult = Data.LoadHolonsForParent<Chapter>(avatarId, HolonType.Chapter, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

                if (loadHolonsResult != null && loadHolonsResult.Result != null && !loadHolonsResult.IsError)
                {
                    result.Result = loadHolonsResult.Result;
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with Data.LoadAllChaptersForAvatar. Reason: {loadHolonsResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IChapter>> CompleteChapterAsync(Guid chapterId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.CompleteChapterAsync. Reason:";

            try
            {
                OASISResult<IChapter> loadResult = await LoadChapterAsync(chapterId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    result = await UpdateChapterAsync(loadResult.Result, avatarId, providerType);

                    if (!(result != null && result.Result != null && !result.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with ChapterManager.UpdateChapterAsync. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IChapter> CompleteChapter(Guid chapterId, Guid avatarId, ProviderType providerType)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.CompleteChapter. Reason:";

            try
            {
                OASISResult<IChapter> loadResult = LoadChapter(chapterId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    result = UpdateChapter(loadResult.Result, avatarId, providerType);

                    if (!(result != null && result.Result != null && !result.IsError))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with ChapterManager.UpdateChapter. Reason: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        //TODO: Dont think we need this? When we can just use the other overload that takes a IQuest?
        //public async Task<OASISResult<IChapter>> AddQuestToChapterAsync(Guid parentChapterId, Guid questId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IChapter> result = new OASISResult<IChapter>();
        //    string errorMessage = "Error occured in ChapterManager.AddQuestToChapterAsync. Reason:";

        //    try
        //    {
        //        OASISResult<IChapter> parentChapterResult = await LoadChapterAsync(parentChapterId, providerType);

        //        if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
        //        {
        //            OASISResult<IQuest> questResult = QuestManager.LoadQuestAsync(questId); 

        //            if (questResult != null && questResult.Result != null && !questResult.IsError)
        //            {
        //                parentChapterResult.Result.Quests.Add(questResult.Result);
        //                result = await UpdateChapterAsync(parentChapterResult.Result, avatarId, providerType);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the quest with QuestManager.LoadQuestAsync. Reason: {questResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapterAsync. Reason: {parentChapterResult.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IChapter>> AddQuestToChapterAsync(Guid parentChapterId, IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.AddQuestToChapterAsync. Reason:";

            try
            {
                OASISResult<IChapter> parentChapterResult = await LoadChapterAsync(parentChapterId, providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    parentChapterResult.Result.SubQuests.Add(quest);
                    result = await UpdateChapterAsync(parentChapterResult.Result, avatarId, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapterAsync. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IChapter>> AddQuestToChapter(Guid parentChapterId, IQuest quest, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.AddQuestToChapter. Reason:";

            try
            {
                OASISResult<IChapter> parentChapterResult = LoadChapter(parentChapterId, providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    parentChapterResult.Result.SubQuests.Add(quest);
                    result = UpdateChapter(parentChapterResult.Result, avatarId, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapter. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }



        public async Task<OASISResult<IChapter>> RemoveQuestFromChapterAsync(Guid parentChapterId, Guid questId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.RemoveQuestFromChapterAsync. Reason:";

            try
            {
                OASISResult<IChapter> parentChapterResult = await LoadChapterAsync(questId, providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    IQuest quest = parentChapterResult.Result.SubQuests.FirstOrDefault(x => x.Id == questId);

                    if (quest != null)
                    {
                        parentChapterResult.Result.SubQuests.Remove(quest);
                        result = await UpdateChapterAsync(parentChapterResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest could be found for the id {questId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapterAsync. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IChapter> RemoveQuestFromChapter(Guid parentChapterId, Guid questId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.RemoveQuestFromChapter. Reason:";

            try
            {
                OASISResult<IChapter> parentChapterResult = LoadChapter(questId, providerType);

                if (parentChapterResult != null && parentChapterResult.Result != null && !parentChapterResult.IsError)
                {
                    IQuest quest = parentChapterResult.Result.SubQuests.FirstOrDefault(x => x.Id == questId);

                    if (quest != null)
                    {
                        parentChapterResult.Result.SubQuests.Remove(quest);
                        result = UpdateChapter(parentChapterResult.Result, avatarId, providerType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quest could be found for the id {questId}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapter. Reason: {parentChapterResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IChapter>> DeleteChapterAsync(Guid chapterId, bool softDelete = true, bool deleteSubChapters = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.DeleteChapterAsync. Reason:";

            try
            {
                OASISResult<IHolon> deleteResult = await Data.DeleteHolonAsync(chapterId, softDelete, providerType);

                //TODO:Delete sub-chapters, hotspots and nfts etc

                if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                    result.Result = (IChapter)deleteResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured deleting the chapter with Data.DeleteHolonAsync. Reason: {deleteResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IChapter> DeleteChapter(Guid chapterId, bool softDelete = true, bool deleteSubChapters = true, bool deleteGeoNFTs = false, bool deleteHotSpots = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IChapter> result = new OASISResult<IChapter>();
            string errorMessage = "Error occured in ChapterManager.DeleteChapter. Reason:";

            try
            {
                //TODO:Delete sub-chapters, hotspots and nfts etc
                OASISResult<IHolon> deleteResult = Data.DeleteHolon(chapterId, softDelete, providerType);

                if (deleteResult != null && deleteResult.Result != null && !deleteResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteResult, result);
                    result.Result = (IChapter)deleteResult.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured deleting the chapter with Data.DeleteHolon. Reason: {deleteResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IQuest>> GetCurentQuestForChapterAsync(Guid chapterId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in ChapterManager.GetCurentQuestForChapterAsync. Reason:";

            OASISResult<IChapter> loadResult = await LoadChapterAsync(chapterId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.SubQuests != null && loadResult.Result.SubQuests.Count() > 0)
                    {
                        result.Result = loadResult.Result.SubQuests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

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
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapterAsync. Reason: {loadResult.Message}");

            return result;
        }

        public OASISResult<IQuest> GetCurentQuestForChapter(Guid chapterId)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            string errorMessage = "Error occured in ChapterManager.GetCurentQuestForChapter. Reason:";

            OASISResult<IChapter> loadResult = LoadChapter(chapterId);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                if (loadResult.Result.CompletedOn != DateTime.MinValue)
                {
                    if (loadResult.Result.SubQuests != null && loadResult.Result.SubQuests.Count() > 0)
                    {
                        result.Result = loadResult.Result.SubQuests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);

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
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the chapter with ChapterManager.LoadChapter. Reason: {loadResult.Message}");

            return result;
        }

        public async Task<OASISResult<IList<IChapter>>> GetAllCurrentChaptersForAvatarAsync(Guid avatarId)
        {
            OASISResult<IList<IChapter>> result = new OASISResult<IList<IChapter>>();

            return result;
        }
    }
}
