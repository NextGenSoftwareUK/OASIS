using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatarDetail avatar, KarmaTypePositive karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in AddKarmaToAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Message = "Karma Successfully Added To Avatar.";
                    result.IsSaved = true;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}", result.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result.Result;

            //TODO: Need to handle return of OASISResult properly..
            ////TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc....
            //return await ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider).Result.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }
        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(Guid avatarId, KarmaTypePositive karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in AddKarmaToAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = await providerResult.Result.LoadAvatarDetailAsync(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = await providerResult.Result.AddKarmaToAvatarAsync(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Added To Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}", result.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result.Result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //IAvatarDetail avatar = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            //return await ProviderManager.Instance.CurrentStorageProvider.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail avatar, KarmaTypePositive karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return new OASISResult<KarmaAkashicRecord>(ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType).Result.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result);
        }
        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(Guid avatarId, KarmaTypePositive karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in AddKarmaToAvatar method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = providerResult.Result.LoadAvatarDetail(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = providerResult.Result.AddKarmaToAvatar(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Added To Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}", result.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result;

            /*
            //TODO: Need to handle return of OASISResult properly...
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();
            OASISResult<IAvatarDetail> avatarResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            //IAvatar avatar = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatar(avatarId);

            if (avatarResult != null )
            {
                result.Result = ProviderManager.Instance.CurrentStorageProvider.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result.Result != null)
                    result.Message = "Karma Successfully Added To Avatar.";
            }
            else
            {
                result.IsError = true;
                result.Message = "Avatar Not Found";
            }

            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...

            return result;
            */
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatarDetail avatar, KarmaTypeNegative karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return await ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider).Result.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result;

            string errorMessage = "Error in RemoveKarmaFromAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Message = "Karma Successfully Removed From Avatar.";
                    result.IsSaved = true;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}", result.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result.Result;
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(Guid avatarId, KarmaTypeNegative karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in RemoveKarmaFromAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = await providerResult.Result.LoadAvatarDetailAsync(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = await providerResult.Result.RemoveKarmaFromAvatarAsync(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Removed From Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result.Result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //IAvatarDetail avatar = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatarDetail(avatarId);
            //return await ProviderManager.Instance.CurrentStorageProvider.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatarDetail avatar, KarmaTypeNegative karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider).Result.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

            string errorMessage = "Error in RemoveKarmaFromAvatar method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = providerResult.Result.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Message = "Karma Successfully Removed From Avatar.";
                    result.IsSaved = true;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}", result.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result.Result;
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(Guid avatarId, KarmaTypeNegative karmaType, Enums.KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in RemoveKarmaFromAvatar method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = providerResult.Result.LoadAvatarDetail(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = providerResult.Result.RemoveKarmaFromAvatar(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Removed From Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}", result.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}", providerResult.DetailedMessage);

            return result;
        }
    }
}
