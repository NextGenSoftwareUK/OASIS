using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public abstract class TaskManagerBase<T1, T2, T3, T4> : STARNETManagerBase<T1, T2, T3, T4>, ITaskManagerBase<T1> 
        where T1 : IQuestBase, new()
        where T2 : IDownloadedSTARNETHolon, new()
        where T3 : IInstalledSTARNETHolon, new()
        where T4 : ISTARNETDNA, new()
    {
        public TaskManagerBase(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public TaskManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }
        public TaskManagerBase(Guid avatarId, OASISDNA OASISDNA = null, Type STARHolonSubType = null, HolonType STARHolonType = HolonType.STARNETHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(avatarId, OASISDNA, STARHolonSubType, STARHolonType, STARInstalledHolonType, STARHolonUIName, STARHolonIdName, STARHolonNameName, STARHolonTypeName, STARHolonFileExtention, STARHolonGoogleBucket, STARHolonDNAFileName, STARHolonDNAJSONName) { }
        public TaskManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null, Type STARHolonSubType = null, HolonType STARHolonType = HolonType.STARNETHolon, HolonType STARInstalledHolonType = HolonType.InstalledSTARNETHolon, string STARHolonUIName = "OAPP System Holon", string STARHolonIdName = "STARHolonId", string STARHolonNameName = "STARHolonName", string STARHolonTypeName = "STARHolonType", string STARHolonFileExtention = "oappsystemholon", string STARHolonGoogleBucket = "oasis_oappsystemholons", string STARHolonDNAFileName = "STARHolonDNA.json", string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(OASISStorageProvider, avatarId, OASISDNA, STARHolonSubType, STARHolonType, STARInstalledHolonType, STARHolonUIName, STARHolonIdName, STARHolonNameName, STARHolonTypeName, STARHolonFileExtention, STARHolonGoogleBucket, STARHolonDNAFileName, STARHolonDNAJSONName) { }

        public async Task<OASISResult<T1>> CompleteAsync(Guid avatarId, Guid chapterId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in ChapterManager.CompleteAsync. Reason:";

            try
            {
                OASISResult<T1> loadResult = await LoadAsync(avatarId, chapterId, providerType: providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    OASISResult<T1> chapterResult = await UpdateAsync(avatarId, loadResult.Result, providerType);

                    if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                        result.Result = chapterResult.Result;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with ChapterManager.UpdateAsync. Reason: {chapterResult.Message}");

                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(chapterResult, result);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T1> Complete(Guid avatarId, Guid chapterId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "Error occured in ChapterManager.Complete. Reason:";

            try
            {
                OASISResult<T1> loadResult = Load(avatarId, chapterId, providerType: providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                {
                    loadResult.Result.CompletedOn = DateTime.Now;
                    loadResult.Result.CompletedBy = avatarId;

                    OASISResult<T1> chapterResult = Update(avatarId, loadResult.Result, providerType);

                    if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                        result.Result = chapterResult.Result;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the chapter with ChapterManager.Update. Reason: {chapterResult.Message}");

                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(chapterResult, result);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }
    }
}