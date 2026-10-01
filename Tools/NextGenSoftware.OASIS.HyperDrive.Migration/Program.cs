using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

return await HyperDriveMigrationProgram.RunAsync(args);

internal static class HyperDriveMigrationProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        Dictionary<string, string> values;
        try { values = Parse(args); }
        catch (ArgumentException ex)
        {
            Write(false, "MONGO_BACKFILL_ARGUMENT_INVALID", ex.Message, null);
            return 2;
        }
        if (values.ContainsKey("help") || !values.TryGetValue("database", out var database) ||
            string.IsNullOrWhiteSpace(database))
        {
            Console.Error.WriteLine(
                "Usage: dotnet run --project Tools/NextGenSoftware.OASIS.HyperDrive.Migration -- --database <name> [--connection-env <environment-variable>] [--batch-size <1..10000>]");
            return 2;
        }
        string environmentVariable = values.TryGetValue("connection-env", out var configuredEnvironmentVariable)
            ? configuredEnvironmentVariable : "OASIS_MONGO_REPLICA_SET_CONNECTION";
        int batchSize = 250;
        if (values.TryGetValue("batch-size", out var configuredBatch) &&
            (!int.TryParse(configuredBatch, out batchSize) || batchSize < 1 || batchSize > 10000))
        {
            Write(false, "MONGO_BACKFILL_BATCH_INVALID", "--batch-size must be between 1 and 10000.", null);
            return 2;
        }
        string? connection = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Write(false, "MONGO_CONNECTION_REQUIRED",
                $"Environment variable '{environmentVariable}' does not contain a MongoDB replica-set connection string.", null);
            return 2;
        }

        try
        {
            var provider = new MongoDBOASIS(connection, database);
            IHostedHyperDriveDomainBackfillStore migration = provider;
            var result = await migration.BackfillDomainStateAsync(batchSize, CancellationToken.None);
            Write(!result.IsError, result.ErrorCode, result.Message, result.Result);
            return result.IsError ? 1 : 0;
        }
        catch (Exception ex)
        {
            Write(false, "MONGO_BACKFILL_UNHANDLED", ex.Message, null);
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
            if (key == "help") { values[key] = "true"; continue; }
            if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Argument '--{key}' requires a value.");
            if (!values.TryAdd(key, args[index]))
                throw new ArgumentException($"Argument '--{key}' was specified more than once.");
        }
        return values;
    }

    private static void Write(bool success, string? code, string? message,
        HostedDomainBackfillResult? result) => Console.WriteLine(JsonSerializer.Serialize(new
        {
            success,
            code,
            message,
            projectedCount = result?.ProjectedCount,
            rejectedCount = result?.RejectedCount,
            captureInitialized = result?.CaptureInitialized
        }));
}
