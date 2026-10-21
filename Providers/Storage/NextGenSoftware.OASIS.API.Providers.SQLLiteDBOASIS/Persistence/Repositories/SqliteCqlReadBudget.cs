using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

internal sealed class SqliteCqlReadBudget : IDisposable
{
    internal const int MaximumPayloadBytes=64*1024*1024;
    private const int Interval=1000;
    private readonly SqliteConnection connection;
    private readonly CancellationToken cancellation;
    private readonly Stopwatch elapsed=Stopwatch.StartNew();
    private readonly long maximumSteps;
    private readonly TimeSpan deadline;
    private readonly int previousLength;
    private readonly SQLitePCL.delegate_progress callback;
    private long steps;
    private int stopped;
    private bool disposed;
    internal SqliteCqlReadBudget(SqliteConnection connection,CancellationToken cancellation,
        long maximumSteps=50000000,TimeSpan? deadline=null)
    {
        if(connection.State!=System.Data.ConnectionState.Open)throw new InvalidOperationException("The query budget requires an open owned connection.");
        if(maximumSteps<1 || maximumSteps>50000000)throw new ArgumentOutOfRangeException(nameof(maximumSteps));
        this.deadline=deadline??TimeSpan.FromSeconds(30);
        if(this.deadline<TimeSpan.Zero || this.deadline>TimeSpan.FromSeconds(30))throw new ArgumentOutOfRangeException(nameof(deadline));
        this.connection=connection;this.cancellation=cancellation;this.maximumSteps=maximumSteps;
        previousLength=SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,-1);
        callback=_=> {
            steps+=Interval;
            if(cancellation.IsCancellationRequested)stopped=1;
            else if(elapsed.Elapsed>=this.deadline)stopped=2;
            else if(steps>=this.maximumSteps)stopped=3;
            return stopped==0?0:1;
        };
        SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,Math.Min(previousLength,MaximumPayloadBytes));
        try { SQLitePCL.raw.sqlite3_progress_handler(connection.Handle,Interval,callback,null); }
        catch
        {
            SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,previousLength);
            throw;
        }
    }
    internal void Check()
    {
        cancellation.ThrowIfCancellationRequested();
        if(stopped==2 || elapsed.Elapsed>=deadline)throw new SqliteCqlDeadlineException();
        if(stopped==3)throw new SqliteCqlResourceException("SQLite CQL exceeds its native instruction budget.");
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        try { SQLitePCL.raw.sqlite3_progress_handler(connection.Handle,0,null,null); }
        finally
        {
            SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,previousLength);
            GC.KeepAlive(callback);
        }
    }
}
internal sealed class SqliteCqlDeadlineException : Exception
{ internal SqliteCqlDeadlineException():base("SQLite CQL exceeded its native query deadline."){} }
internal sealed class SqliteCqlResourceException : Exception
{ internal SqliteCqlResourceException(string message):base(message){} }
