using System;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicSequenceStorageProvider
{
    /// <summary>A uniqueness constraint makes MERGE race-free; the SET takes the node write lock.</summary>
    public async Task<OASISResult<CosmicSequenceBlock>> ReserveSequenceBlockAsync(string name, int blockSize = 1,
        CancellationToken cancellationToken = default)
    {
        var invalid = CosmicSequences.Validate(name, blockSize);
        if (invalid != null) return CosmicSequences.Error("COSMIC_SEQUENCE_INVALID", invalid);
        if (!IsProviderActivated || _driver == null)
            return CosmicSequences.Error("COSMIC_AUTHORITY_UNAVAILABLE", "Neo4j provider is not activated.");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var session = _driver.AsyncSession();
            await (await session.RunAsync(
                "CREATE CONSTRAINT cosmic_sequence_name IF NOT EXISTS FOR (s:CosmicSequence) REQUIRE s.Name IS UNIQUE")
                .ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            var tx = await session.BeginTransactionAsync().ConfigureAwait(false);
            long last;
            try
            {
                var row = await (await tx.RunAsync("MERGE (s:CosmicSequence {Name:$name}) ON CREATE SET s.Value=0 "
                    + "SET s.Value=s.Value+$size RETURN s.Value AS value", new { name, size = (long)blockSize })
                    .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                last = row["value"].As<long>();
                await tx.CommitAsync().ConfigureAwait(false);
            }
            finally { await tx.DisposeAsync().ConfigureAwait(false); }
            return new OASISResult<CosmicSequenceBlock>(new CosmicSequenceBlock(name, checked(last - blockSize + 1), last));
        }
        catch (Exception cause)
        {
            return CosmicSequences.Error(cause is OperationCanceledException ? "COSMIC_SEQUENCE_CANCELLED" : "COSMIC_SEQUENCE_FAILED",
                "The Neo4j sequence block could not be reserved; its outcome is unknown, so the caller must not assume values.", cause);
        }
    }
}
