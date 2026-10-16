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
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(x => x.Email == avatarEmail && (version == 0 || x.Version == version)));
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatarDetails(await _avatarRepository.GetAvatarDetailsAsync(version));
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.HolonId == id && (version == 0 || x.Version == version)));
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.Email == avatarEmail && (version == 0 || x.Version == version)));
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatarDetails(_avatarRepository.GetAvatarDetails(version));
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonGraphAsync(id,null,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version,false);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonGraphAsync(id,null,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();


        //public override T LoadHolon<T>(Guid id)
        //{
        //    return ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(id))).Result;
        //}

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonGraphAsync(null,providerKey,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version,false);

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonGraphAsync(null,providerKey,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();

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

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,id,null,null,MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,false);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,id,null,null,MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,providerKey ?? string.Empty,null,MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,false);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,providerKey ?? string.Empty,null,MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();

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
            => ReadHolonCollectionGraphAsync(type,null,null,new Dictionary<string,string> {{metaKey ?? string.Empty,metaValue}},MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,null,new Dictionary<string,string> {{metaKey ?? string.Empty,metaValue}},MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,false);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,null,metaKeyValuePairs ?? new Dictionary<string,string>(),metaKeyValuePairMatchMode,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,null,metaKeyValuePairs ?? new Dictionary<string,string>(),metaKeyValuePairMatchMode,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,false);

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,null,null,MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,false);

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonCollectionGraphAsync(type,null,null,null,MetaKeyValuePairMatchMode.All,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,true).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var graphError = ValidateMongoChildGraph(holon, maxChildDepth);
            if (graphError != null) return graphError;
            if (saveChildren && saveChildrenOnProvider && holon.Children != null && holon.Children.Any())
                return await SaveGraphAtomicAsync(holon, recursive, maxChildDepth);
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

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var graphError = ValidateMongoChildGraph(holon, maxChildDepth);
            if (graphError != null) return graphError;
            if (saveChildren && saveChildrenOnProvider && holon.Children != null && holon.Children.Any())
                return SaveGraphAtomicAsync(holon, recursive, maxChildDepth).GetAwaiter().GetResult();
            var mongoHolon = DataHelper.ConvertOASISHolonToMongoEntity(holon);
            var persistedHolon = _holonRepository.GetHolon(mongoHolon.HolonId);
            PreserveCreationAuditFields(mongoHolon, persistedHolon);
            var saveResult = persistedHolon == null
                ? _holonRepository.Add(mongoHolon)
                : _holonRepository.Update(mongoHolon);
            OASISResult<IHolon> result = DataHelper.ConvertMongoEntityToOASISHolon(saveResult, saveChildrenOnProvider);

            return result;
        }

        private static OASISResult<IHolon> ValidateMongoChildGraph(IHolon root, int maxChildDepth = 0)
        {
            if (maxChildDepth < 0)
            {
                var cause = new ArgumentOutOfRangeException(nameof(maxChildDepth));
                return new OASISResult<IHolon> { IsError = true, ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID", Exception = cause, Message = cause.Message };
            }
            var pending = new Stack<IHolon>();
            var visited = new HashSet<IHolon>(new HolonReferenceComparer());
            var identities = new HashSet<Guid>();
            pending.Push(root);
            while (pending.Count != 0)
            {
                var node = pending.Pop();
                if (node == null || !visited.Add(node) || (node.Id != Guid.Empty && !identities.Add(node.Id)))
                {
                    var cause = new ArgumentException("Holon graph contains null, cyclic or repeated child identities.");
                    return new OASISResult<IHolon> { IsError = true, ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID", Exception = cause, Message = cause.Message };
                }
                foreach (var child in node.Children ?? Enumerable.Empty<IHolon>()) pending.Push(child);
            }
            return null;
        }

        private sealed class HolonReferenceComparer : IEqualityComparer<IHolon>
        {
            public bool Equals(IHolon left, IHolon right) => ReferenceEquals(left, right);
            public int GetHashCode(IHolon value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
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
