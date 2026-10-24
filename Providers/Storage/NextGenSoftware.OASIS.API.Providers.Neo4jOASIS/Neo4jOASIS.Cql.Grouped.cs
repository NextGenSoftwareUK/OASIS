using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryGroupedStorageProvider
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

    public async Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>> ExecuteCosmicGroupedAsync<T, TKey, TResult>(
        CosmicGroupedPlan<T, TKey, TResult> plan, int maximumGroups = 1000,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicGroupedPlanner.Admit(plan, Core.Enums.ProviderType.Neo4jOASIS, CqlCapabilities, CqlGroupedCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicGroupedRow>>(admission);
            if (maximumGroups < 0 || maximumGroups > plan.Query.Grouping.Options.MaximumGroups
                || maximumPayloadBytes < 0 || maximumPayloadBytes > plan.Query.Grouping.Options.MaximumPayloadBytes)
                return GroupedCqlFailure("CQL_QUERY_LIMIT", "Grouped output bounds are invalid.");
            if (!IsProviderActivated || _driver == null)
                return GroupedCqlFailure("CQL_PROVIDER_INACTIVE", "Neo4j provider is not activated.");
            var command = CompileCqlGrouped(plan.Query, maximumGroups, maximumPayloadBytes);
            var budget = new NeoCqlReadBudget(maximumPayloadBytes, cancellationToken);
            budget.Check();
            using var session = _driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor = await transaction.RunAsync(command.Query, command.Parameters).ConfigureAwait(false);
            var rows = new List<CosmicGroupedRow>();
            long work = 0;
            while (true)
            {
                budget.Check();
                if (!await cursor.FetchAsync().ConfigureAwait(false)) break;
                budget.Check();
                var row = DecodeCqlGrouped(cursor.Current.Values, plan.Query);
                if (rows.Count >= maximumGroups) throw new NeoCqlLimitException("Grouping exceeds its output-group budget.");
                work = checked(work + row.InputCount);
                if (work > CosmicNumericScalarResults.MaximumParticipatingValues)
                    throw new NeoCqlLimitException("Grouping exceeds its cumulative input-work budget.");
                // Two projection envelopes equal the grouped proof overhead;
                // keys and selected values are both charged, including repeats.
                budget.ReserveProjection(row.Keys);
                budget.ReserveProjection(row.Values);
                rows.Add(row);
            }
            budget.Check();
            await transaction.RollbackAsync().ConfigureAwait(false);
            budget.Check();
            return new OASISResult<IReadOnlyList<CosmicGroupedRow>>(rows.AsReadOnly()) { IsLoaded = true };
        }
        catch (NeoCqlGroupedException cause) { return GroupedCqlFailure(cause.Code, cause.Message, cause); }
        catch (NeoCqlLimitException cause) { return GroupedCqlFailure("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NeoCqlTimeoutException cause) { return GroupedCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Neo4jException cause) when (cause.Code == "Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code == "Neo.TransientError.Transaction.TransactionTimedOut" || cause.Code == "Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        { return GroupedCqlFailure(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (OperationCanceledException cause) { return GroupedCqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (NotSupportedException cause) { return GroupedCqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OverflowException cause) { return GroupedCqlFailure("CQL_DATA_INVALID", "Grouped scalar exceeds its declared CLR domain.", cause); }
        catch (InvalidDataException cause) { return GroupedCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (Exception cause) { return GroupedCqlFailure("CQL_PROVIDER_ERROR", cause.Message, cause); }
    }

    private static OASISResult<IReadOnlyList<CosmicGroupedRow>> GroupedCqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
