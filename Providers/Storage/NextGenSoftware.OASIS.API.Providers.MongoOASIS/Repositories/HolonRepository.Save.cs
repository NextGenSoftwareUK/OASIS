using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private async Task<OASISResult<Holon>> SaveNativeHolonAsync(Holon holon,bool updating,bool synchronous)
    {
        var entered=false;
        try
        {
            if(holon==null) throw new ArgumentNullException(nameof(holon));
            if(holon.HolonId==Guid.Empty)
            {
                if(updating) throw new ArgumentException("A public holon identity is required for update.");
                holon.HolonId=Guid.NewGuid();
            }
            FilterDefinition<Holon> expected=null;
            if(updating)
            {
                // Capture the mapped persisted snapshot without read-time identity projection.
                var lookup=Builders<Holon>.Filter.Where(row=>row.HolonId==holon.HolonId && row.DeletedDate==DateTime.MinValue);
                var query=_dbContext.Holon.Find(lookup).Limit(2);
                var originals=synchronous ? query.ToList():await query.ToListAsync().ConfigureAwait(false);
                if(originals.Count>1) throw new StorageIdentityAmbiguousException();
                var original=originals.SingleOrDefault();
                if(original==null) return new OASISResult<Holon> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found for update."};
                if(!string.IsNullOrEmpty(holon.Id) && holon.Id!=original.Id)
                {
                    var conflict=new StorageLifecycleConflictException("Mongo holon physical identity changed before update.");
                    return new OASISResult<Holon> {IsError=true,ErrorCode=StorageLifecycleConflictException.ErrorCode,Exception=conflict,Message=conflict.Message};
                }
                holon.Id=original.Id;
                // Each literal field equality is part of one atomic replacement predicate.
                expected=new BsonDocumentFilterDefinition<Holon>(new BsonDocument(original.ToBsonDocument()
                    .Select(field=>new BsonElement(field.Name,new BsonDocument("$eq",field.Value)))));
            }
            if(!updating) holon.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
            PrepareNativeHolonIdentity(holon);
            var collection=_dbContext.Holon.WithWriteConcern(WriteConcern.WMajority.With(journal:true));
            entered=true;
            if(updating)
            {
                var replaced=synchronous ? collection.ReplaceOne(expected,holon):await collection.ReplaceOneAsync(expected,holon).ConfigureAwait(false);
                if(!replaced.IsAcknowledged) throw new InvalidDataException("Mongo holon update was not acknowledged.");
                if(replaced.MatchedCount==0)
                {
                    var conflict=new StorageLifecycleConflictException("Mongo holon changed after observation; no replacement was written.");
                    return new OASISResult<Holon> {IsError=true,ErrorCode=StorageLifecycleConflictException.ErrorCode,Exception=conflict,Message=conflict.Message};
                }
            }
            else if(synchronous) collection.InsertOne(holon);
            else await collection.InsertOneAsync(holon).ConfigureAwait(false);
            return new OASISResult<Holon> {Result=holon,IsSaved=true,SavedCount=1};
        }
        catch(Exception cause)
        {
            // BadValue is an authoritative server rejection before the single write executes.
            // All other post-dispatch failures remain conservative until native reconciliation.
            var rejected=cause is MongoCommandException command && command.Code==2;
            var failure=entered && !rejected ? new MongoMutationReconciliationException(holon?.HolonId ?? Guid.Empty,cause):cause;
            return new OASISResult<Holon> {IsError=true,Exception=failure,
                ErrorCode=cause is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity
                    :!entered && cause is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID":null,
                Message="Mongo holon save failed."};
        }
    }
}
