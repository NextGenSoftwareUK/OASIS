using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using DataHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {

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
            var result = new OASISResult<IHolon>();
            try
            {
                var holonResult = await _holonRepository.GetHolonAsync(id);
                if (holonResult != null)
                {
                    result.Result = DataHelper.ConvertMongoEntityToOASISHolon(holonResult);
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from MongoDB";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in MongoDB database");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from MongoDB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonResult = _holonRepository.GetHolon(id);
                if (holonResult != null)
                {
                    result.Result = DataHelper.ConvertMongoEntityToOASISHolon(holonResult);
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from MongoDB";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in MongoDB database");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from MongoDB: {ex.Message}", ex);
            }
            return result;
        }


        //public override T LoadHolon<T>(Guid id)
        //{
        //    return ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(id))).Result;
        //}

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonResult = await _holonRepository.GetHolonAsync(providerKey);
                if (holonResult != null)
                {
                    result.Result = DataHelper.ConvertMongoEntityToOASISHolon(holonResult, loadChildrenFromProvider);
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from MongoDB by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in MongoDB database by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from MongoDB by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonResult = _holonRepository.GetHolon(providerKey);
                if (holonResult != null)
                {
                    result.Result = DataHelper.ConvertMongoEntityToOASISHolon(holonResult, loadChildrenFromProvider);
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from MongoDB by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in MongoDB database by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from MongoDB by provider key: {ex.Message}", ex);
            }
            return result;
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

            if (_holonRepository == null)
            {
                result.IsError = true;
                result.Message = "MongoDBOASIS provider is not fully initialised (holonRepository is null). Provider may be activating or was concurrently deactivated — OASIS Hyperdrive will retry.";
                return result;
            }

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

            if (_holonRepository == null)
            {
                result.IsError = true;
                result.Message = "MongoDBOASIS provider is not fully initialised (holonRepository is null). Provider may be activating or was concurrently deactivated — OASIS Hyperdrive will retry.";
                return result;
            }

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

        async Task<OASISResult<IEnumerable<IHolon>>> IOASISStorageProvider.LoadAllHolonsAsync(Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(await _holonRepository.GetAllHolonsAsync(avatarId, includePublic, type), loadChildrenFromProvider));
        }

        OASISResult<IEnumerable<IHolon>> IOASISStorageProvider.LoadAllHolons(Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolons(avatarId, includePublic, type), loadChildrenFromProvider));
        }

        async Task<OASISResult<IEnumerable<IHolon>>> IOASISStorageProvider.LoadHolonsForParentAsync(Guid id, Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(await _holonRepository.GetAllHolonsForParentAsync(id, avatarId, includePublic, type), loadChildrenFromProvider));
        }

        OASISResult<IEnumerable<IHolon>> IOASISStorageProvider.LoadHolonsForParent(Guid id, Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsForParent(id, avatarId, includePublic, type), loadChildrenFromProvider));
        }

        async Task<OASISResult<IEnumerable<IHolon>>> IOASISStorageProvider.LoadHolonsByMetaDataAsync(string metaKey, string metaValue, Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            var repoResult = await _holonRepository.GetHolonsByMetaDataAsync(metaKey, metaValue, avatarId, includePublic, type);
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider));
        }

        OASISResult<IEnumerable<IHolon>> IOASISStorageProvider.LoadHolonsByMetaData(string metaKey, string metaValue, Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            var repoResult = _holonRepository.GetHolonsByMetaData(metaKey, metaValue, avatarId, includePublic, type);
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider));
        }

        async Task<OASISResult<IEnumerable<IHolon>>> IOASISStorageProvider.LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            var repoResult = await _holonRepository.GetHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, avatarId, includePublic, type);
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider));
        }

        OASISResult<IEnumerable<IHolon>> IOASISStorageProvider.LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, Guid avatarId, bool includePublic, HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, bool continueOnError, bool loadChildrenFromProvider, int version)
        {
            var repoResult = _holonRepository.GetHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, avatarId, includePublic, type);
            return new OASISResult<IEnumerable<IHolon>>(DataHelper.ConvertMongoEntitysToOASISHolons(repoResult.Result, loadChildrenFromProvider));
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            // Holon.Id is the public, provider-independent OASIS identity. MongoDB's ObjectId is
            // intentionally private and therefore is not present in REST, JavaScript/NPM, Unity,
            // or native update payloads. Resolve the operation from the persisted public identity
            // itself: update the matching document, otherwise create the first document for it.
            // Do not use CreatedDate or IsNewHolon here: both are lifecycle/audit state and are not
            // a durable cross-client storage key.
            var mongoHolon = DataHelper.ConvertOASISHolonToMongoEntity(holon);
            var persistedHolon = await _holonRepository.GetHolonAsync(mongoHolon.HolonId);
            PreserveCreationAuditFields(mongoHolon, persistedHolon);
            var saveResult = persistedHolon == null
                ? await _holonRepository.AddAsync(mongoHolon)
                : await _holonRepository.UpdateAsync(mongoHolon);
            OASISResult<IHolon> result = DataHelper.ConvertMongoEntityToOASISHolon(saveResult, saveChildrenOnProvider);

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
            var mongoHolon = DataHelper.ConvertOASISHolonToMongoEntity(holon);
            var persistedHolon = _holonRepository.GetHolon(mongoHolon.HolonId);
            PreserveCreationAuditFields(mongoHolon, persistedHolon);
            var saveResult = persistedHolon == null
                ? _holonRepository.Add(mongoHolon)
                : _holonRepository.Update(mongoHolon);
            OASISResult<IHolon> result = DataHelper.ConvertMongoEntityToOASISHolon(saveResult, saveChildrenOnProvider);

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

        private static void PreserveCreationAuditFields(Holon holon, Holon persistedHolon)
        {
            if (persistedHolon != null)
            {
                // Creation audit data is immutable. A stateless update often has none of it,
                // so preserve the persisted values before ReplaceOne performs a full document
                // replacement.
                holon.Id = persistedHolon.Id;
                holon.CreatedDate = persistedHolon.CreatedDate;
                holon.CreatedByAvatarId = persistedHolon.CreatedByAvatarId;
                holon.CreatedProviderType = persistedHolon.CreatedProviderType;
                return;
            }

            // STAR and graph creation flows may allocate the public GUID before their first
            // save. Persist a complete creation audit record for those inserts even though the
            // generic manager correctly treats the supplied GUID as an existing-client shape.
            if (holon.CreatedDate == DateTime.MinValue)
                holon.CreatedDate = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(holon.CreatedByAvatarId)
                && holon.MetaData != null
                && holon.MetaData.TryGetValue("CreatedByAvatarId", out var createdByAvatarId))
            {
                holon.CreatedByAvatarId = createdByAvatarId?.ToString();
            }

            // The manager cannot know whether a caller-assigned public GUID is a first save.
            // This branch can. A first insert has creation audit only; it is not an update.
            holon.ModifiedDate = DateTime.MinValue;
            holon.ModifiedByAvatarId = string.Empty;
        }
    }
}
