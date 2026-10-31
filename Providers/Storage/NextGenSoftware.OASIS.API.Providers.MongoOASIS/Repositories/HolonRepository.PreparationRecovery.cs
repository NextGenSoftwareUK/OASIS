using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    public async Task<OASISResult<HolonWritePreparation>> ReadHolonWritePreparationAsync(
        Guid operationId, Guid actorId, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (operationId == Guid.Empty || actorId == Guid.Empty)
                return PreparationError("COSMIC_PREPARATION_INVALID", "Operation and authenticated actor are required.");
            // A primary majority read observes the durable original. No fence,
            // session, transaction, insert or first-winner claim is entered.
            var collection = _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWritePreparation")
                .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority);
            var body = await collection.Find(new BsonDocument("_id", operationId.ToString("D")))
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            var result = body == null ? new OASISResult<HolonWritePreparation>() : ReadPreparation(operationId, actorId, body);
            result.IsLoaded = !result.IsError;
            return result;
        }
        catch (Exception cause)
        {
            return PreparationError(cause is OperationCanceledException ? "COSMIC_PREPARATION_CANCELLED" : "COSMIC_PREPARATION_UNVERIFIED",
                "The original protected preparation could not be read.", cause);
        }
    }
}
