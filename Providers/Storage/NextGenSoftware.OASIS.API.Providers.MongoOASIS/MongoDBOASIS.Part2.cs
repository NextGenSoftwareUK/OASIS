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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            // CHANGED: Previously this used ProviderUniqueStorageKey.ContainsKey(MongoDBOASIS)
            // to decide insert vs update. That key is an internal MongoDB implementation detail
            // (the ObjectId) that is only present if the caller round-tripped a previously loaded
            // holon. Stateless REST/JS clients constructing a holon from scratch never have this
            // key, so they always hit AddAsync and created a new document on every save.
            // The sync SaveHolon overload below already uses IsNewHolon correctly; this async
            // path now matches it. IsNewHolon is set reliably by PrepareHolonForSaving (called
            // by SaveHolonAsync in HolonManager) based solely on Id == Guid.Empty.
            //
            // Old code (kept for reference):
            // OASISResult<IHolon> result = !holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS)
            //     ? DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.AddAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider)
            //     : DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.UpdateAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider);

            //OASISResult<IHolon> result = holon.IsNewHolon
            OASISResult<IHolon> result = holon.IsNewHolon || holon.CreatedDate == DateTime.MinValue
                ? DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.AddAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider)
                : DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.UpdateAsync(DataHelper.ConvertOASISHolonToMongoEntity(holon)), saveChildrenOnProvider);

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
            OASISResult<IHolon> result = holon.IsNewHolon || holon.CreatedDate == DateTime.MinValue
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
    }
}
