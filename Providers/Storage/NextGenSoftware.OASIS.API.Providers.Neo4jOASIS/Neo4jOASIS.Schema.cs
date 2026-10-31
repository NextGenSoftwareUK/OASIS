using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private async Task EnsureHolonWriteSchemaAsync()
    {
        using var session = _driver.AsyncSession();
        try
        {
            if (await HasHolonWriteSchemaAsync(session).ConfigureAwait(false)) return;
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_holon_id IF NOT EXISTS FOR (h:Holon) REQUIRE h.Id IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_receipt_operation IF NOT EXISTS FOR (r:CosmicWriteReceipt) REQUIRE r.OperationId IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_record_version_identity IF NOT EXISTS FOR (v:OasisRecordVersion) REQUIRE (v.Kind,v.RecordId,v.Version) IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_replica_head IF NOT EXISTS FOR (r:CosmicReplicaHead) REQUIRE r.HolonId IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_outbox_sequence_owner IF NOT EXISTS FOR (s:CosmicHolonOutboxSequence) REQUIRE s.Name IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_outbox_sequence IF NOT EXISTS FOR (s:CosmicHolonOutbox) REQUIRE s.Sequence IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_outbox_operation_target IF NOT EXISTS FOR (s:CosmicHolonOutbox) REQUIRE (s.OperationId,s.TargetId) IS UNIQUE").ConfigureAwait(false))
                .ConsumeAsync().ConfigureAwait(false);
            // IF NOT EXISTS can encounter an existing name. Verify the actual guarantee by shape.
            if (!await HasHolonWriteSchemaAsync(session).ConfigureAwait(false))
                throw new InvalidDataException("Neo4j requires native holon, receipt, replica-head, historical-version and primary-outbox identity constraints before activation.");
        }
        catch (ClientException cause)
        {
            throw new InvalidDataException("Neo4j rejected the required native write schema; activation cannot continue.", cause);
        }
    }
    private static async Task<bool> HasHolonWriteSchemaAsync(IAsyncSession session)
    {
        var verified = await (await session.RunAsync("SHOW CONSTRAINTS YIELD type,labelsOrTypes,properties "
            + "WHERE type IN ['UNIQUENESS','NODE_KEY'] AND "
            + "((labelsOrTypes=['Holon'] AND properties=['Id']) OR "
            + "(labelsOrTypes=['CosmicWriteReceipt'] AND properties=['OperationId']) OR "
            + "(labelsOrTypes=['CosmicReplicaHead'] AND properties=['HolonId']) OR "
            + "(labelsOrTypes=['CosmicHolonOutboxSequence'] AND properties=['Name']) OR "
            + "(labelsOrTypes=['CosmicHolonOutbox'] AND properties=['Sequence']) OR "
            + "(labelsOrTypes=['CosmicHolonOutbox'] AND size(properties)=2 AND all(p IN ['OperationId','TargetId'] WHERE p IN properties)) OR "
            + "(labelsOrTypes=['OasisRecordVersion'] AND size(properties)=3 AND all(p IN ['Kind','RecordId','Version'] WHERE p IN properties))) "
            + "RETURN count(DISTINCT CASE WHEN labelsOrTypes=['CosmicHolonOutbox'] THEN CASE WHEN properties=['Sequence'] THEN 'outbox-sequence' ELSE 'outbox-operation-target' END ELSE labelsOrTypes[0] END) AS count").ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        return verified["count"].As<long>() == 7;
    }
}
