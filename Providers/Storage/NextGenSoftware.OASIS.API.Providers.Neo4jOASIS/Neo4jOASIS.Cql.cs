using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryStorageProvider
{
    ProviderType ICosmicQueryStorageProvider.QueryProviderType => Core.Enums.ProviderType.Neo4jOASIS;
    CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities => CqlCapabilities;
    public static CosmicQueryCapabilities CqlCapabilities => NeoCqlContract.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != Core.Enums.ProviderType.Neo4jOASIS)
                return CqlFailure("CQL_PLAN_INVALID", "Neo4j execution requires an exclusively Neo4j plan.");
            if (maximumResults < 1 || maximumResults > 10000)
                return CqlFailure("CQL_QUERY_LIMIT", "Neo4j CQL result limit must be between one and 10,000.");
            var admission = CqlCapabilities.Admit(plan.Query);
            if (admission.IsError) return CqlFailure(admission.ErrorCode, admission.Message);
            var translator = new NeoCqlTranslator();
            var predicate = plan.Query.Predicates.Count == 0 ? "true" : string.Join(" AND ", plan.Query.Predicates.Select(translator.Translate));
            translator.Parameters.Add("schema", NeoCqlProjection.SchemaVersion);
            translator.Parameters.Add("limit", maximumResults + 1);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated || _driver == null) return CqlFailure("CQL_PROVIDER_INACTIVE", "Neo4j provider is not activated.");
            // Invalid projections sort first and cannot silently disappear behind the
            // predicate/result window. Query predicates execute entirely in Cypher.
            var query = "MATCH (h:Holon) OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) "
                + "WITH h,collect(p) AS projections WITH h,head(projections) AS p,size(projections)=1 AS uniqueProjection "
                + "WITH h,p,coalesce(uniqueProjection AND p.SchemaVersion=$schema AND p.f_Id=toLower(h.Id) AND h.SnapshotJson IS NOT NULL "
                + "AND coalesce(p.ProviderRevisionId,'')=coalesce(h.ProviderRevisionId,''),false) AS valid "
                + "WHERE NOT valid OR (" + predicate + ") "
                + "RETURN h.Id AS id,h.ProviderRevisionId AS revision,valid, "
                + "CASE WHEN size(h.SnapshotJson)<=2097152 THEN h.SnapshotJson ELSE null END AS snapshot "
                + "ORDER BY valid ASC,id ASC LIMIT $limit";
            using var session = _driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            var cursor = await transaction.RunAsync(query, translator.Parameters).ConfigureAwait(false);
            var rows = new List<IHolon>(); long bytes = 0;
            while (await cursor.FetchAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = cursor.Current;
                if (!row["valid"].As<bool>()) throw new InvalidDataException("Neo4j CQL projection is missing, stale or has invalid ownership/schema.");
                if (rows.Count >= maximumResults) return CqlFailure("CQL_QUERY_LIMIT", "Neo4j query exceeds its declared result limit.");
                var snapshot = row["snapshot"].As<string>();
                if (snapshot == null || Encoding.UTF8.GetByteCount(snapshot) > 8 * 1024 * 1024)
                    return CqlFailure("CQL_QUERY_LIMIT", "Neo4j CQL record exceeds its payload budget.");
                bytes += Encoding.UTF8.GetByteCount(snapshot);
                if (bytes > 32 * 1024 * 1024) return CqlFailure("CQL_QUERY_LIMIT", "Neo4j CQL results exceed their payload budget.");
                var holon = StoredRecordJson.DeserializeStoredHolon(snapshot);
                if (!Guid.TryParse(row["id"].As<string>(), out var id) || holon.Id != id)
                    throw new InvalidDataException("Neo4j CQL snapshot identity differs from its native identity.");
                var physical = row["revision"].As<string>();
                var revision = Guid.Empty;
                if (physical != null && !Guid.TryParse(physical, out revision)) throw new InvalidDataException("Neo4j CQL native revision is invalid.");
                CosmicConcurrency.SetProviderRevisionId(holon, Core.Enums.ProviderType.Neo4jOASIS, revision);
                holon.IsNewHolon = false; rows.Add(holon);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new OASISResult<IReadOnlyList<IHolon>>(rows) { IsLoaded = true };
        }
        catch (OperationCanceledException cause) { return CqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (NotSupportedException cause) { return CqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (InvalidDataException cause) { return CqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (Exception cause) { return CqlFailure("CQL_PROVIDER_ERROR", cause.Message, cause); }
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
