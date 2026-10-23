using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public async Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admission = CosmicScalarPlanner.Admit(plan, ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlScalarCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, TResult>(admission);
            var source = plan.Query.Source;
            var translator = new CqlSqlTranslator(source.ReadAccess);
            var predicate = source.Predicates.Count == 0 ? "1" : string.Join(" AND ", source.Predicates.Select(translator.Translate));
            cancellationToken.ThrowIfCancellationRequested();
            if (source.Limit == 0) return CosmicScalarResults.FromCount(plan.Query, 0);

            // Count cardinality is independent of ordering. Count the complete
            // eligible source first so a page/Any cannot hide a traversal error,
            // then apply the immutable window with exact native integer math.
            translator.Parameters.Add(new SqliteParameter("$countOffset", (long)source.Offset));
            translator.Parameters.Add(new SqliteParameter("$countTake", source.Limit.HasValue ? (long)source.Limit.Value : long.MaxValue));
            var sql = "SELECT CASE WHEN total <= $countOffset THEN 0 ELSE min(total - $countOffset, $countTake) END AS Value "
                + "FROM (SELECT count(*) AS total FROM Holon WHERE " + predicate + ")";

            using var exclusive = _dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget = new SqliteCqlReadBudget(connection, cancellationToken);
                using var traversal = translator.HasRelationship ? new SqliteCqlTraversalGuard(connection, budget.Check) : null;
                var needsSnapshot = source.GraphOptions != null || translator.HasTraversal;
                if (needsSnapshot && _dbContext.Database.CurrentTransaction != null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return ScalarCqlError<TResult>("CQL_CONSISTENCY_UNSUPPORTED", "SQLite scalar graph/traversal intent requires a serializable read owner.");
                using var transaction = needsSnapshot && _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
                    budget.Check();
                    var count = await _dbContext.Database.SqlQueryRaw<long>(sql, translator.Parameters.Cast<object>().ToArray())
                        .SingleAsync(cancellationToken).ConfigureAwait(false);
                    budget.Check(); traversal?.Check();
                    transaction?.Commit();
                    budget.Check();
                    return CosmicScalarResults.FromCount(plan.Query, count);
                }
                catch (SqliteException cause) when (cause.SqliteErrorCode == 9) { budget.Check(); throw; }
            }
            finally
            {
                if (opened) await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
        catch (System.IO.InvalidDataException cause) { return ScalarCqlError<TResult>("CQL_DATA_INVALID", cause.Message, cause); }
        catch (SqliteCqlDeadlineException cause) { return ScalarCqlError<TResult>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (SqliteCqlResourceException cause) { return ScalarCqlError<TResult>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (SqliteException cause) when (cause.SqliteErrorCode == 18) { return ScalarCqlError<TResult>("CQL_QUERY_LIMIT", "SQLite scalar query exceeds its native record-size budget.", cause); }
        catch (CqlTranslationLimitException cause) { return ScalarCqlError<TResult>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return ScalarCqlError<TResult>("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ScalarCqlError<TResult>("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (Exception cause) { return ScalarCqlError<TResult>("CQL_QUERY_FAILED", "SQLite scalar execution failed: " + cause.Message, cause); }
    }

    private static OASISResult<T> ScalarCqlError<T>(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
