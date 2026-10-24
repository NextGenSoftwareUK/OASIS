using System;
using System.Globalization;
using System.Numerics;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Constant-size aggregate packets are normalized inside SQLite's execution.
// No holon is hydrated and no client-side row filtering/sorting is performed.
internal sealed class SqliteCqlNumericKeys : IDisposable
{
    internal const string PartsFunction = "cosmic_cql_average_key";
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
            var type = mode switch { 0 => typeof(int), 1 => typeof(int?), 2 => typeof(long), _ => typeof(long?) };
            var result = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average,
                type, high, low, count);
            if (result.IsError)
            {
                // Preserve the typed outcome before publishing, even when SQL
                // excludes this group. SQLite otherwise wraps callback errors.
                failureCode ??= result.ErrorCode;
                failureMessage ??= result.Message;
                return null;
            }
            return result.Result == null ? null : Key(Convert.ToDecimal(result.Result, CultureInfo.InvariantCulture));
        }, isDeterministic: false);
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

    // Every Decimal is an integer on the common 10^-28 grid. A fixed-width
    // sign/complement encoding gives SQLite BINARY collation exact numeric order.
    internal static string Key(decimal value)
    {
        var bits = decimal.GetBits(value);
        var magnitude = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32)
            | (new BigInteger((uint)bits[2]) << 64);
        magnitude *= BigInteger.Pow(10, 28 - ((bits[3] >> 16) & 255));
        var negative = value < 0;
        if (negative) magnitude = BigInteger.Pow(10, 57) - 1 - magnitude;
        return (negative ? "0" : "1") + magnitude.ToString("D57", CultureInfo.InvariantCulture);
    }

    internal void Check()
    {
        if (budgetFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(budgetFailure).Throw();
        if (failureCode != null) throw new SqliteCqlNumericException(failureCode, failureMessage);
    }

    public void Dispose()
    {
        connection.CreateFunction<long, long, long, int, string>(PartsFunction, null);
    }
}

internal sealed class SqliteCqlNumericException : Exception
{
    internal string Code { get; }
    internal SqliteCqlNumericException(string code, string message) : base(message) { Code = code; }
}
