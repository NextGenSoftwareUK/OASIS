using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private static CreateIndexModel<BsonDocument>[] PrimaryOutboxIndexes()=>new[]{
        new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("Sequence"),new CreateIndexOptions{Unique=true,Name="cosmic_holon_outbox_sequence_v1"}),
        new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("OperationId").Ascending("TargetId"),new CreateIndexOptions{Unique=true,Name="cosmic_holon_outbox_operation_target_v1"}),
        new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("TargetId").Ascending("Sequence"),new CreateIndexOptions{Name="cosmic_holon_outbox_target_order_v1"})};
    internal void EnsurePrimaryOutboxIndexes()=>_dbContext.MongoDB.GetCollection<BsonDocument>("CosmicHolonOutbox").Indexes.CreateMany(PrimaryOutboxIndexes());
    internal Task EnsurePrimaryOutboxIndexesAsync()=>_dbContext.MongoDB.GetCollection<BsonDocument>("CosmicHolonOutbox").Indexes.CreateManyAsync(PrimaryOutboxIndexes());

    private async Task WritePrimaryHolonObligationsAsync(IClientSessionHandle session,ConditionalHolonPlan plan,Guid actor,
        string payload,DateTime now,CancellationToken cancellationToken)
    {
        // The counter shares the graph transaction: rolled-back source writes cannot consume or expose an obligation sequence.
        var counters=_dbContext.MongoDB.GetCollection<BsonDocument>("CosmicHolonOutboxSequence");
        var targetCount=plan.ReplicationEnrollment.TargetIds.Count;
        var counter=await counters.FindOneAndUpdateAsync(session,new BsonDocument("_id","primary-holon-outbox-v1"),
            new BsonDocument("$inc",new BsonDocument("Value",(long)targetCount)),new FindOneAndUpdateOptions<BsonDocument>{IsUpsert=true,ReturnDocument=ReturnDocument.After},cancellationToken).ConfigureAwait(false);
        if(counter==null || !counter.TryGetValue("Value",out var value) || !value.IsInt64 || value.AsInt64<targetCount)
            throw new InvalidDataException("The primary outbox sequence is invalid.");
        var sequence=value.AsInt64-targetCount;
        var collection=_dbContext.MongoDB.GetCollection<BsonDocument>("CosmicHolonOutbox");
        var newest=await collection.Find(session,FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("Sequence",-1)).Limit(1).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if(newest!=null && (!newest.TryGetValue("Sequence",out var highest) || !highest.IsInt64 || highest.AsInt64>sequence))
            throw new InvalidDataException("The primary outbox sequence regressed behind retained obligations.");
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        foreach(var target in plan.ReplicationEnrollment.TargetIds)
        {
            sequence=checked(sequence+1);
            await collection.InsertOneAsync(session,new BsonDocument{
                {"_id",plan.OperationId.ToString("D")+":"+target},{"Format",1},{"Sequence",sequence},
                {"OperationId",plan.OperationId.ToString("D")},{"TargetId",target},{"SourceId",plan.ReplicationEnrollment.SourceId},
                {"SourceAuthority",plan.Authority.ToString()},{"ActorId",actor.ToString("D")},
                {"PayloadJson",payload},{"PayloadDigest",digest},{"CreatedAtUtcTicks",now.Ticks},
                {"DeliveryState","Pending"},{"AttemptCount",0},{"LeaseToken",""},{"LeaseWorkerId",""},
                {"LeaseUntilUtcTicks",0L},{"NextAttemptUtcTicks",0L},{"CompletedAtUtcTicks",0L},
                {"LastErrorCode",BsonNull.Value},{"LastErrorMessage",BsonNull.Value},
                {"RecoveryEvidenceJson",BsonNull.Value},{"RecoveryEvidenceDigest",BsonNull.Value}
            },cancellationToken:cancellationToken).ConfigureAwait(false);
        }
    }
}
