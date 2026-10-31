using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
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

        // Each bulk request is one mutation unit: failed writes must not leave earlier roots committed.
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveGraphsAtomicAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth).GetAwaiter().GetResult();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveGraphsAtomicAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth);
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

        public IEnumerable<IHolon> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            return GetHolonsNearMeAsync(geoLat, geoLong, radiusInMeters, Type).Result.Result;
        }

        public Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
            => ReadNearbyRecordsAsync(geoLat, geoLong, radiusInMeters,
                () => LoadAllHolonsAsync(HolonType.All, loadChildren: false), Type);

        public IEnumerable<IAvatar> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            return GetAvatarsNearMeAsync(geoLat, geoLong, radiusInMeters).Result.Result;
        }

        public Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long geoLat, long geoLong, int radiusInMeters)
            => ReadNearbyRecordsAsync(geoLat, geoLong, radiusInMeters, () => LoadAllAvatarsAsync());

        private async Task<OASISResult<IEnumerable<T>>> ReadNearbyRecordsAsync<T>(long latitude, long longitude, int radius,
            Func<Task<OASISResult<IEnumerable<T>>>> load, HolonType type = HolonType.All) where T : IHolonBase
        {
            var result = new OASISResult<IEnumerable<T>>();
            try
            {
                var query = new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(latitude, longitude, radius);
                if (!Enum.IsDefined(typeof(HolonType), type)) throw new ArgumentOutOfRangeException(nameof(type));
                var loaded = await load();
                if (loaded.IsError) return loaded;
                result = NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<T>>(loaded);
                result.Result = query.Select(loaded.Result, row => row.IsActive && !row.IsDeleted && (type == HolonType.All || row.HolonType == type));
                result.IsLoaded = true;
            }
            catch (Exception cause)
            {
                result.Result = null;
                result.IsLoaded = false;
                OASISErrorHandling.HandleError(ref result, cause.Message, cause);
            }
            return result;
        }


        //IOASISSuperStar Interface Implementation

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // Mongo currently does not generate native code from STAR metadata.
            return false;
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            return GetAvatarsNearMeAsync(geoLat, geoLong, radiusInMeters).Result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            return GetHolonsNearMeAsync(geoLat, geoLong, radiusInMeters, Type).Result;
        }

        // distance calculation moved to GeoHelper for reuse
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate MongoDB provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null) throw new ArgumentNullException(nameof(holons));
                var importRoots = holons.ToList();
                foreach (var importRoot in importRoots)
                {
                    var graphError = ValidateMongoChildGraph(importRoot);
                    if (graphError != null)
                    {
                        result.IsError = true;
                        result.ErrorCode = graphError.ErrorCode;
                        result.Exception = graphError.Exception;
                        result.Message = graphError.Message;
                        return result;
                    }
                }
                return await _holonRepository.ImportAtomicAsync(importRoots.Select(holon => DataHelper.ConvertOASISHolonToMongoEntity(holon)), PreserveCreationAuditFields).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to MongoDB: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            return await ReadCanonicalMongoExportAsync(version,avatarId);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await ReadCanonicalMongoExportSelectorAsync(avatarUsername,false,version);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            return await ReadCanonicalMongoExportSelectorAsync(avatarEmailAddress,true,version);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await ReadCanonicalMongoExportAsync(version);
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> ReadCanonicalMongoExportAsync(int version,Guid? owner=null)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try {
                if(version < -1)throw new ArgumentOutOfRangeException(nameof(version));
                if(owner.HasValue&&owner.Value==Guid.Empty)throw new ArgumentException("A nonempty export owner is required.",nameof(owner));
                var loaded=await LoadMongoExportRootsAsync(version);
                if(loaded.IsError)return loaded;
                if(loaded.Result==null)throw new System.IO.InvalidDataException("Mongo export source returned no collection.");
                result=NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IEnumerable<IHolon>>(loaded);
                var rows=loaded.Result.Where(row=>!owner.HasValue||row.CreatedByAvatarId==owner.Value).ToList();
                result.Result=rows;result.IsLoaded=true;result.LoadedCount=rows.Count;
            }catch(Exception cause){OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
            return result;
        }

        protected virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadMongoExportRootsAsync(int version)
        {
            if(version==0)return await LoadAllHolonsAsync(loadChildren:false);
            var result=new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(version < -1)throw new ArgumentOutOfRangeException(nameof(version));
                if(!IsProviderActivated)
                {
                    var activation=await ActivateProviderAsync();
                    if(activation.IsError)return NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IHolon>>(activation);
                }
                var collection=Database.MongoDB.GetCollection<BsonDocument>(Database.Holon.CollectionNamespace.CollectionName);
                var documents=await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                var records=documents.SelectMany(document=>MongoNativeHolonHistory.Read(document)).ToArray();
                if(records.GroupBy(row=>(row.Id,row.Version)).Any(group=>group.Count()>1))
                    throw new StorageIdentityAmbiguousException();
                var selected=records.Where(row=>(version==-1 || row.Version==version) && row.DeletedDate==DateTime.MinValue).ToList();
                result.Result=selected;result.IsLoaded=true;result.LoadedCount=selected.Count;
            }
            catch(Exception cause){OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> ReadCanonicalMongoExportSelectorAsync(string selector,bool email,int version)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try {
                if(version < -1)throw new ArgumentOutOfRangeException(nameof(version));
                if(string.IsNullOrWhiteSpace(selector))throw new ArgumentException("A nonblank export identity is required.",nameof(selector));
                var owner=email?await LoadAvatarByEmailAsync(selector):await LoadAvatarByUsernameAsync(selector);
                if(owner.IsError)return NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(owner);
                if(owner.Result==null)throw new System.IO.InvalidDataException("Mongo export identity returned no avatar.");
                return await ReadCanonicalMongoExportAsync(version,owner.Result.Id);
            }catch(Exception cause){OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }
    }
}
