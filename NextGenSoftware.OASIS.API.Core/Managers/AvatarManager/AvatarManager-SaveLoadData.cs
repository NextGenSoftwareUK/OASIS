using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public OASISResult<bool> SaveData(string key, string value, Guid avatarId, ProviderType providerType = ProviderType.Default, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "An error occured in AvatarManager.SaveData. Reason: ";

            OASISResult<IAvatar> avatarLoadResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarLoadResult != null && avatarLoadResult.Result != null && !avatarLoadResult.IsError)
            {
                avatarLoadResult.Result.MetaData[key] = value;
                OASISResult<IAvatar> avatarSaveResult = avatarLoadResult.Result.Save(autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);

                if (avatarSaveResult != null && avatarSaveResult.Result != null && !avatarSaveResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatarSaveResult, result);
                    result.Result = true;
                    result.Message = "Data Saved";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error saving the data to the avatar. Reason: {avatarSaveResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the avatar. Reason: {avatarLoadResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> SaveDataAsync(string key, string value, Guid avatarId, ProviderType providerType = ProviderType.Default, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "An error occured in AvatarManager.SaveDataAsync. Reason: ";

            OASISResult<IAvatar> avatarLoadResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarLoadResult != null && avatarLoadResult.Result != null && !avatarLoadResult.IsError)
            {
                avatarLoadResult.Result.MetaData[key] = value;
                OASISResult<IAvatar> avatarSaveResult = await avatarLoadResult.Result.SaveAsync(autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);

                if (avatarSaveResult != null && avatarSaveResult.Result != null && !avatarSaveResult.IsError)
                {
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatarSaveResult, result);
                    result.Result = true;
                    result.Message = "Data Saved";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error saving the data to the avatar. Reason: {avatarSaveResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the avatar. Reason: {avatarLoadResult.Message}");

            return result;
        }

        public OASISResult<string> LoadData(string key, Guid avatarId, ProviderType providerType = ProviderType.Default, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false)
        {
            OASISResult<string> result = new OASISResult<string>();
            string errorMessage = "An error occured in AvatarManager.LoadData. Reason: ";

            OASISResult<IAvatar> avatarLoadResult = AvatarManager.Instance.LoadAvatar(avatarId, false, true, providerType);

            if (avatarLoadResult != null && avatarLoadResult.Result != null && !avatarLoadResult.IsError)
            {
                result.Result = avatarLoadResult.Result.MetaData[key].ToString();
                result.IsLoaded = true;
                result.Message = "Data Loaded";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the avatar. Reason: {avatarLoadResult.Message}");

            return result;
        }

        public async Task<OASISResult<string>> LoadDataAsync(string key, Guid avatarId, ProviderType providerType = ProviderType.Default, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false)
        {
            OASISResult<string> result = new OASISResult<string>();
            string errorMessage = "An error occured in AvatarManager.LoadDataAsync. Reason: ";

            OASISResult<IAvatar> avatarLoadResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId, false, true, providerType);

            if (avatarLoadResult != null && avatarLoadResult.Result != null && !avatarLoadResult.IsError)
            {
                result.Result = avatarLoadResult.Result.MetaData[key].ToString();
                result.IsLoaded = true;
                result.Message = "Data Loaded";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the avatar. Reason: {avatarLoadResult.Message}");

            return result;
        }
    }
}