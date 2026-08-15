using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    public partial class AzureCosmosDBOASIS
    {
        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in AzureCosmosDBOASIS Provider. Reason:";

            try
            {
                if (dbClientFactory == null)
                {
                    var cosmosClient = new CosmosClient(serviceEndpoint.ToString(), authKey);
                    dbClientFactory = new CosmosDbClientFactory(cosmosClient, databaseName, collectionNames);
                    OASISResult<bool> ensureDbSetupResult = dbClientFactory.EnsureDbSetupAsync().Result;

                    if (ensureDbSetupResult.IsError || !ensureDbSetupResult.Result)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from EnsureDbSetupAsync: {ensureDbSetupResult.Message}.");
                    else
                    {
                        avatarRepository = new AvatarRepository(dbClientFactory);
                        holonRepository = new HolonRepository(dbClientFactory);
                        avatarDetailRepository = new AvatarDetailRepository(dbClientFactory);
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in AzureCosmosDBOASIS Provider. Reason:";

            try
            {
                if (dbClientFactory == null)
                {
                    var cosmosClient = new CosmosClient(serviceEndpoint.ToString(), authKey);
                    dbClientFactory = new CosmosDbClientFactory(cosmosClient, databaseName, collectionNames);
                    OASISResult<bool> ensureDbSetupResult = await dbClientFactory.EnsureDbSetupAsync();

                    if (ensureDbSetupResult.IsError || !ensureDbSetupResult.Result)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from EnsureDbSetupAsync: {ensureDbSetupResult.Message}.");
                    else
                    {
                        avatarRepository = new AvatarRepository(dbClientFactory);
                        holonRepository = new HolonRepository(dbClientFactory);
                        avatarDetailRepository = new AvatarDetailRepository(dbClientFactory);
                    }

                    IsProviderActivated = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}.");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            dbClientFactory = null;
            avatarRepository = null;
            avatarDetailRepository = null;
            holonRepository = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
            //return base.DeActivateProvider();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            dbClientFactory = null;
            avatarRepository = null;
            avatarDetailRepository = null;
            holonRepository = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
            //return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with id {id}";
            try
            {
                if (softDelete)
                {
                    OASISResult<IAvatar> avatarResult = LoadAvatar(id);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                            reason = avatarResult.Message;

                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    avatarRepository.DeleteAsync(id).Wait();

                result.Result = true;
                result.IsSaved = true;                
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with providerKey {providerKey}";
            try
            {
                //TODO HB: Re-write as same way as DeleteHolon methods do... thanks
                //Normally the providerKey is different to the Id but in this case they are the same since Azure uses GUID's the same as the OASIS does for ID.
                if (softDelete)
                {
                    OASISResult<IAvatar> avatarResult = LoadAvatar(new Guid(providerKey));

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                            reason = avatarResult.Message;

                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    avatarRepository.DeleteAsync(providerKey).Wait();

                result.Result = true;
                result.IsSaved = true;                
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with id {id}";
            try
            {
                //TODO HB: Re-write as same way as DeleteHolon methods do... thanks
                if (softDelete)
                {
                    OASISResult<IAvatar> avatarResult = await LoadAvatarAsync(id);

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = await SaveAvatarAsync(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                            reason = avatarResult.Message;

                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    await avatarRepository.DeleteAsync(id);                
            }
            catch (Exception ex) {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public async override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with providerKey {providerKey}";
            try
            {
                //Normally the providerKey is different to the Id but in this case they are the same since Azure uses GUID's the same as the OASIS does for ID.
                if (softDelete)
                {
                    OASISResult<IAvatar> avatarResult = await LoadAvatarAsync(new Guid(providerKey));

                    if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = await SaveAvatarAsync(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                            reason = avatarResult.Message;

                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    await avatarRepository.DeleteAsync(providerKey);                
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with email {avatarEmail}";
            try
            {
                //TODO HB: Re-write as same way as DeleteHolon methods do... thanks
                //TODO: May want to cache this in future?

                OASISResult<IAvatar> avatarResult = LoadAvatarByEmail(avatarEmail);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    if (softDelete)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        avatarRepository.DeleteAsync(avatarResult.Result).Wait();
                    }
                }
                else
                {
                    if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                        reason = avatarResult.Message;

                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with email {avatarEmail}";
            try
            {
                //TODO HB: Re-write as same way as DeleteHolon methods do... thanks
                //TODO: May want to cache this in future?
                
                OASISResult<IAvatar> avatarResult = await LoadAvatarByEmailAsync(avatarEmail);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    if (softDelete)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = await SaveAvatarAsync(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        await avatarRepository.DeleteAsync(avatarResult.Result);
                    }
                }
                else
                {
                    if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                        reason = avatarResult.Message;

                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                }                            
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with username {avatarUsername}";
            try
            {
                //TODO HB: Re-write as same way as DeleteHolon methods do... thanks
                //TODO: May want to cache this in future?

                OASISResult<IAvatar> avatarResult = LoadAvatarByUsername(avatarUsername);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    if (softDelete)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        avatarRepository.DeleteAsync(avatarResult.Result).Wait();
                    }
                }
                else
                {
                    if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                        reason = avatarResult.Message;

                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the avatar with user name {avatarUsername}";
            try
            {
                //TODO HB: Re-write as same way as DeleteHolon methods do... thanks
                //TODO: May want to cache this in future?

                OASISResult<IAvatar> avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    if (softDelete)
                    {
                        avatarResult.Result.DeletedDate = DateTime.Now;
                        avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IAvatar> saveAvatarResult = await SaveAvatarAsync(avatarResult.Result);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveAvatarResult != null && !string.IsNullOrEmpty(saveAvatarResult.Message))
                                reason = saveAvatarResult.Message;

                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {avatarResult.Result.Id} and name {avatarResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        await avatarRepository.DeleteAsync(avatarResult.Result);
                    }
                }
                else
                {
                    if (avatarResult != null && !string.IsNullOrEmpty(avatarResult.Message))
                        reason = avatarResult.Message;

                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }
            return result;
        }

        //public override OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();
        //    string reason = "unknown";
        //    string softDeleting = "";

        //    if (softDelete)
        //        softDeleting = "soft";

        //    string errorMessage = $"An error occured {softDeleting} deleting the holon with id {id}";

        //    try
        //    {
        //        if (softDelete)
        //        {
        //            OASISResult<IHolon> holonResult = LoadHolon(id);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                holonResult.Result.DeletedDate = DateTime.Now;
        //                holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
        //                OASISResult<IHolon> saveHolonResult = SaveHolon(holonResult.Result);

        //                if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
        //                {
        //                    result.Result = saveHolonResult.Result;
        //                    result.IsSaved = true;
        //                }
        //                else
        //                {
        //                    if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
        //                        reason = saveHolonResult.Message;

        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {holonResult.Result.Id} and name {holonResult.Result.Name}. Reason: {reason}.");
        //                }
        //            }
        //            else
        //            {
        //                if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
        //                    reason = holonResult.Message;

        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
        //            }
        //        }
        //        else
        //            holonRepository.DeleteAsync(id).Wait();

        //       // result.Result = true;
        //        result.IsSaved = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
        //    }

        //    return result;
        //}

        //public override OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();
        //    string reason = "unknown";
        //    string softDeleting = "";

        //    if (softDelete)
        //        softDeleting = "soft";

        //    string errorMessage = $"An error occured {softDeleting} deleting the holon with providerKey {providerKey}";

        //    try
        //    {
        //        if (softDelete)
        //        {
        //            OASISResult<IHolon> holonResult = LoadHolon(providerKey);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                holonResult.Result.DeletedDate = DateTime.Now;
        //                holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
        //                OASISResult<IHolon> saveHolonResult = SaveHolon(holonResult.Result);

        //                if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
        //                {
        //                    result.Result = saveHolonResult.Result;
        //                    result.IsSaved = true;
        //                }
        //                else
        //                {
        //                    if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
        //                        reason = saveHolonResult.Message;

        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {holonResult.Result.Id} and name {holonResult.Result.Name}. Reason: {reason}.");
        //                }
        //            }
        //            else
        //            {
        //                if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
        //                    reason = holonResult.Message;

        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
        //            }
        //        }
        //        else
        //            holonRepository.DeleteAsync(providerKey).Wait();

        //        //result.Result = true;
        //        result.IsSaved = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
        //    }

        //    return result;
        //}

        //public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();
        //    string reason = "unknown";
        //    string softDeleting = "";

        //    if (softDelete)
        //        softDeleting = "soft";

        //    string errorMessage = $"An error occured {softDeleting} deleting the holon with id {id}";

        //    try
        //    {
        //        if (softDelete)
        //        {
        //            OASISResult<IHolon> holonResult = await LoadHolonAsync(id);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                holonResult.Result.DeletedDate = DateTime.Now;
        //                holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
        //                OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holonResult.Result);

        //                if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
        //                {
        //                    result.Result = saveHolonResult.Result;
        //                    result.IsSaved = true;
        //                }
        //                else
        //                {
        //                    if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
        //                        reason = saveHolonResult.Message;

        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} and name {holonResult.Result.Name}. Reason: {reason}.");
        //                }
        //            }
        //            else
        //            {
        //                if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
        //                    reason = holonResult.Message;

        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
        //            }
        //        }
        //        else
        //            await holonRepository.DeleteAsync(id);

        //        //result.Result = true;
        //        result.IsSaved = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
        //    }

        //    return result;
        //}

        //public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();
        //    string reason = "unknown";
        //    string softDeleting = "";

        //    if (softDelete)
        //        softDeleting = "soft";

        //    string errorMessage = $"An error occured {softDeleting} deleting the holon with providerKey {providerKey}";

        //    try
        //    {
        //        if (softDelete)
        //        {
        //            OASISResult<IHolon> holonResult = await LoadHolonAsync(providerKey);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                holonResult.Result.DeletedDate = DateTime.Now;
        //                holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
        //                OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holonResult.Result);

        //                if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
        //                {
        //                    result.Result = saveHolonResult.Result;
        //                    result.IsSaved = true;
        //                }
        //                else
        //                {
        //                    if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
        //                        reason = saveHolonResult.Message;

        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}, id {holonResult.Result.Id} and name {holonResult.Result.Name}. Reason: {reason}.");
        //                }
        //            }
        //            else
        //            {
        //                if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
        //                    reason = holonResult.Message;

        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
        //            }
        //        }
        //        else
        //            await holonRepository.DeleteAsync(providerKey);

        //        //result.Result = true;
        //        result.IsSaved = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
        //    }

        //    return result;
        //}

    }
}
