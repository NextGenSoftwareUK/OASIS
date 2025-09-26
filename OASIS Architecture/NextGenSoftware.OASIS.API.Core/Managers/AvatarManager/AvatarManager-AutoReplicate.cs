using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public class AutoReplicateAvatarWorkerParams
        {
            public IAvatar Avatar { get; set; }
            public OASISResult<IAvatar> Result { get; set; }
            public ProviderType PreviousProviderType { get; set; }
            public string ProviderList { get; set; }
        }

        public class AutoReplicateAvatarDetailWorkerParams
        {
            public IAvatarDetail AvatarDetail { get; set; }
            public OASISResult<IAvatarDetail> Result { get; set; }
            public ProviderType PreviousProviderType { get; set; }
            public string ProviderList { get; set; }
        }

        public void AutoReplicateAvatarWorker(object obj)
        {
            AutoReplicateAvatarWorkerParams workerParams = obj as AutoReplicateAvatarWorkerParams;

            if (workerParams != null)
            {
                OASISResult<IAvatar> result = workerParams.Result;
                //string providerList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString();

                //foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersFromListAsEnumList("AutoReplicate", workerParams.ProviderList).Result)
                {
                    if (type.Value != workerParams.PreviousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        workerParams.Result = SaveAvatarForProvider(workerParams.Avatar, workerParams.Result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarResults(workerParams.Avatar, result, workerParams.PreviousProviderType, workerParams.ProviderList);
            }
        }

        public void AutoReplicateAvatarDetailWorker(object obj)
        {
            AutoReplicateAvatarDetailWorkerParams workerParams = obj as AutoReplicateAvatarDetailWorkerParams;

            if (workerParams != null)
            {
                OASISResult<IAvatarDetail> result = workerParams.Result;
                //string providerList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString();

                //foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersFromListAsEnumList("AutoReplicate", workerParams.ProviderList).Result)
                {
                    if (type.Value != workerParams.PreviousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        workerParams.Result = SaveAvatarDetailForProvider(workerParams.AvatarDetail, workerParams.Result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarDetailResults(workerParams.AvatarDetail, result, workerParams.PreviousProviderType, workerParams.ProviderList);
            }
        }

        private async Task<OASISResult<IAvatar>> AutoReplicateAvatarAsync(IAvatar avatar, OASISResult<IAvatar> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        result = await SaveAvatarForProviderAsync(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarResults(avatar, result, previousProviderType, ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString());     
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarWorkerParams() { Avatar = avatar, Result = result, PreviousProviderType = previousProviderType, ProviderList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString() });
            }

            return result;
        }

        private OASISResult<IAvatar> AutoReplicateAvatar(IAvatar avatar, OASISResult<IAvatar> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        result = SaveAvatarForProvider(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarResults(avatar, result, previousProviderType, ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString());
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarWorkerParams() { Avatar = avatar, Result = result, PreviousProviderType = previousProviderType, ProviderList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString() });
            }

            return result;
        }

        private async Task<OASISResult<IAvatarDetail>> AutoReplicateAvatarDetailAsync(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        result = await SaveAvatarDetailForProviderAsync(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarDetailResults(avatar, result, previousProviderType, ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString());
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarDetailWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarDetailWorkerParams() { AvatarDetail = avatar, Result = result, PreviousProviderType = previousProviderType, ProviderList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString() });
            }

            return result;
        }

        private OASISResult<IAvatarDetail> AutoReplicateAvatarDetail(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        result = SaveAvatarDetailForProvider(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarDetailResults(avatar, result, previousProviderType, ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString());
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarDetailWorkerParams() { AvatarDetail = avatar, Result = result, PreviousProviderType = previousProviderType, ProviderList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString() });
            }

            return result;
        }

        private OASISResult<IAvatar> ProcessAvatarResults(IAvatar avatar, OASISResult<IAvatar> result, ProviderType previousProviderType, string providerList)
        {
            if (result.WarningCount > 0)
                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", providerList), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                //OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
            else
                LoggingManager.Log("Avatar Successfully Saved/Replicated", LogType.Info, ref result, true, false);

            return result;
        }

        private OASISResult<IAvatarDetail> ProcessAvatarDetailResults(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, ProviderType previousProviderType, string providerList)
        {
            if (result.WarningCount > 0)
                //OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", providerList), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
            else
                LoggingManager.Log("Avatar Detail Successfully Saved/Replicated", LogType.Info, ref result, true, false);

            return result;
        }
    }
}