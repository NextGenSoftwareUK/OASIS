﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using DataHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    //TODO: Implement OASISResult properly on below methods! :)
    public class MongoDBOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        public MongoDbContext Database { get; set; }
        private AvatarRepository _avatarRepository = null;
        private HolonRepository _holonRepository = null;
        private SearchRepository _searchRepository = null;

        public string ConnectionString { get; set; }
        public string DBName { get; set; }
        public bool IsVersionControlEnabled { get; set; }

        public MongoDBOASIS(string connectionString, string dbName) : base()
        {
            Init(connectionString, dbName);
        }

        //TODO: Need to implement these OASISDNA overloads for ALL Providers ASAP! :)
        public MongoDBOASIS(string connectionString, string dbName, OASISDNA OASISDNA, string OASISDNAPath = "OASIS_DNA.json") : base(OASISDNA, OASISDNAPath)
        {
            Init(connectionString, dbName);
        }

        public MongoDBOASIS(string connectionString, string dbName, OASISDNA OASISDNA) : base(OASISDNA)
        {
            Init(connectionString, dbName);
        }

        public MongoDBOASIS(string connectionString, string dbName, string OASISDNAPath = "OASIS_DNA.json") : base (OASISDNAPath)
        {
            Init(connectionString, dbName);
        }

        private void Init(string connectionString, string dbName)
        {
            ConnectionString = connectionString;
            DBName = dbName;

            this.ProviderName = "MongoDBOASIS";
            this.ProviderDescription = "MongoDB Atlas Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("NextGenSoftware") || type.FullName.StartsWith("System")); 
            BsonSerializer.RegisterSerializer(objectSerializer);
            //BsonClassMap.RegisterClassMap<OAPPDNA>();

            /*
            ConventionRegistry.Register(
                   "DictionaryRepresentationConvention",
                   new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) },
                   _ => true);*/
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database == null)
                {
                    Database = new MongoDbContext(ConnectionString, DBName);
                    _avatarRepository = new AvatarRepository(Database);
                    _holonRepository = new HolonRepository(Database);
                    _searchRepository = new SearchRepository(Database);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex) 
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.ActivateProvider. Reason: {ex}");
            }
            
            return result;
            //return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                //TODO: {URGENT} Disconnect, Dispose and release resources here.
                if (Database != null)
                {
                    Database.MongoDB = null;
                    Database.MongoClient = null;
                    Database = null;
                }

                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.DeActivateProvider. Reason: {ex}");
            }

            return result;
            //return base.DeActivateProvider();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database == null)
                {
                    Database = new MongoDbContext(ConnectionString, DBName);
                    _avatarRepository = new AvatarRepository(Database);
                    _holonRepository = new HolonRepository(Database);
                    _searchRepository = new SearchRepository(Database);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.ActivateProviderAsync. Reason: {ex}");
            }

            return result;
            //return await base.ActivateProviderAsync();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                //TODO: {URGENT} Disconnect, Dispose and release resources here.
                if (Database != null)
                {
                    Database.MongoDB = null;
                    Database.MongoClient = null;
                    Database = null;
                }

                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.DeActivateProviderAsync. Reason: {ex}");
            }

            return result;
            //return await base.DeActivateProviderAsync();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatars(await _avatarRepository.GetAvatarsAsync());
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatars(_avatarRepository.GetAvatars());
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.Email == avatarEmail));
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.Username == avatarUsername));
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        //{
        //    return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(username));
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.Username == avatarUsername));
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        //{
        //    return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username));
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(Id));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.Email == avatarEmail));
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(Id));
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(username, password));
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    return new OASISResult<IAvatar>(ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username, password)));
        //}

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon ?
               await _avatarRepository.AddAsync(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)) :
               await _avatarRepository.UpdateAsync(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)));
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(avatar.IsNewHolon ?
               _avatarRepository.Add(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)) :
               _avatarRepository.Update(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)));
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(avatar.IsNewHolon ?
               await _avatarRepository.AddAsync(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)) :
               await _avatarRepository.UpdateAsync(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)));
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon ?
                _avatarRepository.Add(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)) :
                _avatarRepository.Update(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)));
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return _avatarRepository.Delete(x => x.Username == avatarUsername, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(id);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(x => x.Email == avatarEmail, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(x => x.Username == avatarUsername, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return _avatarRepository.Delete(id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return _avatarRepository.Delete(x => x.Email == avatarEmail, softDelete);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(providerKey));
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(providerKey));
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _avatarRepository.Delete(providerKey, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(providerKey, softDelete);
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await _searchRepository.SearchAsync(searchParams);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return _searchRepository.Search(searchParams);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.Username == avatarUsername));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(id));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(x => x.Username == avatarUsername));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(x => x.Email == avatarEmail));
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatarDetails(await _avatarRepository.GetAvatarDetailsAsync());
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(id));
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.Email == avatarEmail));
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatarDetails(_avatarRepository.GetAvatarDetails());
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(await _holonRepository.GetHolonAsync(id))).Result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(id))).Result);
        }


        //public override T LoadHolon<T>(Guid id)
        //{
        //    return ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(id))).Result;
        //}

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(await _holonRepository.GetHolonAsync(providerKey)), loadChildrenFromProvider).Result);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(providerKey)), loadChildrenFromProvider).Result);
        }

        //public override async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //TODO: Finish implementing OASISResult properly...
        //    return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(await _holonRepository.GetHolonByCustomKeyAsync(customKey))).Result);
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //TODO: Finish implementing OASISResult properly...
        //    return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolonByCustomKey(customKey))).Result);
        //}

        //public override async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //TODO: Finish implementing OASISResult properly...
        //    return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(await _holonRepository.GetHolonByMetaDataAsync(metaKey, metaValue))).Result);
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //TODO: Finish implementing OASISResult properly...
        //    return new OASISResult<IHolon>(DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolonByMetaData(metaKey, metaValue))).Result);
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(await _holonRepository.GetAllHolonsForParentAsync(id, type), loadChildrenFromProvider));
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsForParent(id, type), loadChildrenFromProvider));
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = await _holonRepository.GetAllHolonsForParentAsync(providerKey, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider);

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsForParent(providerKey, type), loadChildrenFromProvider));
        }

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsByCustomKey(customKey, type), loadChildrenFromProvider));
        //}

        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
        //    OASISResult<IEnumerable<Holon>> repoResult = await _holonRepository.GetAllHolonsByCustomKeyAsync(customKey, type);

        //    if (repoResult.IsError)
        //    {
        //        result.IsError = true;
        //        result.Message = repoResult.Message;
        //    }
        //    else
        //        result.Result = DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider);

        //    return result;
        //}

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetHolonsByMetaData(metaKey, metaValue, type), loadChildrenFromProvider));
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = _holonRepository.GetHolonsByMetaData(metaKey, metaValue, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider);

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = await _holonRepository.GetHolonsByMetaDataAsync(metaKey, metaValue, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider);

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetHolonsByMetaData(metaKeyValuePairs, type), loadChildrenFromProvider));

            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = _holonRepository.GetHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider);

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = await _holonRepository.GetHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider);

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(await _holonRepository.GetAllHolonsAsync(type), loadChildrenFromProvider));
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolons(type), loadChildrenFromProvider));
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = !holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS)
                ? DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.AddAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider)
                : DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.UpdateAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider);

            //OASISResult<IHolon> result =  holon.IsNewHolon
            //    ? DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.AddAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)))
            //    : DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.UpdateAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)));

            if (!result.IsError && result.Result != null && saveChildren && saveChildrenOnProvider && result.Result.Children != null && result.Result.Children.Count() > 0)
            {
                OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(result.Result.Children);

                if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                    result.Result.Children = saveChildrenResult.Result.ToList();
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}";
                }
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = holon.IsNewHolon
                ? DataHelper.ConvertMongoEntityToOASISHolon(_holonRepository.Add(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider)
                : DataHelper.ConvertMongoEntityToOASISHolon(_holonRepository.Update(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider);

            if (!result.IsError && result.Result != null && saveChildren && result.Result.Children != null && result.Result.Children.Count() > 0)
            {
                OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(result.Result.Children);

                if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                    result.Result.Children = saveChildrenResult.Result.ToList();
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}";
                }
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            if (holons == null)
            {
                result.Message = "Holons is null";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            if (holons.Count() == 0)
            {
                result.Message = "Holons collection is empty.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = SaveHolon(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildren && saveChildrenOnProvider && holonResult.Result.Children != null && holonResult.Result.Children.Count() > 0)
                    {
                        //TODO: Need to add recursive code like HolonManager has...
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(holonResult.Result.Children, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result.ToList();
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            result.Result = savedHolons.ToList();

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the MongoDBOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            if (holons == null)
            {
                result.Message = "Holons is null";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            if (holons.Count() == 0)
            {
                result.Message = "Holons collection is empty.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = await SaveHolonAsync(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildren && saveChildrenOnProvider && holonResult.Result.Children != null && holonResult.Result.Children.Count() > 0)
                    {
                        //TODO: Need to add recursive code like HolonManager has...
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = await SaveHolonsAsync(holonResult.Result.Children);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result.ToList();
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            result.Result = savedHolons.ToList();

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the SQLLiteDBOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return _holonRepository.Delete(id);
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            return await _holonRepository.DeleteAsync(id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return _holonRepository.Delete(providerKey);
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            return await _holonRepository.DeleteAsync(providerKey);
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }


        //IOASISSuperStar Interface Implementation

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }
    }
}