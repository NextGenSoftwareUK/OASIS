using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteCosmicChainIndexStore
{
    public static CosmicProjectionCapabilities CanonicalProjectionCapabilities { get; } = new(new[] {
        new CosmicProjectionFieldCapability("Id", typeof(Guid)), new CosmicProjectionFieldCapability("VersionId", typeof(Guid)),
        new CosmicProjectionFieldCapability("CreatedByAvatarId", typeof(Guid)), new CosmicProjectionFieldCapability("Name", typeof(string)),
        new CosmicProjectionFieldCapability("Version", typeof(int)), new CosmicProjectionFieldCapability("IsPublic", typeof(bool)),
        new CosmicProjectionFieldCapability("IsDeleted", typeof(bool)) }, Array.Empty<Type>());

    public CosmicProjectionCapabilities IndexedProjectionCapabilities => canonicalRowProjection
        ? CanonicalProjectionCapabilities : new(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>());

    public async Task<OASISResult<CosmicChainIndexProjectionRows>> QueryProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, ICosmicChainQuerySourceObserver observer,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        if(plan == null) return Error<CosmicChainIndexProjectionRows>("CQL_PLAN_INVALID", "A selected indexed plan is required.");
        var admission = IndexedProjectionCapabilities.Admit(plan.Query);
        if(admission.IsError) return Error<CosmicChainIndexProjectionRows>(admission.ErrorCode, admission.Message);
        var cells = new List<CosmicProjectionRow>();
        // Selected cells, encrypted payload and receipt share ONE SQL transaction.
        // Full source validation completes before cells can be acknowledged.
        var evidence = await QueryRowsCoreAsync(plan.SourcePlan, observer, maximumResults, cancellationToken, plan.Query.Columns, cells);
        if(evidence.IsError) return Error<CosmicChainIndexProjectionRows>(evidence.ErrorCode, evidence.Message, evidence.Exception);
        return new(new(evidence.Result, cells)) { IsLoaded = true, LoadedCount = cells.Count };
    }

    private static string ProjectionSqlColumn(CosmicQueryPath path)
    {
        if(path.IsMetadata || path.Segments.Count != 1 || !CanonicalProjectionCapabilities.CanonicalFields.Any(
            field => field.Name == path.Segments[0] && field.ValueType == path.ValueType))
            throw new NotSupportedException("Indexed selected SQL requires an enrolled canonical field/domain.");
        return "Holon.\"" + path.Segments[0] + "\"";
    }

    private static object ProjectionSqlValue(SqliteDataReader reader, int ordinal, CosmicQueryPath path)
    {
        if(reader.IsDBNull(ordinal)) return null;
        if(path.ValueType == typeof(Guid)) return Guid.Parse(reader.GetString(ordinal));
        if(path.ValueType == typeof(int)) return reader.GetInt32(ordinal);
        if(path.ValueType == typeof(bool)) return reader.GetBoolean(ordinal);
        if(path.ValueType == typeof(string)) return reader.GetString(ordinal);
        throw new NotSupportedException("No indexed selected SQL scalar owner.");
    }
}
