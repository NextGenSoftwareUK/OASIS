using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public class AutoReplicateAvatarWorkerParams
        {
            public IAvatar Avatar { get; set; }
            public OASISResult<IAvatar> Result { get; set; }
            public ProviderType PreviousProviderType { get; set; }
        }

        public class AutoReplicateAvatarDetailWorkerParams
        {
            public IAvatarDetail AvatarDetail { get; set; }
            public OASISResult<IAvatarDetail> Result { get; set; }
            public ProviderType PreviousProviderType { get; set; }
        }

        public void AutoReplicateAvatarWorker(object obj)
        {
            AutoReplicateAvatarWorkerParams workerParams = obj as AutoReplicateAvatarWorkerParams;

            if (workerParams != null)
            {
                OASISResult<IAvatar> result = workerParams.Result;

                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != workerParams.PreviousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        workerParams.Result = SaveAvatarForProvider(workerParams.Avatar, workerParams.Result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarResults(workerParams.Avatar, result, workerParams.PreviousProviderType);
            }
        }

        public void AutoReplicateAvatarDetailWorker(object obj)
        {
            AutoReplicateAvatarDetailWorkerParams workerParams = obj as AutoReplicateAvatarDetailWorkerParams;

            if (workerParams != null)
            {
                OASISResult<IAvatarDetail> result = workerParams.Result;

                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != workerParams.PreviousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        workerParams.Result = SaveAvatarDetailForProvider(workerParams.AvatarDetail, workerParams.Result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarDetailResults(workerParams.AvatarDetail, result, workerParams.PreviousProviderType);
            }
        }

        private async Task<OASISResult<IAvatar>> AutoReplicateAvatarAsync(IAvatar avatar, OASISResult<IAvatar> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = await SaveAvatarForProviderAsync(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarResults(avatar, result, previousProviderType);     
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarWorkerParams() { Avatar = avatar, Result = result, PreviousProviderType = previousProviderType });
            }

            return result;
        }

        private OASISResult<IAvatar> AutoReplicateAvatar(IAvatar avatar, OASISResult<IAvatar> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = SaveAvatarForProvider(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarResults(avatar, result, previousProviderType);
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarWorkerParams() { Avatar = avatar, Result = result, PreviousProviderType = previousProviderType });
            }

            return result;
        }

        private async Task<OASISResult<IAvatarDetail>> AutoReplicateAvatarDetailAsync(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = await SaveAvatarDetailForProviderAsync(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarDetailResults(avatar, result, previousProviderType);
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarDetailWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarDetailWorkerParams() { AvatarDetail = avatar, Result = result, PreviousProviderType = previousProviderType });
            }

            return result;
        }

        private OASISResult<IAvatarDetail> AutoReplicateAvatarDetail(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, ProviderType previousProviderType, bool waitForAutoReplicationResult = false)
        {
            if (waitForAutoReplicationResult)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = SaveAvatarDetailForProvider(avatar, result, SaveMode.AutoReplication, type.Value);
                }

                result = ProcessAvatarDetailResults(avatar, result, previousProviderType);
            }
            else
            {
                Thread worker = new Thread(new ParameterizedThreadStart(AutoReplicateAvatarWorker));
                worker.IsBackground = true;
                worker.Start(new AutoReplicateAvatarDetailWorkerParams() { AvatarDetail = avatar, Result = result, PreviousProviderType = previousProviderType });
            }

            return result;
        }

        private OASISResult<IAvatar> ProcessAvatarResults(IAvatar avatar, OASISResult<IAvatar> result, ProviderType previousProviderType)
        {
            if (result.WarningCount > 0)
                ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
            else
                LoggingManager.Log("Avatar Successfully Saved/Replicated", LogType.Info, ref result, true, false);

            return result;
        }

        private OASISResult<IAvatarDetail> ProcessAvatarDetailResults(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, ProviderType previousProviderType)
        {
            if (result.WarningCount > 0)
                ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
            else
                LoggingManager.Log("Avatar Detail Successfully Saved/Replicated", LogType.Info, ref result, true, false);

            return result;
        }
    }
}