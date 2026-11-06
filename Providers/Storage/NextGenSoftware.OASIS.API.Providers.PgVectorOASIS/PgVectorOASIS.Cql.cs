using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PgVectorOASIS;

public partial class PgVectorOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _queryProbeId;
    private readonly BoundedPublicKeyValueQuery? _publicQuery;

    public PgVectorOASIS(string connectionString, Guid publicProbeId) : this(connectionString)
    {
        if (publicProbeId == Guid.Empty)
            throw new ArgumentException("An existing public probe is required.", nameof(publicProbeId));
        _queryProbeId = publicProbeId;
        _publicQuery = new(QueryProviderType, (id, cancellationToken) =>
            ReadExactDocumentAsync(CollHolons, id, 1024 * 1024, cancellationToken)!);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.PgVectorOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        if (_publicQuery == null)
            return QueryUnavailable<bool>();
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            if (!await VerifyReadAuthorityAsync(deadline.Token))
                throw new InvalidDataException("The configured pgvector table or reader grant differs from the teaching contract.");
            var result = await _publicQuery.VerifyProbeAsync(_queryProbeId, deadline.Token);
            IsProviderActivated = !result.IsError && result.Result;
            return result;
        }
        catch (Exception error)
        {
            return QueryUnavailable<bool>(error, cancellationToken.IsCancellationRequested);
        }
    }

    private async Task<bool> VerifyReadAuthorityAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var verify = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_extension AS e
                JOIN pg_catalog.pg_type AS t ON t.typnamespace = e.extnamespace AND t.typname = 'vector'
                JOIN pg_catalog.pg_attribute AS a ON a.atttypid = t.oid
                JOIN pg_catalog.pg_class AS c ON c.oid = a.attrelid
                WHERE e.extname = 'vector' AND c.oid = pg_catalog.to_regclass('oasis_holons')
                      AND a.attname = 'embedding' AND NOT a.attisdropped AND a.atttypmod = 1536
                      AND c.relkind IN ('r', 'p')
                      AND pg_catalog.has_column_privilege(c.oid, 'id', 'SELECT')
                      AND pg_catalog.has_column_privilege(c.oid, 'doc', 'SELECT')
                      AND NOT pg_catalog.has_any_column_privilege(c.oid, 'INSERT, UPDATE, REFERENCES')
                      AND NOT pg_catalog.has_table_privilege(c.oid, 'DELETE, TRUNCATE, TRIGGER')
                      AND NOT pg_catalog.has_schema_privilege(c.relnamespace, 'CREATE')
            )
            """, conn);
        return await verify.ExecuteScalarAsync(cancellationToken) is true;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        _publicQuery == null
            ? Task.FromResult(QueryUnavailable<IReadOnlyList<IHolon>>())
            : _publicQuery.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    private static OASISResult<T> QueryUnavailable<T>(Exception? error = null, bool cancelled = false) => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = cancelled ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
        Message = "An existing pgvector teaching table and public probe are required.", Exception = error
    };
}
