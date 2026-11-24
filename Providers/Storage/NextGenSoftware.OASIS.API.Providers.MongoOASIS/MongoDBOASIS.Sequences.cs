using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicSequenceStorageProvider
{
    /// <summary>One atomic findOneAndUpdate($inc, upsert) per block, majority/journaled.</summary>
    public async Task<OASISResult<CosmicSequenceBlock>> ReserveSequenceBlockAsync(string name, int blockSize = 1,
        CancellationToken cancellationToken = default)
    {
        var invalid = CosmicSequences.Validate(name, blockSize);
        if (invalid != null) return CosmicSequences.Error("COSMIC_SEQUENCE_INVALID", invalid);
        if (!IsProviderActivated || Database?.MongoDB == null)
            return CosmicSequences.Error("COSMIC_AUTHORITY_UNAVAILABLE", "MongoDB provider is not activated.");
        try
        {
            var sequences = Database.MongoDB.GetCollection<BsonDocument>("CosmicSequence")
                .WithWriteConcern(WriteConcern.WMajority.With(journal: true));
            var updated = await sequences.FindOneAndUpdateAsync(new BsonDocument("_id", name),
                new BsonDocument("$inc", new BsonDocument("Value", (long)blockSize)),
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
                cancellationToken).ConfigureAwait(false);
            var last = updated["Value"].ToInt64();
            return new OASISResult<CosmicSequenceBlock>(new CosmicSequenceBlock(name, checked(last - blockSize + 1), last));
        }
        catch (Exception cause)
        {
            return CosmicSequences.Error(cause is OperationCanceledException ? "COSMIC_SEQUENCE_CANCELLED" : "COSMIC_SEQUENCE_FAILED",
                "The MongoDB sequence block could not be reserved; its outcome is unknown, so the caller must not assume values.", cause);
        }
    }
}
