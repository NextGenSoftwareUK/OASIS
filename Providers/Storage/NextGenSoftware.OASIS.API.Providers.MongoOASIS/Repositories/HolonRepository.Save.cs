using System;
using System.IO;
using System.Threading.Tasks;
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
            if(updating && string.IsNullOrEmpty(holon.Id))
            {
                var original=synchronous ? GetHolon(holon.HolonId):await GetHolonAsync(holon.HolonId).ConfigureAwait(false);
                if(original==null) return new OASISResult<Holon> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found for update."};
                holon.Id=original.Id;
            }
            if(!updating) holon.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
            PrepareNativeHolonIdentity(holon);
            var collection=_dbContext.Holon.WithWriteConcern(WriteConcern.WMajority.With(journal:true));
            entered=true;
            if(updating)
            {
                var filter=Builders<Holon>.Filter.Eq(row=>row.HolonId,holon.HolonId) & Builders<Holon>.Filter.Eq(row=>row.Id,holon.Id);
                var replaced=synchronous ? collection.ReplaceOne(filter,holon):await collection.ReplaceOneAsync(filter,holon).ConfigureAwait(false);
                if(!replaced.IsAcknowledged) throw new InvalidDataException("Mongo holon update was not acknowledged.");
                if(replaced.MatchedCount==0)
                {
                    var conflict=new StorageLifecycleConflictException("Mongo holon no longer matches its observed physical identity.");
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
                ErrorCode=!entered && cause is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID":null,
                Message="Mongo holon save failed."};
        }
    }
}
