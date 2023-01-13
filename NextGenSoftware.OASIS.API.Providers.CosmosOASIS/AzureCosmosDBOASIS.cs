using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entites;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entities;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;

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


                //var avatarList = avatarRepository.GetListAsync();
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

                //var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
            try
            {
                holonRepository.DeleteAsync(id.ToString()).Wait();

                //var holonList = holonRepository.GetListAsync();
                //var holon = holonList.Where(a => a.HolonId == id).FirstOrDefault();
                //if (holon != null)
                //{
                //    holonRepository.DeleteAsync(holon).Wait();
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            try
            {
                holonRepository.DeleteAsync(providerKey).Wait();

                //var holonList = holonRepository.GetListAsync();
                //var holon = holonList.Where(a => a.HolonId ==new Guid(providerKey)).FirstOrDefault();
                //if (holon != null)
                //{
                //    holonRepository.DeleteAsync(holon).Wait();
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = await holonRepository.GetByIdAsync(id.ToString());

                    if (holon != null)
                    {
                        holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.AvatarId;
                        holon.DeletedDate = DateTime.Now;

                        await holonRepository.UpdateAsync(holon);
                        return new OASISResult<bool> { IsError = false, Result = true };
                    }
                    else
                        return new OASISResult<bool> { IsError = true, Result = false, Message = $"Holon was not found for id {id}" };
                }
                else
                {
                    await holonRepository.DeleteAsync(id.ToString());
                    return new OASISResult<bool> { IsError = false, Result = true };
                }

                //var holonList = holonRepository.GetListAsync();
                //var holon = holonList.Where(a => a.HolonId == id).FirstOrDefault();
                //if (holon != null)
                //{
                //    await holonRepository.DeleteAsync(holon);
                //}
                return new OASISResult<bool> { IsError = false, Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = await holonRepository.GetByIdAsync(providerKey);

                    if (holon != null)
                    {
                        holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.AvatarId;
                        holon.DeletedDate = DateTime.Now;

                        await holonRepository.UpdateAsync(holon);
                        return new OASISResult<bool> { IsError = false, Result = true };
                    }
                    else
                        return new OASISResult<bool> { IsError = true, Result = false, Message = $"Holon was not found for providerKey {providerKey}" };
                }
                else
                {
                    await holonRepository.DeleteAsync(providerKey.ToString());
                    return new OASISResult<bool> { IsError = false, Result = true };
                }

                //var holonList = holonRepository.GetListAsync();
                //var holon = holonList.Where(a => a.HolonId == new Guid(providerKey)).FirstOrDefault();
                //if (holon != null)
                //{
                //    await holonRepository.DeleteAsync(holon);
                //}
                
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Result = false, Message = ex.Message };
            }
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            try
            {
                var holonList = holonRepository.GetListAsync();
                var holonFilterd = holonList.Where(a => a.HolonType == Type).ToList();
                if (holonFilterd.Count <= 0)
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = true, Message = "Holon fetched", Result = holonFilterd };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> Import(IEnumerable<IHolon> holons)
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
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avtarFilterd = avatarList.Where(a => a.Version == version).ToList();
                if (avtarFilterd.Count <= 0)
                {
                    return new OASISResult<IEnumerable<IAvatar>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IAvatar>> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avtarFilterd };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>> { IsError = true, IsLoaded = false, Message = ex.Message};
            }
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avtarFilterd = avatarList.Where(a => a.Version == version).ToList();
                if (avtarFilterd == null)
                {
                    return new OASISResult<IEnumerable<IAvatar>> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IEnumerable<IAvatar>> { IsError = false, IsLoaded = true, Message = "Avatar fetched", Result = avtarFilterd };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetListAsync();
                var holonFiltered = holonList.Where(h => h.HolonType == type).ToList();
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
                return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = ex.Message };
            }            
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetListAsync();
                var holonFiltered = holonList.Where(h => h.HolonType == type).ToList();
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
                return new OASISResult<IEnumerable<IHolon>> { IsError = false, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avatar = avatarList.Where(a => a.AvatarId == Id).FirstOrDefault();
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

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avatar = avatarList.Where(a => a.Username == username).FirstOrDefault();
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avatar = avatarList.Where(a => a.Username == username).FirstOrDefault();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarList = avatarRepository.GetListAsync();
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
                var avatarDetList = avatarDetailRepository.GetListAsync();
                var avatar= avatarDetList.Where(a=>a.Id==id).FirstOrDefault();
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

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                var avatarDetList = avatarDetailRepository.GetListAsync();
                var avatar = avatarDetList.Where(a => a.Id == id).FirstOrDefault();
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

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarDetList = avatarDetailRepository.GetListAsync();
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
                var avatarDetList = avatarDetailRepository.GetListAsync();
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
                var avatarDetList = avatarDetailRepository.GetListAsync();
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
                var avatarDetList = avatarDetailRepository.GetListAsync();
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

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avatar = avatarList.Where(a => a.AvatarId == new Guid(providerKey)).FirstOrDefault();
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

        public async override Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            try
            {
                var avatarList = avatarRepository.GetListAsync();
                var avatar = avatarList.Where(a => a.AvatarId == new Guid(providerKey)).FirstOrDefault();
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
                var holonList = holonRepository.GetListAsync();
                var holonFiltered = holonList.Where(h => h.Id == id).FirstOrDefault();
                if (holonFiltered !=null)
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holonFiltered };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonList = holonRepository.GetListAsync();
                var holonFiltered = holonList.Where(h => h.Id == new Guid(providerKey)).FirstOrDefault();
                if (holonFiltered != null)
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holonFiltered };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holon =await holonRepository.GetByIdAsync(id.ToString());
                
                if (holon == null)
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = false, Message = "No record found" };
                }
                else
                {
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holon };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsError = true, IsLoaded = false, Message = ex.Message };
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
                    return new OASISResult<IHolon> { IsError = false, IsLoaded = true, Message = "Holons fetched", Result = holon };
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
                var holonList = holonRepository.GetListAsync();
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
                var holonList = holonRepository.GetListAsync();
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
                var holonList = holonRepository.GetListAsync();
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
                var holonList = holonRepository.GetListAsync();
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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            try
            {
                Avatar objAvatar = new Avatar();
                objAvatar.Name = Avatar.Name;
                objAvatar.FirstName = Avatar.FirstName;
                objAvatar.LastName = Avatar.LastName;
                objAvatar.Title = Avatar.Title;
                objAvatar.AvatarId = Avatar.AvatarId;

                //TODO: Where are the rest of the properties?

                avatarRepository.AddAsync(objAvatar).Wait();
                return new OASISResult<IAvatar>
                { IsSaved = true, IsError = false, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            try
            {
                Avatar objAvatar = new Avatar();
                objAvatar.Name = Avatar.Name;
                objAvatar.FirstName = Avatar.FirstName;
                objAvatar.LastName = Avatar.LastName;
                objAvatar.Title = Avatar.Title;
                objAvatar.AvatarId = Avatar.AvatarId;
                objAvatar.ProviderPrivateKey= new Dictionary<ProviderType, string>
                            { {Core.Enums.ProviderType.AzureCosmosDBOASIS,"AzureDB" } };
                objAvatar.ProviderPublicKey = new Dictionary<ProviderType, string>
                            { {Core.Enums.ProviderType.AzureCosmosDBOASIS,"AzureDB" } };
                objAvatar = await avatarRepository.AddAsync(objAvatar);
                return new OASISResult<IAvatar> 
                           { IsSaved = true,IsError=false,Result= objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsSaved = false,IsError=true,Message=ex.Message };
            }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            try
            {
                AvatarDetail objAvatar = new AvatarDetail();
                objAvatar.Name = Avatar.Name;
                objAvatar.Username = Avatar.Username;
                objAvatar.Email = Avatar.Email;
                objAvatar.Address = Avatar.Address;
                objAvatar.Country = Avatar.Country;
                avatarDetailRepository.AddAsync(objAvatar).Wait();
                return new OASISResult<IAvatarDetail>
                { IsSaved = true, IsError = false, Result = objAvatar };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail> { IsSaved = false, IsError = true, Message = ex.Message };
            }
        }

        public async override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            try
            {
                AvatarDetail objAvatar = new AvatarDetail();
                objAvatar.Name = Avatar.Name;
                objAvatar.Username = Avatar.Username;
                objAvatar.Email = Avatar.Email;
                objAvatar.Address = Avatar.Address;
                objAvatar.Country = Avatar.Country;                
                objAvatar = await avatarDetailRepository.AddAsync(objAvatar);
                return new OASISResult<IAvatarDetail>
                { IsSaved = true, IsError = false, Result = objAvatar };
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
                Holon objHolon = new Holon
                {
                    Name = holon.Name,
                    Description = holon.Description,
                };

                holonRepository.AddAsync(objHolon).Wait();
                return new OASISResult<IHolon>
                { IsSaved = true, IsError = false, Result = objHolon };
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
                Holon objHolon = new Holon
                {
                    Name = holon.Name,
                    Description = holon.Description,
                };

                objHolon = await holonRepository.AddAsync(objHolon);
                return new OASISResult<IHolon>
                { IsSaved = true, IsError = false, Result = objHolon };
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
                foreach (var holon in holons)
                {
                    Holon objHolon = new Holon
                    {
                        Name = holon.Name,
                        Description = holon.Description,
                    };
                    holonRepository.AddAsync(objHolon).Wait();
                }               

                
                return new OASISResult<IEnumerable<IHolon>>
                { IsSaved = true, IsError = false, Result = holons };
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
                if (holons != null)
                {
                    foreach (var holon in holons)
                    {
                        Holon objHolon = new Holon
                        {
                            Name = holon.Name,
                            Description = holon.Description,
                        };

                        objHolon = await holonRepository.AddAsync(objHolon);
                    }
                }
                
                return new OASISResult<IEnumerable<IHolon>>
                { IsSaved = true, IsError = false, Result = holons };
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
    }
}
