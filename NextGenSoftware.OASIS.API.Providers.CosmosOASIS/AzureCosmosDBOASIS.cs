using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

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
                dbClientFactory.EnsureDbSetupAsync().Wait();

                avatarRepository = new AvatarRepository(dbClientFactory);
                holonRepository = new HolonRepository(dbClientFactory);
                avatarDetailRepository = new AvatarDetailRepository(dbClientFactory);
            }

            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            dbClientFactory = null;
            avatarRepository = null;
            avatarDetailRepository = null;
            holonRepository = null;
            return base.DeActivateProvider();
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            try
            {
                avatarRepository.DeleteAsync(id.ToString()).Wait();


                //var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.AvatarId == id).FirstOrDefault();
                //if (avatar != null)
                //{
                //    avatarRepository.DeleteAsync(avatar).Wait();
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            try
            {
                //Normally the providerKey is different to the Id but in this case they are the same since Azure uses GUID's the same as the OASIS does for ID.
                avatarRepository.DeleteAsync(providerKey).Wait();

                //var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.AvatarId == new Guid(providerKey)).FirstOrDefault();
                //if (avatar != null)
                //{
                //    avatarRepository.DeleteAsync(avatar).Wait();
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            try
            {
                await avatarRepository.DeleteAsync(id.ToString());

                //var avatar =await avatarRepository.GetByIdAsync(id.ToString());
                //if (avatar != null)
                //{                    
                //    await avatarRepository.DeleteAsync(avatar);
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex) {
                return new OASISResult<bool> { IsError = true, Result = false,Message=ex.Message };
            }
        }

        public async override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                //Normally the providerKey is different to the Id but in this case they are the same since Azure uses GUID's the same as the OASIS does for ID.
                await avatarRepository.DeleteAsync(providerKey);

                //var avatar = await avatarRepository.GetByIdAsync(providerKey);
                //if (avatar != null)
                //{
                //    await avatarRepository.DeleteAsync(avatar);
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            try
            {
                //TODO: May want to cache this in future?
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Email == avatarEmail).FirstOrDefault();
                if (avatar != null)
                {
                    avatarRepository.DeleteAsync(avatar).Wait();
                }
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            try
            {
                //TODO: May want to cache this in future?
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Email == avatarEmail).FirstOrDefault();
                if (avatar != null)
                {
                    await avatarRepository.DeleteAsync(avatar);
                }
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            try
            {
                //TODO: May want to cache this in future?
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Username == avatarUsername).FirstOrDefault();
                if (avatar != null)
                {
                    avatarRepository.DeleteAsync(avatar).Wait();
                }
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            try
            {
                //TODO: May want to cache this in future?
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Username == avatarUsername).FirstOrDefault();
                if (avatar != null)
                {
                    await avatarRepository.DeleteAsync(avatar);
                }
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the holon with id {id}";

            try
            {
                if (softDelete)
                {
                    OASISResult<IHolon> holonResult = LoadHolon(id);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        holonResult.Result.DeletedDate = DateTime.Now;
                        holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IHolon> saveHolonResult = SaveHolon(holonResult.Result);

                        if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
                                reason = saveHolonResult.Message;

                            ErrorHandling.HandleError(ref result, $"{errorMessage}, id {holonResult.Result.Id} and name {holonResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
                            reason = holonResult.Message;

                        ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    holonRepository.DeleteAsync(id).Wait();

                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the holon with providerKey {providerKey}";
            
            try
            {
                if (softDelete)
                {
                    OASISResult<IHolon> holonResult = LoadHolon(providerKey);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        holonResult.Result.DeletedDate = DateTime.Now;
                        holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IHolon> saveHolonResult = SaveHolon(holonResult.Result);

                        if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
                                reason = saveHolonResult.Message;

                            ErrorHandling.HandleError(ref result, $"{errorMessage}, id {holonResult.Result.Id} and name {holonResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
                            reason = holonResult.Message;

                        ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    holonRepository.DeleteAsync(providerKey).Wait();

                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the holon with id {id}";

            try
            {
                if (softDelete)
                {
                    OASISResult<IHolon> holonResult = await LoadHolonAsync(id);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        holonResult.Result.DeletedDate = DateTime.Now;
                        holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holonResult.Result);

                        if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
                                reason = saveHolonResult.Message;

                            ErrorHandling.HandleError(ref result, $"{errorMessage} and name {holonResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
                            reason = holonResult.Message;

                        ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    await holonRepository.DeleteAsync(id);

                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            string reason = "unknown";
            string softDeleting = "";

            if (softDelete)
                softDeleting = "soft";

            string errorMessage = $"An error occured {softDeleting} deleting the holon with providerKey {providerKey}";

            try
            {
                if (softDelete)
                {
                    OASISResult<IHolon> holonResult = await LoadHolonAsync(providerKey);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        holonResult.Result.DeletedDate = DateTime.Now;
                        holonResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                        OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holonResult.Result);

                        if (saveHolonResult != null && !saveHolonResult.IsError && saveHolonResult.Result != null)
                        {
                            result.Result = true;
                            result.IsSaved = true;
                        }
                        else
                        {
                            if (saveHolonResult != null && !string.IsNullOrEmpty(saveHolonResult.Message))
                                reason = saveHolonResult.Message;

                            ErrorHandling.HandleError(ref result, $"{errorMessage}, id {holonResult.Result.Id} and name {holonResult.Result.Name}. Reason: {reason}.");
                        }
                    }
                    else
                    {
                        if (holonResult != null && !string.IsNullOrEmpty(holonResult.Message))
                            reason = holonResult.Message;

                        ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {reason}.");
                    }
                }
                else
                    await holonRepository.DeleteAsync(providerKey);

                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            string errorMessage = "Error occured in LoadAllAvatarsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarList = avatarRepository.GetList();

                if (avatarList == null)
                    ErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
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
                ErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return LoadAllAvatars(version);
        }

        //public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, int version = 0)
        {
            return LoadAllHolonsAsync(type, version).Result;
        }

        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, int version = 0)
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
                //        OASISResult<IEnumerable<IHolon>> holonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);

                //        if (!holonsResult.IsError && holonsResult.Result != null)
                //            allHolonsToReturn.AddRange(holonsResult.Result);
                //    }
                //}

                if (holonList.Count <= 0)
                    ErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    result.Result = holonsFiltered;
                    result.IsLoaded = true;
                    result.Message = "Holons fetched";
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            try
            {
                IAvatar avatar = avatarRepository.GetByIdAsync(id.ToString()).Result;

                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            try
            {
                var avatar = await avatarRepository.GetByIdAsync(Id.ToString());
                //var avatar=avatarList.Where(a=>a.AvatarId==Id).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Email == avatarEmail).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Email == avatarEmail).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Username == avatarUsername).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetList();
                var avatar = avatarList.Where(a => a.Username == avatarUsername).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            try
            {
                IAvatarDetail avatarDetail = avatarDetailRepository.GetByIdAsync(id.ToString()).Result;

                if (avatarDetail == null)
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = true, Message = "Avatar Detail fetched", Result = avatarDetail };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                IAvatarDetail avatarDetail = avatarDetailRepository.GetByIdAsync(id.ToString()).Result;

                if (avatarDetail == null)
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = true, Message = "Avatar Detail fetched", Result = avatarDetail };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarDetList = avatarDetailRepository.GetList();
                var avatar = avatarDetList.Where(a => a.Email == avatarEmail).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = true, Message = "Avatar Detail fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarDetList = avatarDetailRepository.GetList();
                var avatar = avatarDetList.Where(a => a.Email == avatarEmail).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = true, Message = "Avatar Detail fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarDetList = avatarDetailRepository.GetList();
                var avatar = avatarDetList.Where(a => a.Username == avatarUsername).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = true, Message = "Avatar Detail fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarDetList = avatarDetailRepository.GetList();
                var avatar = avatarDetList.Where(a => a.Username == avatarUsername).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatarDetail> { IsError = false, IsLoaded = true, Message = "Avatar Detail fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.Id == new Guid(providerKey)).FirstOrDefault(); //The ID and ProviderUniqueStorageKey are the same for Azure because Azure uses GUID for ID's like OASIS does.
                var avatar = avatarList.Where(a => a.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] == providerKey).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.Id == new Guid(providerKey)).FirstOrDefault(); //The ID and ProviderUniqueStorageKey are the same for Azure because Azure uses GUID for ID's like OASIS does.
                var avatar = avatarList.Where(a => a.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] == providerKey).FirstOrDefault();
                if (avatar == null)
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IAvatar> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avatar };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holon = holonRepository.GetByIdAsync(id.ToString()).Result;

                if (holon == null)
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found in LoadHolonAsync method in AzureCOSMOSDBOASIS." };
                }
                else
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holon fetched in LoadHolonAsync method in AzureCOSMOSDBOASIS.", Result = holon };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = $"Error occured in LoadHolonAsync method in AzureCOSMOSDBOASIS loading holon. Reason: {ex.Message }." };
            }
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holon = holonRepository.GetByIdAsync(providerKey).Result;

                if (holon != null)
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holon fetched in LoadHolon method in AzureCOSMOSDBOASIS.", Result = holon };
                else
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found in LoadHolon method in AzureCOSMOSDBOASIS." };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = $"Error occured in LoadHolon method in AzureCOSMOSDBOASIS loading holon. Reason: {ex.Message }." };
            }
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holon = await holonRepository.GetByIdAsync(id.ToString());
                
                if (holon == null)
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found in LoadHolonAsync method in AzureCOSMOSDBOASIS." };
                }
                else
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holon fetched in LoadHolonAsync method in AzureCOSMOSDBOASIS.", Result = holon };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = $"Error occured in LoadHolonAsync method in AzureCOSMOSDBOASIS loading holon. Reason: {ex.Message }." };
            }
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holon = await holonRepository.GetByIdAsync(providerKey);

                if (holon == null)
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holon fetched", Result = holon };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetList();
                var holonFiltered = holonList.Where(h => h.HolonType == type && h.ParentHolonId==id).ToList();
                if (holonList.Count <= 0)
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holonFiltered };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetList();
                var holonFiltered = holonList.Where(h => h.HolonType == type && h.ParentHolonId == new Guid(providerKey)).ToList();
                if (holonList.Count <= 0)
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holonFiltered };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetList();
                var holonFiltered = holonList.Where(h => h.HolonType == type && h.ParentHolonId == id).ToList();
                if (holonList.Count <= 0)
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holonFiltered };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetList();
                var holonFiltered = holonList.Where(h => h.HolonType == type && h.ParentHolonId == new Guid(providerKey)).ToList();
                if (holonList.Count <= 0)
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holonFiltered };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
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

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }
    }
}
