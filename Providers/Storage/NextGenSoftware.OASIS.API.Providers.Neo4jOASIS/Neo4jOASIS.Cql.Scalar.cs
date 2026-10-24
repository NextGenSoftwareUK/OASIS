using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryScalarStorageProvider
{
    private static class ScalarContract
    {
        internal static readonly CosmicScalarCapabilities Capabilities = new(
            new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any },
            new[]
            {
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, CqlProjectionCapabilities)
            });
    }
    public static CosmicScalarCapabilities CqlScalarCapabilities => ScalarContract.Capabilities;
    public CosmicScalarCapabilities ScalarCapabilities => CqlScalarCapabilities;

    public async Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admission = CosmicScalarPlanner.Admit(plan, Core.Enums.ProviderType.Neo4jOASIS, CqlCapabilities, CqlScalarCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, TResult>(admission);
            var source = plan.Query.Source;
            var compiled = CompileCqlSource(source);
            cancellationToken.ThrowIfCancellationRequested();
            var numeric = plan.Query.Operation == CosmicScalarOperation.Sum || plan.Query.Operation == CosmicScalarOperation.Average;
            var selected = plan.Query.Selector != null;
            if (source.Limit == 0 && numeric) return CosmicNumericScalarResults.FromParts(plan.Query, 0, 0, 0);
            if (source.Limit == 0) return selected ? CosmicSelectedScalarResults.FromValue(plan.Query, null, false)
                : CosmicScalarResults.FromCount(plan.Query, 0);
            if (!IsProviderActivated || _driver == null) return ScalarCqlFailure<TResult>("CQL_PROVIDER_INACTIVE", "Neo4j provider is not activated.");

            var parameters = compiled.Translator.Parameters;
            parameters.Add("countOffset", (long)source.Offset);
            parameters.Add("countTake", source.Limit.HasValue ? (long)source.Limit.Value : long.MaxValue);
            var traversalError = compiled.Translator.TraversalClauses.Count == 0 ? "" :
                " UNION ALL " + compiled.EligibleSource + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
                + " RETURN 0 AS value,false AS valid," + compiled.Cycle + " AS traversalCycle,"
                + compiled.TraversalLimit + " AS traversalLimit LIMIT 1";
            var query = "CALL { " + compiled.Source
                + "WHERE NOT valid RETURN 0 AS value,false AS valid,false AS traversalCycle,false AS traversalLimit LIMIT 1"
                + traversalError + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate
                + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
                + "WITH count(*) AS total RETURN CASE WHEN total <= $countOffset THEN 0 "
                + "WHEN total-$countOffset > $countTake THEN $countTake ELSE total-$countOffset END AS value,"
                + "true AS valid,false AS traversalCycle,false AS traversalLimit } RETURN value,valid,traversalCycle,traversalLimit";

            if (numeric) query = CompileCqlNumeric(compiled, plan.Query);
            else if (selected) query = CompileCqlExtremum(compiled, plan.Query);
            // ReserveProjection includes 32 bytes of envelope overhead; the
            // selected scalar itself retains the common exact 32 MiB bound.
            var budget = new NeoCqlReadBudget(selected ? CosmicProjectionResults.MaximumPayloadLimit + 32 : 1024, cancellationToken);
            budget.Check();
            using var session = _driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor = await transaction.RunAsync(query, parameters).ConfigureAwait(false);
            var observed = false;
            long count = 0;
            object selectedValue = null;
            var hasSelectedValue = false;
            NeoCqlNumericValue numericValue = null;
            while (true)
            {
                budget.Check();
                if (!await cursor.FetchAsync().ConfigureAwait(false)) break;
                budget.Check();
                var row = cursor.Current;
                if (row["traversalCycle"].As<bool>()) throw new InvalidDataException("Neo4j scalar traversal encountered a cycle.");
                if (row["traversalLimit"].As<bool>()) throw new NeoCqlLimitException("Neo4j scalar traversal exceeds its visited-node budget.");
                if (!row["valid"].As<bool>()) throw new InvalidDataException("Neo4j scalar ownership, membership or selected-value domain is invalid.");
                if (observed) throw new InvalidDataException("Neo4j scalar returned multiple native values.");
                if (numeric)
                {
                    numericValue = DecodeCqlNumeric(row.Values);
                }
                else if (selected)
                {
                    if (row["payloadLimit"].As<bool>()) throw new NeoCqlLimitException("Selected scalar exceeds its native payload bound.");
                    if (row["present"] is not long present || present < 0)
                        throw new InvalidDataException("Selected scalar requires a nonnegative participating-value count.");
                    hasSelectedValue = present != 0;
                    if ((!hasSelectedValue && row["value"] != null) || (hasSelectedValue && row["value"] == null))
                        throw new InvalidDataException("Selected scalar value/presence disagree.");
                    if (hasSelectedValue)
                    {
                        selectedValue = DecodeNativeProjectionValue(row["value"],
                            Nullable.GetUnderlyingType(plan.Query.Selector.ValueType) ?? plan.Query.Selector.ValueType);
                        budget.ReserveProjection(new[] { selectedValue });
                    }
                }
                else
                {
                    if (row["value"] is not long value || value < 0)
                        throw new InvalidDataException("Neo4j scalar requires exactly one nonnegative Int64 result.");
                    count = value;
                }
                observed = true;
            }
            budget.Check();
            if (!observed) throw new InvalidDataException("Neo4j scalar reduction did not return its empty/nonempty result.");
            await transaction.CommitAsync().ConfigureAwait(false);
            budget.Check();
            if (numeric) return CosmicNumericScalarResults.FromParts(plan.Query, numericValue.High, numericValue.Low, numericValue.Count);
            return selected ? CosmicSelectedScalarResults.FromValue(plan.Query, selectedValue, hasSelectedValue)
                : CosmicScalarResults.FromCount(plan.Query, count);
        }
        catch (NeoCqlLimitException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NeoCqlTimeoutException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Neo4jException cause) when (cause.Code == "Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code == "Neo.TransientError.Transaction.TransactionTimedOut" || cause.Code == "Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        { return ScalarCqlFailure<TResult>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (NotSupportedException cause) { return ScalarCqlFailure<TResult>("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (InvalidDataException cause) { return ScalarCqlFailure<TResult>("CQL_DATA_INVALID", cause.Message, cause); }
        catch (Exception cause) { return ScalarCqlFailure<TResult>("CQL_PROVIDER_ERROR", cause.Message, cause); }
    }

    private static OASISResult<T> ScalarCqlFailure<T>(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
