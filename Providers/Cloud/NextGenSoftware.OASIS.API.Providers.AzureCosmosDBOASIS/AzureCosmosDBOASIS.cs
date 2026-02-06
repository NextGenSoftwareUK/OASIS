using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
    public class AzureCosmosDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly Uri serviceEndpoint;
        private readonly string authKey;
        private readonly string databaseName;
        private readonly List<string> collectionNames;
        private CosmosDbClientFactory dbClientFactory;
        private IAvatarRepository avatarRepository;
        private IAvatarDetailRepository avatarDetailRepository;
        private IHolonRepository holonRepository;

        public AzureCosmosDBOASIS(Uri serviceEndpoint, string authKey, string databaseName, List<string> collectionNames)
        {
            this.ProviderName = "AzureCosmosDBOASIS";
            this.ProviderDescription = "Microsoft Azure Cosmos DB Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureCosmosDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.serviceEndpoint = serviceEndpoint;
            this.authKey = authKey;
            this.databaseName = databaseName;
            this.collectionNames = collectionNames;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in AzureCosmosDBOASIS Provider. Reason:";

            try
            {
                if (dbClientFactory == null)
                {
                    var documentClient = new DocumentClient(serviceEndpoint, authKey, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });

                    documentClient.OpenAsync().Wait();

                    dbClientFactory = new CosmosDbClientFactory(databaseName, collectionNames, documentClient);
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
                    var documentClient = new DocumentClient(serviceEndpoint, authKey, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });

                    await documentClient.OpenAsync();

                    dbClientFactory = new CosmosDbClientFactory(databaseName, collectionNames, documentClient);
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

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with id {id}";

            try
            {
                holonRepository.DeleteAsync(id).Wait();
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with providerKey {providerKey}";

            try
            {
                holonRepository.DeleteAsync(providerKey).Wait();
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with id {id}";

            try
            {
                await holonRepository.DeleteAsync(id);
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with providerKey {providerKey}";

            try
            {
                await holonRepository.DeleteAsync(providerKey);
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            //TODO HB: implement...
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = "Error occured in LoadAllAvatarDetails method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarDetailsList = avatarDetailRepository.GetList();

                if (avatarDetailsList == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    if (version > 0)
                        avatarDetailsList = avatarDetailsList.Where(a => a.Version == version).ToList();

                    result.Result = avatarDetailsList;
                    result.IsLoaded = true;
                    result.Message = "Avatar details fetched";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = "Error occured in LoadAllAvatarDetailsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarDetailsList = avatarDetailRepository.GetList();

                if (avatarDetailsList == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    if (version > 0)
                        avatarDetailsList = avatarDetailsList.Where(a => a.Version == version).ToList();

                    result.Result = avatarDetailsList;
                    result.IsLoaded = true;
                    result.Message = "Avatar details fetched asynchronously";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            string errorMessage = "Error occured in LoadAllAvatarsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarList = avatarRepository.GetList();

                if (avatarList == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    if (version > 0)
                        avatarList = avatarList.Where(a => a.Version == version).ToList();

                    result.Result = avatarList;
                    result.IsLoaded = true;
                    result.Message = "Avatars fetched";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return LoadAllAvatars(version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, int version = 0)
        {
            //return LoadAllHolonsAsync(type, version).Result;
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error occured in LoadAllHolonsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                List<IHolon> allHolonsToReturn = new List<IHolon>();
                List<IHolon> holonList = holonRepository.GetList();
                IEnumerable<IHolon> holonsFiltered = null;

                if (version > 0)
                    holonsFiltered = holonList.Where(h => h.HolonType == type && h.Version == version).ToList();
                else
                    holonsFiltered = holonList.Where(h => h.HolonType == type).ToList();

                //TODO: Implement loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError params...
                //if (loadChildren)
                //{
                //    foreach (IHolon holon in holonsFiltered)
                //    {
                //        //TODO: NEED TO COME BACK TO THIS!
                //        //THIS METHOD WILL CALL LOADALLHOLONS ON HOLON WHICH COULD CAUSE INFINITE RECURSION AND STACK OVERFLOW! ;-)
                //        //PLUS NEED TO THINK ABOUT CACHING AND EFFICIENCY ETC... LOTS TO THINK ABOUT AND DONT HAVE TIME FOR THIS JUST YET! ;-)
                //        holon.LoadChildHolons(type, recursive, maxChildDepth, continueOnError, true);
                //    }
                //}

                if (holonList.Count <= 0)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    result.Result = holonsFiltered;
                    result.IsLoaded = true;
                    result.Message = "Holons fetched";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatar avatar = avatarRepository.GetByIdAsync(id.ToString()).Result;

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatar method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatar method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatar method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                var avatar = await avatarRepository.GetByIdAsync(Id.ToString());
                
                if (avatar == null)
                {
                    result.Message = "No avatars found in LoadAvatarAsync method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarAsync method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatar method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                //TODO: Need to test to make sure this works!
                IAvatar avatar = avatarRepository.GetByField("Email", avatarEmail, version);

                //var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.Email == avatarEmail).FirstOrDefault();

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatarByEmail method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByEmail method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatar method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                //TODO: Need to test to make sure this works!
                IAvatar avatar = avatarRepository.GetByField("Email", avatarEmail, version);                

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatarByEmailAsync method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByEmailAsync method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarByEmailAsync method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                //TODO: Need to test to make sure this works!
                IAvatar avatar = avatarRepository.GetByField("UserName", avatarUsername, version);

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatarByUsername method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByUsername method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarByUsername method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsername(avatarUsername,version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatarDetail avatarDetail =await avatarDetailRepository.GetByIdAsync(id.ToString());

                if (avatarDetail == null)
                {
                    result.Message = "No AvatarDetails found in LoadAvatarDetailAsync method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatarDetail;
                    result.Message = "AvatarDetails fetched in LoadAvatarDetailAsync method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarDetailAsync method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatarDetail avatarDetail = avatarDetailRepository.GetByField("Email",avatarEmail, version);

                if (avatarDetail == null)
                {
                    result.Message = "No AvatarDetails found in LoadAvatarDetailByEmail method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatarDetail;
                    result.Message = "AvatarDetails fetched in LoadAvatarDetailByEmail method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarDetailByEmail method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmail(avatarEmail, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatarDetail avatarDetail = avatarDetailRepository.GetByField("UserName", avatarUsername, version);

                if (avatarDetail == null)
                {
                    result.Message = "No AvatarDetails found in LoadAvatarDetailByUsername method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatarDetail;
                    result.Message = "AvatarDetails fetched in LoadAvatarDetailByUsername method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarDetailByUsername method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsername(avatarUsername, version);
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                IAvatar avatar = avatarRepository.GetByField("Id", providerKey, version);

                //var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.Id == new Guid(providerKey)).FirstOrDefault(); //The ID and ProviderUniqueStorageKey are the same for Azure because Azure uses GUID for ID's like OASIS does.
                //var avatar = avatarList.Where(a => a.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] == providerKey).FirstOrDefault();
                
                if (avatar == null)
                    result.Message = "No record found in LoadAvatarByProviderKey method in AzureCosmosDbOASIS Provider.";
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByProviderKey method in AzureCosmosDbOASIS Provider.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarByProviderKey method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }

            return result;
        }

        public async override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKey(providerKey, version);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolon(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                var holon = holonRepository.GetByIdAsync(providerKey).Result;

                if (holon == null)
                    result.Message = "No holons found in LoadHolon method in AzureCOSMOSDBOASIS.";
                else
                {
                    result.Result = holon;
                    result.Message = "Holon fetched in LoadHolon method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadHolon method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }

            return result;
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolon(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider,version);
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParent(id.ToString(), type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var holonList = holonRepository.GetList();
                var holonFiltered = holonList.Where(h => h.HolonType == type && h.ParentHolonId.ToString() == providerKey).ToList();

                if (holonList.Count <= 0)
                    result.Message = "No holons found in LoadHolonsForParent method in AzureCOSMOSDBOASIS.";
                else
                {
                    result.Result = holonFiltered;
                    result.Message = "Holons fetched in LoadHolonsForParent method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadHolonsForParent method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }

            return result;
        }

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParent(id.ToString(), type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            try
            {
                IAvatar objAvatar = avatarRepository.AddAsync(avatar).Result;
                return new OASISResult<IAvatar> { IsSaved = true, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            try
            {
                IAvatar objAvatar = await avatarRepository.AddAsync(avatar);
                return new OASISResult<IAvatar> { IsSaved = true, IsError = false, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsSaved = false, IsError=true, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            try
            {
                IAvatarDetail objAvatar = avatarDetailRepository.AddAsync(avatarDetail).Result;
                return new OASISResult<IAvatarDetail> { IsSaved = true, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            try
            {
                IAvatarDetail objAvatar = await avatarDetailRepository.AddAsync(avatarDetail);
                return new OASISResult<IAvatarDetail> { IsSaved = true, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                IHolon objHolon = holonRepository.AddAsync(holon).Result;
                return new OASISResult<IHolon> { IsSaved = true, Result = objHolon };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                IHolon objHolon = await holonRepository.AddAsync(holon);
                return new OASISResult<IHolon> { IsSaved = true, Result = objHolon };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                List<IHolon> savedHolons = new List<IHolon>();

                if (holons != null)
                {
                    foreach (var holon in holons)
                        savedHolons.Add(holonRepository.AddAsync(holon).Result);
                }

                return new OASISResult<IEnumerable<IHolon>> { IsSaved = true, IsError = false, Result = savedHolons };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                List<IHolon> savedHolons = new List<IHolon>();

                if (holons != null)
                {
                    foreach (var holon in holons)
                        savedHolons.Add(await holonRepository.AddAsync(holon));
                }
                
                return new OASISResult<IEnumerable<IHolon>> { IsSaved = true, IsError = false, Result = savedHolons };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                // Extract search query from SearchGroups
                string searchQuery = null;
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.First();
                    if (firstGroup is ISearchTextGroup textGroup)
                        searchQuery = textGroup.SearchQuery;
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Search holons - using synchronous method or basic filtering
                    var holonSearchResult = holonRepository.GetList().Where(h => h.Name.Contains(searchQuery) || h.Description.Contains(searchQuery));
                    holons.AddRange(holonSearchResult);

                    // Search avatars - using synchronous method or basic filtering
                    var avatarSearchResult = avatarRepository.GetList().Where(a => a.Username.Contains(searchQuery) || a.Email.Contains(searchQuery));
                    avatars.AddRange(avatarSearchResult);
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in Azure Cosmos DB with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    try
                    {
                        await holonRepository.AddAsync(holon);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {ex.Message}");
                        return result;
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar ID
                var holons = holonRepository.GetList().Where(h => h.CreatedByAvatarId == avatarId);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons for avatar {avatarId} from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by ID from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar username
                var holons = holonRepository.GetList().Where(h => h.CreatedByAvatar.Username == avatarUsername);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons for avatar {avatarUsername} from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar email
                var holons = holonRepository.GetList().Where(h => h.CreatedByAvatar.Email == avatarEmailAddress);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons for avatar {avatarEmailAddress} from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons
                var holons = holonRepository.GetList();
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count()} holons from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by metadata from Azure Cosmos DB
                var holons = holonRepository.GetList().Where(h => h.MetaData != null && h.MetaData.ContainsKey(metaKey) && h.MetaData[metaKey].ToString() == metaValue);

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count()} holons by metadata from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by multiple metadata pairs from Azure Cosmos DB
                var holons = holonRepository.GetList().Where(h =>
                {
                    if (h.MetaData == null) return false;
                    foreach (var kvp in metaKeyValuePairs)
                    {
                        if (!h.MetaData.ContainsKey(kvp.Key) || h.MetaData[kvp.Key].ToString() != kvp.Value)
                            return false;
                    }
                    return true;
                });

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count()} holons by metadata pairs from Azure Cosmos DB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }
    }
}
