using System;
using System.Data;
using System.Runtime.ExceptionServices;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Runs inside the owned SQLite statement, in explicit aggregate ORDER BY order.
// Only the checked running total and overflow latch survive between inputs.
internal sealed class SqliteCqlCheckedInt64Average : IDisposable
{
    internal const string Function = "cosmic_cql_checked_int64_prefix";
    private readonly SqliteConnection connection;
    private readonly Action checkBudget;
    private Exception failure;
    private bool disposed;

    private sealed class State
    {
        internal long Sum;
        internal long Count;
        internal bool Overflow;
    }

    internal SqliteCqlCheckedInt64Average(SqliteConnection connection, Action checkBudget)
    {
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Checked Average requires an open owned connection.");
        this.connection = connection;
        this.checkBudget = checkBudget ?? throw new ArgumentNullException(nameof(checkBudget));
        connection.CreateAggregate<long?, State, long>(Function, seed: null,
            func: Accumulate, resultSelector: Finish, isDeterministic: false);
    }

    private State Accumulate(State state, long? value)
    {
        if (!WithinBudget() || !value.HasValue) return state;
        state ??= new State();
        if (state.Count >= CosmicNumericScalarResults.MaximumParticipatingValues)
        {
            failure ??= new SqliteCqlResourceException("Checked Average exceeds its participating input bound.");
            return state;
        }
        state.Count++;
        if (!state.Overflow)
        {
            try { state.Sum = checked(state.Sum + value.Value); }
            catch (OverflowException) { state.Overflow = true; }
        }
        return state;
    }

    private long Finish(State state)
    {
        WithinBudget();
        return state?.Overflow == true ? 1L : 0L;
    }

    private bool WithinBudget()
    {
        try { checkBudget(); return true; }
        catch (Exception cause) when (cause is OperationCanceledException
            || cause is SqliteCqlDeadlineException || cause is SqliteCqlResourceException)
        {
            failure ??= cause;
            return false;
        }
    }

    internal void Check()
    {
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        connection.CreateAggregate<long?, State, long>(Function, seed: null, func: null, resultSelector: null);
    }
}
