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
    public static CosmicScalarCapabilities CqlScalarCapabilities { get; } = new(
        CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any);
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
            if (source.Limit == 0) return CosmicScalarResults.FromCount(plan.Query, 0);
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

            var budget = new NeoCqlReadBudget(1024, cancellationToken);
            budget.Check();
            using var session = _driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor = await transaction.RunAsync(query, parameters).ConfigureAwait(false);
            var observed = false;
            long count = 0;
            while (true)
            {
                budget.Check();
                if (!await cursor.FetchAsync().ConfigureAwait(false)) break;
                budget.Check();
                var row = cursor.Current;
                if (row["traversalCycle"].As<bool>()) throw new InvalidDataException("Neo4j scalar traversal encountered a cycle.");
                if (row["traversalLimit"].As<bool>()) throw new NeoCqlLimitException("Neo4j scalar traversal exceeds its visited-node budget.");
                if (!row["valid"].As<bool>()) throw new InvalidDataException("Neo4j scalar projection/membership ownership is invalid.");
                if (observed || row["value"] is not long value || value < 0)
                    throw new InvalidDataException("Neo4j scalar requires exactly one nonnegative Int64 result.");
                observed = true; count = value;
            }
            budget.Check();
            if (!observed) throw new InvalidDataException("Neo4j scalar count did not return its empty/nonempty result.");
            await transaction.CommitAsync().ConfigureAwait(false);
            budget.Check();
            return CosmicScalarResults.FromCount(plan.Query, count);
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
