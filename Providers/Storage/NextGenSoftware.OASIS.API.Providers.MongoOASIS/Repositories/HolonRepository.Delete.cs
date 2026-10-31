using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private async Task<OASISResult<IHolon>> DeleteObservedHolonAsync(FilterDefinition<Holon> lookup)
    {
        var entered=false;var id=Guid.Empty;
        try
        {
            // No GetHolon call: that legacy lookup can write provider keys during a read.
            var rows=await _dbContext.Holon.Find(lookup).Limit(2).ToListAsync();
            if(rows.Count>1) throw new StorageIdentityAmbiguousException();
            var observed=rows.SingleOrDefault();
            if(observed==null) return new OASISResult<IHolon> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found."};
            id=observed.HolonId;
            // Explicit $eq prevents metadata values resembling operators from becoming query syntax.
            var expected=new BsonDocumentFilterDefinition<Holon>(new BsonDocument(observed.ToBsonDocument()
                .Select(field=>new BsonElement(field.Name,new BsonDocument("$eq",field.Value)))));
            entered=true;
            var deleted=await _dbContext.Holon.WithWriteConcern(WriteConcern.WMajority.With(journal:true)).FindOneAndDeleteAsync(expected);
            if(deleted==null) return new OASISResult<IHolon> {IsError=true,ErrorCode=StorageLifecycleConflictException.ErrorCode,
                Exception=new StorageLifecycleConflictException("MongoDB holon changed before deletion; no observed record was deleted."),Message="Observed holon changed before deletion."};
            return new OASISResult<IHolon> {IsDeleted=true,DeletedCount=1,Result=Helpers.DataHelper.ConvertMongoEntityToOASISHolon(deleted)};
        }
        catch(Exception cause)
        {
            var failure=entered ? new MongoMutationReconciliationException(id,cause):cause;
            return new OASISResult<IHolon> {IsError=true,Exception=failure,
                ErrorCode=cause is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity:null,
                Message="MongoDB holon deletion failed."};
        }
    }
}
