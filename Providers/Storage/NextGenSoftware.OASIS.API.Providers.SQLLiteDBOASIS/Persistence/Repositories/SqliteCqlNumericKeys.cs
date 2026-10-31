using System;
using System.Globalization;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Constant-size aggregate packets are normalized inside SQLite's execution.
// No holon is hydrated and no client-side row filtering/sorting is performed.
internal sealed class SqliteCqlNumericKeys : IDisposable
{
    internal const string PartsFunction = "cosmic_cql_average_key";
    internal const string DecimalPartsFunction = "cosmic_cql_decimal_aggregate_key";
    private readonly SqliteConnection connection;
    private readonly Action checkBudget;
    private string failureCode, failureMessage;
    private Exception budgetFailure;

    internal SqliteCqlNumericKeys(SqliteConnection connection, Action checkBudget)
    {
        this.connection = connection;
        this.checkBudget = checkBudget;
        connection.CreateFunction<long, long, long, int, string>(PartsFunction, (high, low, count, mode) =>
        {
            if (!WithinBudget()) return null;
            var type = mode switch { 0 => typeof(int), 1 => typeof(int?), 2 => typeof(long), 3 => typeof(long?),
                4 => typeof(ulong), 5 => typeof(ulong?), _ => null };
            if (type == null)
            {
                failureCode ??= "CQL_DATA_INVALID";
                failureMessage ??= "Native average key requires a declared input domain.";
                return null;
            }
            var result = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average,
                type, high, low, count);
            return NormalizeKey(result);
        }, isDeterministic: false);
        connection.CreateFunction<long, long, long, long, long, long, long, long, int, string>(DecimalPartsFunction,
            (a, b, c, d, e, f, g, count, mode) =>
            {
                if (!WithinBudget()) return null;
                if (mode < 0 || mode > 3)
                {
                    failureCode ??= "CQL_DATA_INVALID";
                    failureMessage ??= "Native Decimal aggregate key requires a declared operation/input domain.";
                    return null;
                }
                return NormalizeKey(CosmicDecimalScalarResults.FromParts(
                    mode < 2 ? CosmicScalarOperation.Sum : CosmicScalarOperation.Average,
                    mode % 2 == 0 ? typeof(decimal) : typeof(decimal?), new[] { a, b, c, d, e, f, g }, count));
            }, isDeterministic: false);
    }

    private string NormalizeKey(OASISResult<object> result)
    {
        // Retain typed errors even when a filter or page excludes this group.
        // SQLite otherwise wraps callback exceptions as a generic SQL failure.
        if (result.IsError)
        {
            failureCode ??= result.ErrorCode;
            failureMessage ??= result.Message;
            return null;
        }
        return result.Result == null ? null : CosmicNumericScalarResults.DecimalOrderKey(Convert.ToDecimal(result.Result, CultureInfo.InvariantCulture));
    }

    private bool WithinBudget()
    {
        try { checkBudget(); return true; }
        catch (Exception cause) when (cause is OperationCanceledException || cause is SqliteCqlDeadlineException || cause is SqliteCqlResourceException)
        {
            budgetFailure ??= cause;
            return false;
        }
    }

    internal void Check()
    {
        if (budgetFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(budgetFailure).Throw();
        if (failureCode != null) throw new SqliteCqlNumericException(failureCode, failureMessage);
    }

    public void Dispose()
    {
        connection.CreateFunction<long, long, long, int, string>(PartsFunction, null);
        connection.CreateFunction<long, long, long, long, long, long, long, long, int, string>(DecimalPartsFunction, null);
    }
}

internal sealed class SqliteCqlNumericException : Exception
{
    internal string Code { get; }
    internal SqliteCqlNumericException(string code, string message) : base(message) { Code = code; }
}
