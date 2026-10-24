using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryGroupedStorageProvider
{
    private static class GroupContract
    {
        internal static readonly CosmicGroupedCapabilities Value = Create();
        private static CosmicGroupedCapabilities Create()
        {
            var keys = CqlCapabilities.CanonicalFields.Select(field => field.ValueType)
                .Concat(CqlCapabilities.MetadataTypes.Select(type => type.ValueType)).Distinct().ToArray();
            var aggregates = keys.SelectMany(type => new[]
            {
                new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Min, type),
                new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Max, type)
            }).Concat(new[]
            {
                new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Count, typeof(int)),
                new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.LongCount, typeof(long))
            }).Concat(new[] { typeof(int), typeof(int?), typeof(long), typeof(long?) }
                .Select(type => new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Sum, type))).ToArray();
            return new CosmicGroupedCapabilities(CqlProjectionCapabilities, CqlScalarCapabilities,
                new CosmicGroupedOrderingCapabilities(keys, aggregates), supportsResultPaging: true);
        }
    }
    public static CosmicGroupedCapabilities CqlGroupedCapabilities => GroupContract.Value;
    public CosmicGroupedCapabilities GroupedCapabilities => CqlGroupedCapabilities;
    public Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>> ExecuteCosmicGroupedAsync<T, TKey, TResult>(
        CosmicGroupedPlan<T, TKey, TResult> plan, int maximumGroups = 1000,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit, CancellationToken cancellationToken = default) where T : IHolon
    {
        if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<IReadOnlyList<CosmicGroupedRow>>
            { IsError = true, ErrorCount = 1, ErrorCode = "CQL_PROVIDER_NOT_READY", Message = "Activate SQLite before native grouped execution." });
        return repository.ExecuteCosmicGroupedAsync(plan, maximumGroups, maximumPayloadBytes, cancellationToken);
    }
}
