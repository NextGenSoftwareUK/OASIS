using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>One native identity-membership query over Mongo and another admitted provider.</summary>
public static class IdentityFederationWalkthrough
{
    public static async Task<CosmicFederationResult> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider mongo,
        ICosmicQueryStorageProvider secondary,
        IEnumerable<Guid> selectedIds,
        IEnumerable<Guid> readableMongoIds,
        IEnumerable<Guid> readableSecondaryIds,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // 1. Freeze the user's selection. Both native sources receive the same
        // Contains predicate. These are canonical holon IDs, not storage addresses.
        var ids = selectedIds.ToArray();
        var scope = Require(CosmicQueryScope.Named(mongo.QueryProviderType, secondary.QueryProviderType));
        var source = Require(CosmicQuery<Holon>.Create(scope).Where(row => ids.Contains(row.Id)));

        return await RunQueryAsync(manager, mongo, secondary, source,
            readableMongoIds, readableSecondaryIds, actor, output, cancellationToken);
    }

    /// <summary>Execute a captured content or identity query through the same native owners.</summary>
    public static async Task<CosmicFederationResult> RunQueryAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider mongo,
        ICosmicQueryStorageProvider secondary,
        CosmicQuery<Holon> source,
        IEnumerable<Guid> readableMongoIds,
        IEnumerable<Guid> readableSecondaryIds,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (mongo.QueryProviderType != ProviderType.MongoDBOASIS ||
            secondary.QueryProviderType is ProviderType.MongoDBOASIS or ProviderType.Default or ProviderType.None or ProviderType.All)
            throw new ArgumentException("Supply activated Mongo and one distinct concrete provider, in that order.");

        // 2. Authorization belongs to each source. Obtain these catalogues from
        // trusted application policy, never from IDs submitted in an HTTP request.
        var policies = new[]
        {
            new CosmicFederationReadPolicy(mongo.QueryProviderType,
                Require(CosmicQueryReadAccess.AuthoritativeRecords(actor, mongo.QueryProviderType, readableMongoIds))),
            new CosmicFederationReadPolicy(secondary.QueryProviderType,
                Require(CosmicQueryReadAccess.AuthoritativeRecords(actor, secondary.QueryProviderType, readableSecondaryIds)))
        };

        // 3. Bound complete provider reads before merging. Physical copies remain
        // visible with provider provenance; this does not claim a shared snapshot.
        var adapters = new[] { mongo, secondary };
        var targets = adapters.Select(adapter => new CosmicQueryTarget(
            adapter.QueryProviderType, adapter.QueryCapabilities)).ToArray();
        var options = Require(CosmicFederationOptions.Create(
            take: 16, maximumInputRows: 16, maximumRowsPerProvider: 8,
            maximumProviderAttempts: 2, maximumDurationMilliseconds: 30000));
        var plan = Require(CosmicFederationPlanner.Plan(source, options, mongo.QueryProviderType, targets, policies));

        // 4. HolonManager dispatches both native queries. A failed source fails the
        // operation: no cached or other-provider rows replace its contribution.
        var result = Require(await manager.ExecuteFederationQueryAsync(plan, adapters, cancellationToken));

        await output.WriteLineAsync($"\n=== CQL: Mongo + {DisplayName(secondary)} native query ===\n");
        await output.WriteLineAsync($"Physical rows observed: {result.ObservedInputRows}\n");
        foreach (var row in result.Rows)
        {
            await output.WriteLineAsync($"  Provider: {row.Selected.Provider}");
            await output.WriteLineAsync($"  Holon ID: {row.Selected.Row.Id}\n");
        }

        return result;
    }

    internal static string DisplayName(ICosmicQueryStorageProvider provider) => provider.QueryProviderType switch
    {
        ProviderType.TRONOASIS => "TRON",
        ProviderType.OracleDBOASIS => "Oracle",
        ProviderType.AWSOASIS => "AWS DynamoDB",
        ProviderType.EthereumOASIS => "Ethereum",
        ProviderType.PolygonOASIS => "Polygon",
        ProviderType.ArbitrumOASIS => "Arbitrum",
        ProviderType.BaseOASIS => "Base",
        ProviderType.AvalancheOASIS => "Avalanche",
        ProviderType.OptimismOASIS => "Optimism",
        ProviderType.RootstockOASIS => "Rootstock",
        ProviderType.BNBChainOASIS => "BNBChain",
        ProviderType.FantomOASIS => "Fantom",
        ProviderType.LineaOASIS => "Linea",
        ProviderType.ScrollOASIS => "Scroll",
        ProviderType.MonadOASIS => "Monad",
        ProviderType.AzureStorageOASIS => "Azure Blob",
        ProviderType.AWSS3OASIS => "AWS S3",
        ProviderType.LocalFileOASIS => "LocalFile",
        ProviderType.AptosOASIS => "Aptos",
        ProviderType.SuiOASIS => "Sui",
        ProviderType.RedisOASIS => "Redis",
        ProviderType.PostgreSQLOASIS => "PostgreSQL",
        ProviderType.SQLServerDBOASIS => "SQL Server",
        ProviderType.DuckDBOASIS => "DuckDB",
        ProviderType.UpstashOASIS => "Upstash",
        ProviderType.LitestreamOASIS => "Litestream",
        ProviderType.MemcachedOASIS => "Memcached",
        ProviderType.ValKeyOASIS => "Valkey",
        ProviderType.KeyDBOASIS => "KeyDB",
        ProviderType.DragonflyOASIS => "Dragonfly",
        ProviderType.GarnetOASIS => "Garnet",
        ProviderType.PocketBaseOASIS => "PocketBase",
        ProviderType.SupabaseOASIS => "Supabase",
        ProviderType.CouchDBOASIS => "CouchDB",
        ProviderType.SurrealDBOASIS => "SurrealDB",
        ProviderType.ArangoDBOASIS => "ArangoDB",
        ProviderType.RavenDBOASIS => "RavenDB",
        ProviderType.CockroachDBOASIS => "CockroachDB",
        ProviderType.TimescaleDBOASIS => "TimescaleDB",
        ProviderType.CassandraOASIS => "Cassandra",
        ProviderType.ElasticsearchOASIS => "Elasticsearch",
        ProviderType.OpenSearchOASIS => "OpenSearch",
        ProviderType.CouchbaseOASIS => "Couchbase",
        ProviderType.ClickHouseOASIS => "ClickHouse",
        ProviderType.InfluxDBOASIS => "InfluxDB",
        ProviderType.QdrantOASIS => "Qdrant",
        ProviderType.WeaviateOASIS => "Weaviate",
        ProviderType.MeilisearchOASIS => "Meilisearch",
        ProviderType.TypesenseOASIS => "Typesense",
        ProviderType.SolrOASIS => "Solr",
        ProviderType.ChromaOASIS => "Chroma",
        ProviderType.ScyllaDBOASIS => "ScyllaDB",
        ProviderType.FirebaseOASIS => "Firebase RTDB",
        ProviderType.GoogleCloudOASIS => "Google Firestore",
        ProviderType.CosmosBlockChainOASIS => "Cosmos blockchain",
        ProviderType.PolkadotOASIS => "Polkadot",
        ProviderType.NEAROASIS => "NEAR",
        _ => provider.QueryProviderType.ToString()
    };

    private static T Require<T>(OASISResult<T> result)
    {
        if (result.IsError || result.Result == null)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }
}
