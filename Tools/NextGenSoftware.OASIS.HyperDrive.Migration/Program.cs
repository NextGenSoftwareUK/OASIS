using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

return await HyperDriveMigrationProgram.RunAsync(args);

internal static class HyperDriveMigrationProgram
{
    public static async Task<int> RunAsync(string[] args)
        => await RunAsync(args, Console.Out, Console.Error, Environment.GetEnvironmentVariable,
            (connection, database) => new MongoDBOASIS(connection, database), CancellationToken.None)
            .ConfigureAwait(false);

    internal static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error,
        Func<string, string?> readEnvironment,
        Func<string, string, IHostedHyperDriveDomainBackfillStore> createStore,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> values;
        try { values = Parse(args); }
        catch (ArgumentException ex)
        {
            Write(output, false, "MONGO_BACKFILL_ARGUMENT_INVALID", ex.Message, null);
            return 2;
        }
        if (values.ContainsKey("help"))
        {
            WriteUsage(output);
            return 0;
        }
        if (!values.TryGetValue("database", out var database) || string.IsNullOrWhiteSpace(database))
        {
            WriteUsage(error);
            return 2;
        }
        string environmentVariable = values.TryGetValue("connection-env", out var configuredEnvironmentVariable)
            ? configuredEnvironmentVariable : "OASIS_MONGO_REPLICA_SET_CONNECTION";
        int batchSize = 250;
        if (values.TryGetValue("batch-size", out var configuredBatch) &&
            (!int.TryParse(configuredBatch, out batchSize) || batchSize < 1 || batchSize > 10000))
        {
            Write(output, false, "MONGO_BACKFILL_BATCH_INVALID", "--batch-size must be between 1 and 10000.", null);
            return 2;
        }
        string? connection = readEnvironment(environmentVariable);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Write(output, false, "MONGO_CONNECTION_REQUIRED",
                $"Environment variable '{environmentVariable}' does not contain a MongoDB replica-set connection string.", null);
            return 2;
        }

        try
        {
            IHostedHyperDriveDomainBackfillStore migration = createStore(connection, database);
            var result = await migration.BackfillDomainStateAsync(batchSize, cancellationToken).ConfigureAwait(false);
            Write(output, !result.IsError, result.ErrorCode, result.Message, result.Result);
            return result.IsError ? 1 : 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Write(output, false, "MONGO_BACKFILL_CANCELLED", "The hosted domain backfill was cancelled.", null);
            return 1;
        }
        catch (Exception ex)
        {
            Write(output, false, "MONGO_BACKFILL_UNHANDLED", ex.Message, null);
            return 1;
        }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument '{args[index]}'.");
            string key = args[index].Substring(2);
            if (key is not ("help" or "database" or "connection-env" or "batch-size"))
                throw new ArgumentException($"Unknown argument '--{key}'.");
            if (key == "help") { values[key] = "true"; continue; }
            if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Argument '--{key}' requires a value.");
            if (!values.TryAdd(key, args[index]))
                throw new ArgumentException($"Argument '--{key}' was specified more than once.");
        }
        return values;
    }

    private static void WriteUsage(TextWriter writer) => writer.WriteLine(
        "Usage: dotnet run --project Tools/NextGenSoftware.OASIS.HyperDrive.Migration -- --database <name> [--connection-env <environment-variable>] [--batch-size <1..10000>]");

    private static void Write(TextWriter writer, bool success, string? code, string? message,
        HostedDomainBackfillResult? result) => writer.WriteLine(JsonSerializer.Serialize(new
        {
            success,
            code,
            message,
            projectedCount = result?.ProjectedCount,
            rejectedCount = result?.RejectedCount,
            captureInitialized = result?.CaptureInitialized
        }));
}
