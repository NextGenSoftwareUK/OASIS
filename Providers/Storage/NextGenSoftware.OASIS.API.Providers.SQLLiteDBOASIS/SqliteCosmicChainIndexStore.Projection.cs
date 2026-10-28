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
        ? metadataPolicy==null?CanonicalProjectionCapabilities:new CosmicProjectionCapabilities(
            CanonicalProjectionCapabilities.CanonicalFields.ToArray(),new[]{typeof(string),typeof(bool),typeof(bool?),typeof(int),typeof(int?),typeof(long),typeof(long?)})
            .WithMetadataRoots(metadataPolicy.Keys) : new(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>());

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

    private static object ProjectionSqlValue(SqliteDataReader reader,int stateOrdinal,CosmicQueryPath path)
    {
        var state=reader.GetInt64(stateOrdinal);
        if(state is <0 or >2)throw new System.IO.InvalidDataException("Selected index metadata has an ambiguous or invalid domain.");
        if(state is 0 or 1)
        {
            if(path.ValueType.IsValueType&&Nullable.GetUnderlyingType(path.ValueType)==null)
                throw new System.IO.InvalidDataException("A selected non-nullable index value is missing or null.");
            return null;
        }
        return Persistence.Repositories.HolonRepository.DecodeChainIndexProjection(reader.GetValue(stateOrdinal+1),path.ValueType);
    }
}
