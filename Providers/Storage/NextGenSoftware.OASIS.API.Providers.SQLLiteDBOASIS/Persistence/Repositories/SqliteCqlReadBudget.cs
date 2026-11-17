using System;
using System.Diagnostics;
using System.Threading;
using System.Text;
using Microsoft.Data.Sqlite;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

internal sealed class SqliteCqlReadBudget : IDisposable
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private const string Utf8Function = "cosmic_cql_utf8_valid";
    internal const string StringLengthFunction = "cosmic_cql_utf16_length";
    internal const string TextStringLengthFunction = "cosmic_cql_text_utf16_length";
    internal const string TextStringMatchFunction = "cosmic_cql_text_ordinal_match";
    private static bool ValidUtf8(byte[] bytes)
    {
        if (bytes == null) return false;
        try { StrictUtf8.GetCharCount(bytes); return true; }
        catch (DecoderFallbackException) { return false; } // Native guard records invalid data, not replacement text.
    }
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
    private Exception textFailure;
    private readonly SQLitePCL.delegate_function_scalar textLengthCallback;
    private readonly SQLitePCL.delegate_function_scalar textMatchCallback;

    // value_text ensures UTF-8, but its wrapper is NUL-terminated. Read the
    // complete byte span after conversion, before performing any other conversion.
    private static string CompleteText(SQLitePCL.sqlite3_value value)
    {
        if (SQLitePCL.raw.sqlite3_value_type(value) == SQLitePCL.raw.SQLITE_NULL) return null;
        if (SQLitePCL.raw.sqlite3_value_type(value) != SQLitePCL.raw.SQLITE_TEXT)
            throw new ArgumentException("A native text function requires a text argument.");
        SQLitePCL.raw.sqlite3_value_text(value);
        return StrictUtf8.GetString(SQLitePCL.raw.sqlite3_value_blob(value));
    }

    private void NativeText(SQLitePCL.sqlite3_context context, SQLitePCL.sqlite3_value[] values, bool match)
    {
        try
        {
            Check();
            var text = CompleteText(values[0]);
            long? result = match
                ? (TextStringMatch(text, CompleteText(values[1]), SQLitePCL.raw.sqlite3_value_int64(values[2])) ? 1L : 0L)
                : text == null ? null : (long)text.Length;
            Check();
            if (result.HasValue) SQLitePCL.raw.sqlite3_result_int64(context, result.Value);
            else SQLitePCL.raw.sqlite3_result_null(context);
        }
        catch (Exception cause) when (cause is ArgumentException or OperationCanceledException
            or SqliteCqlDeadlineException or SqliteCqlResourceException)
        {
            textFailure = cause;
            SQLitePCL.raw.sqlite3_result_error(context, "Native CQL text evaluation failed.");
        }
    }

    private void SetTextFunction(string name, int arity, SQLitePCL.delegate_function_scalar function)
    {
        var code = SQLitePCL.raw.sqlite3_create_function(connection.Handle, name, arity, 0, null, function);
        if (code != SQLitePCL.raw.SQLITE_OK)
            throw new SqliteException("Native CQL text function registration failed.", code);
    }

    private bool TextStringMatch(string text, string value, long operation)
    {
        Check();
        var match = text != null && value != null && ((NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeRecordFunction)operation switch
        {
            NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeRecordFunction.Contains => text.IndexOf(value, StringComparison.Ordinal) >= 0,
            NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeRecordFunction.StartsWith => text.StartsWith(value, StringComparison.Ordinal),
            NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeRecordFunction.EndsWith => text.EndsWith(value, StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        });
        Check();
        return match;
    }

    private long? Utf16Length(byte[] bytes)
    {
        Check();
        var units = bytes == null ? (long?)null : StrictUtf8.GetCharCount(bytes);
        Check();
        return units;
    }

    internal SqliteCqlReadBudget(SqliteConnection connection,CancellationToken cancellation,
        long maximumSteps=50000000,TimeSpan? deadline=null)
    {
        if(connection.State!=System.Data.ConnectionState.Open)throw new InvalidOperationException("The query budget requires an open owned connection.");
        if(maximumSteps<1 || maximumSteps>50000000)throw new ArgumentOutOfRangeException(nameof(maximumSteps));
        this.deadline=deadline??TimeSpan.FromSeconds(30);
        if(this.deadline<TimeSpan.Zero || this.deadline>TimeSpan.FromSeconds(30))throw new ArgumentOutOfRangeException(nameof(deadline));
        this.connection=connection;this.cancellation=cancellation;this.maximumSteps=maximumSteps;
        textLengthCallback = (context, state, values) => NativeText(context, values, false);
        textMatchCallback = (context, state, values) => NativeText(context, values, true);
        previousLength=SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,-1);
        callback=_=> {
            steps+=Interval;
            if(cancellation.IsCancellationRequested)stopped=1;
            else if(elapsed.Elapsed>=this.deadline)stopped=2;
            else if(steps>=this.maximumSteps)stopped=3;
            return stopped==0?0:1;
        };
        SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,Math.Min(previousLength,MaximumPayloadBytes));
        try
        {
            SQLitePCL.raw.sqlite3_progress_handler(connection.Handle,Interval,callback,null);
            connection.CreateFunction<byte[], bool>(Utf8Function, ValidUtf8, isDeterministic: true);
            connection.CreateFunction<byte[], long?>(StringLengthFunction, Utf16Length, isDeterministic: false);
            SetTextFunction(TextStringLengthFunction, 1, textLengthCallback);
            SetTextFunction(TextStringMatchFunction, 3, textMatchCallback);
            connection.CreateFunction<string, string>(SqliteCqlDecimal.Function, DecimalKey, isDeterministic: false);
            connection.CreateFunction<string, string>(SqliteCqlFloating.DoubleFunction, DoubleKey, isDeterministic: false);
            connection.CreateFunction<string, string>(SqliteCqlFloating.SingleFunction, SingleKey, isDeterministic: false);
        }
        catch
        {
            connection.CreateFunction<byte[], bool>(Utf8Function, null);
            connection.CreateFunction<byte[], long?>(StringLengthFunction, null);
            SetTextFunction(TextStringLengthFunction, 1, null);
            SetTextFunction(TextStringMatchFunction, 3, null);
            connection.CreateFunction<string, string>(SqliteCqlDecimal.Function, null);
            connection.CreateFunction<string, string>(SqliteCqlFloating.DoubleFunction, null);
            connection.CreateFunction<string, string>(SqliteCqlFloating.SingleFunction, null);
            SQLitePCL.raw.sqlite3_progress_handler(connection.Handle,0,null,null);
            SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,previousLength);
            throw;
        }
    }
    private string DecimalKey(string json) => NumericKey(json, SqliteCqlDecimal.Key);
    private string SingleKey(string json) => NumericKey(json, SqliteCqlFloating.SingleKey);
    private string DoubleKey(string json) => NumericKey(json, SqliteCqlFloating.DoubleKey);
    private string NumericKey(string json, Func<string, string> encode)
    {
        try
        {
            Check();
            if (json != null && json.Length > SqliteCqlDecimal.MaximumTokenCharacters)
            { stopped = 3; return null; }
            var key = encode(json);
            Check();
            return key;
        }
        catch (OperationCanceledException) { stopped = 1; return null; }
        catch (SqliteCqlDeadlineException) { stopped = 2; return null; }
        catch (SqliteCqlResourceException) { stopped = 3; return null; }
    }
    internal void Check()
    {
        cancellation.ThrowIfCancellationRequested();
        if (textFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(textFailure).Throw();
        if(stopped==2 || elapsed.Elapsed>=deadline)throw new SqliteCqlDeadlineException();
        if(stopped==3)throw new SqliteCqlResourceException("SQLite CQL exceeds its native instruction budget.");
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        try
        {
            connection.CreateFunction<byte[], bool>(Utf8Function, null);
            connection.CreateFunction<byte[], long?>(StringLengthFunction, null);
            SetTextFunction(TextStringLengthFunction, 1, null);
            SetTextFunction(TextStringMatchFunction, 3, null);
            connection.CreateFunction<string, string>(SqliteCqlDecimal.Function, null);
            connection.CreateFunction<string, string>(SqliteCqlFloating.DoubleFunction, null);
            connection.CreateFunction<string, string>(SqliteCqlFloating.SingleFunction, null);
        }
        finally
        {
            try { SQLitePCL.raw.sqlite3_progress_handler(connection.Handle,0,null,null); }
            finally { SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,previousLength); GC.KeepAlive(callback); }
        }
    }
}
internal sealed class SqliteCqlDeadlineException : Exception
{ internal SqliteCqlDeadlineException():base("SQLite CQL exceeded its native query deadline."){} }
internal sealed class SqliteCqlResourceException : Exception
{ internal SqliteCqlResourceException(string message):base(message){} }
