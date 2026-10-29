using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private const string OutboxTargetIndexShape = "type='RANGE' AND labelsOrTypes=['CosmicHolonOutbox'] AND properties=['TargetId']";
    private static async Task<bool> HasOutboxTargetOrderingIndexAsync(IAsyncSession session)
    {
        var row = await (await session.RunAsync("SHOW INDEXES YIELD type,state,labelsOrTypes,properties WHERE "
            + OutboxTargetIndexShape + " AND state='ONLINE' RETURN count(*) AS count").ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        return row["count"].As<long>() > 0;
    }
    private static async Task EnsureOutboxTargetOrderingIndexAsync(IAsyncSession session)
    {
        if (await HasOutboxTargetOrderingIndexAsync(session).ConfigureAwait(false)) return;
        await (await session.RunAsync("CREATE RANGE INDEX oasis_cosmic_outbox_target_order IF NOT EXISTS "
            + "FOR (o:CosmicHolonOutbox) ON (o.TargetId)").ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        // Resolve by actual shape, not the requested name: an equivalent index may
        // already have another name, and a name collision is not schema proof.
        var candidates = await (await session.RunAsync("SHOW INDEXES YIELD name,type,state,labelsOrTypes,properties WHERE "
            + OutboxTargetIndexShape + " RETURN name ORDER BY name LIMIT 1").ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if (candidates.Count != 1) throw new InvalidDataException("The required native outbox target-order index is missing or has the wrong shape.");
        await (await session.RunAsync("CALL db.awaitIndex($name,30)", new { name = candidates[0]["name"].As<string>() })
            .ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        if (!await HasOutboxTargetOrderingIndexAsync(session).ConfigureAwait(false))
            throw new InvalidDataException("The required native outbox target-order index is not online.");
    }
}
