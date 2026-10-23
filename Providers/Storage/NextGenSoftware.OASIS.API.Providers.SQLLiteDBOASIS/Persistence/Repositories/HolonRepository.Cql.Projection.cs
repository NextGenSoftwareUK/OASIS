using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    private static readonly Encoding ProjectionUtf8 = new UTF8Encoding(false, true);

    public async Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admission = CosmicProjectionPlanner.Admit(plan, ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlProjectionCapabilities);
            if (admission.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicProjectionRow>>(admission);
            if (maximumResults < 0 || maximumResults > CosmicProjectionResults.MaximumResultLimit
                || maximumPayloadBytes < 0 || maximumPayloadBytes > CosmicProjectionResults.MaximumPayloadLimit)
                return ProjectionCqlError("CQL_QUERY_LIMIT", "Projection output bounds are invalid.");
            var source = plan.Query.Source;
            var translator = new CqlSqlTranslator(source.ReadAccess);
            var predicate = source.Predicates.Count == 0 ? "1" : string.Join(" AND ", source.Predicates.Select(translator.Translate));
            var ordering = translator.TranslateOrdering(source.Orderings);
            var columns = translator.TranslateProjection(plan.Query.Columns);
            var fetch = source.Limit.HasValue ? Math.Min(source.Limit.Value, maximumResults + 1) : maximumResults + 1;
            translator.Parameters.Add(new SqliteParameter("$projectionLimit", fetch));
            translator.Parameters.Add(new SqliteParameter("$projectionOffset", source.Offset));
            cancellationToken.ThrowIfCancellationRequested();
            if (fetch == 0) return new OASISResult<IReadOnlyList<CosmicProjectionRow>>(Array.Empty<CosmicProjectionRow>()) { IsLoaded = true };

            using var exclusive = _dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget = new SqliteCqlReadBudget(connection, cancellationToken);
                using var traversal = translator.HasRelationship ? new SqliteCqlTraversalGuard(connection, budget.Check) : null;
                var needsSnapshot = source.GraphOptions != null || translator.HasRelationship;
                if (needsSnapshot && _dbContext.Database.CurrentTransaction != null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return ProjectionCqlError("CQL_CONSISTENCY_UNSUPPORTED", "SQLite projection scope requires a serializable read owner.");
                using var transaction = needsSnapshot && _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
                    budget.Check();
                    // Mandatory scope validation must finish before the source
                    // window can hide another root's malformed traversal.
                    if (translator.HasRelationship)
                    {
                        await _dbContext.Database.SqlQueryRaw<long>("SELECT count(*) AS Value FROM Holon WHERE " + predicate,
                            translator.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
                        budget.Check(); traversal.Check();
                    }
                    using var command = connection.CreateCommand();
                    command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
                    command.CommandText = "/* COSMIC projection */ SELECT " + columns + " FROM Holon WHERE " + predicate
                        + " ORDER BY " + ordering + " LIMIT $projectionLimit OFFSET $projectionOffset";
                    foreach (var parameter in translator.Parameters)
                        command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.SqliteType) { Value = parameter.Value });
                    var rows = new List<CosmicProjectionRow>();
                    long bytes = 0;
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            budget.Check(); traversal?.Check();
                            if (rows.Count >= maximumResults) throw new SqliteCqlResourceException("Projection exceeds its declared result count.");
                            var cells = new object[plan.Query.Columns.Count];
                            bytes += 16;
                            for (var column = 0; column < cells.Length; column++)
                            {
                                budget.Check();
                                var state = reader.GetInt64(column * 2);
                                var declared = plan.Query.Columns[column].Path.ValueType;
                                if (state == 3 || state is < 0 or > 3)
                                    throw new InvalidDataException("Selected metadata path is ambiguous or has the wrong native domain.");
                                cells[column] = state == 0 || state == 1 ? null
                                    : DecodeProjectionCell(reader.GetValue(column * 2 + 1), declared);
                                if (cells[column] == null && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                    throw new InvalidDataException("A selected non-nullable projection value is missing or null.");
                                bytes += 16 + (cells[column] is string text ? ProjectionUtf8.GetByteCount(text) : 16);
                                if (bytes > maximumPayloadBytes) throw new SqliteCqlResourceException("Projection exceeds its detached payload budget.");
                            }
                            rows.Add(new CosmicProjectionRow(cells));
                        }
                    }
                    budget.Check(); traversal?.Check(); transaction?.Commit(); budget.Check();
                    return new OASISResult<IReadOnlyList<CosmicProjectionRow>>(rows.AsReadOnly()) { IsLoaded = true };
                }
                catch (SqliteException cause) when (cause.SqliteErrorCode == 9) { budget.Check(); throw; }
            }
            finally { if (opened) await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false); }
        }
        catch (OverflowException cause) { return ProjectionCqlError("CQL_DATA_INVALID", "Selected native scalar exceeds its declared CLR domain.", cause); }
        catch (InvalidDataException cause) { return ProjectionCqlError("CQL_DATA_INVALID", cause.Message, cause); }
        catch (SqliteCqlDeadlineException cause) { return ProjectionCqlError("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (SqliteCqlResourceException cause) { return ProjectionCqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (SqliteException cause) when (cause.SqliteErrorCode == 18) { return ProjectionCqlError("CQL_QUERY_LIMIT", "Projection exceeds native record-size budget.", cause); }
        catch (CqlTranslationLimitException cause) { return ProjectionCqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return ProjectionCqlError("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ProjectionCqlError("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (Exception cause) { return ProjectionCqlError("CQL_QUERY_FAILED", "SQLite projection did not complete.", cause); }
    }

    private static object DecodeProjectionCell(object value, Type declared)
    {
        var type = Nullable.GetUnderlyingType(declared) ?? declared;
        if (type == typeof(string) && value is string text) return text;
        if (type == typeof(Guid) && value is string guid && Guid.TryParseExact(guid, "D", out var id)) return id;
        if (type == typeof(DateTime) && value is string date && DateTime.TryParseExact(date,
            new[] { "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'" }, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var instant)) return instant;
        if (value is long integer)
        {
            if (type == typeof(bool) && integer is 0 or 1) return integer == 1;
            if (type.IsEnum && Enum.IsDefined(type, Convert.ChangeType(integer, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture)))
                return Enum.ToObject(type, integer);
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(uint) || type == typeof(int) || type == typeof(long))
                return Convert.ChangeType(integer, type, CultureInfo.InvariantCulture);
        }
        throw new InvalidDataException("Selected native scalar does not match its declared CLR domain.");
    }

    private static OASISResult<IReadOnlyList<CosmicProjectionRow>> ProjectionCqlError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
