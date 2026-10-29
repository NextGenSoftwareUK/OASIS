using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static async Task EnsureMigrationFenceSchemaAsync(IAsyncSession session)
    {
        async Task<bool> HasConstraints()
        {
            var row = await (await session.RunAsync("SHOW CONSTRAINTS YIELD type,labelsOrTypes,properties "
                + "WHERE type IN ['UNIQUENESS','NODE_KEY'] AND ((labelsOrTypes=['CosmicMigrationRecordGate'] AND properties=['HolonId']) "
                + "OR (labelsOrTypes=['CosmicMigrationFence'] AND properties=['MigrationId'])) RETURN count(DISTINCT labelsOrTypes[0]) AS count")
                .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            return row["count"].As<long>() == 2;
        }
        if (!await HasConstraints().ConfigureAwait(false))
        {
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_migration_record_gate IF NOT EXISTS FOR (g:CosmicMigrationRecordGate) REQUIRE g.HolonId IS UNIQUE").ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            await (await session.RunAsync("CREATE CONSTRAINT oasis_cosmic_migration_fence IF NOT EXISTS FOR (f:CosmicMigrationFence) REQUIRE f.MigrationId IS UNIQUE").ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            if (!await HasConstraints().ConfigureAwait(false)) throw new InvalidDataException("Native migration gate/fence uniqueness is required before activation.");
        }
        var markers = await (await session.RunAsync("MATCH (g:CosmicMigrationRecordGate {HolonId:$schema}) RETURN g.Format AS format",
            new { schema = NeoHolonMigrationFence.SchemaId }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if (markers.Count == 0)
        {
            var history = await (await session.RunAsync("MATCH (f:CosmicMigrationFence) RETURN count(f) AS count").ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if (history["count"].As<long>() != 0) throw new InvalidDataException("Retained fence history lost its admission marker; it was not recreated.");
            markers = await (await session.RunAsync("MERGE (g:CosmicMigrationRecordGate {HolonId:$schema}) ON CREATE SET g.Format=1 RETURN g.Format AS format",
                new { schema = NeoHolonMigrationFence.SchemaId }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        }
        if (markers.Count != 1 || markers[0]["format"] is not long format || format != 1) throw new InvalidDataException("Native migration admission marker is invalid.");
    }
}
