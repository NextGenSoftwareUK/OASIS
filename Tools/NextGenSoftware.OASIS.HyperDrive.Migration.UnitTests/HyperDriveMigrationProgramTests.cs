using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

public sealed class HyperDriveMigrationProgramTests
{
    [Fact]
    public async Task HelpReturnsSuccessWithoutOpeningAStore()
    {
        var output = new StringWriter();
        int opened = 0;

        int exitCode = await RunAsync(new[] { "--help" }, output, _ => null, (_, _) =>
        {
            opened++;
            return new FakeBackfillStore();
        });

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(0, opened);
    }

    [Theory]
    [InlineData("--unknown", "value", "MONGO_BACKFILL_ARGUMENT_INVALID")]
    [InlineData("--batch-size", "0", "MONGO_BACKFILL_BATCH_INVALID")]
    [InlineData("--batch-size", "10001", "MONGO_BACKFILL_BATCH_INVALID")]
    public async Task InvalidArgumentsFailBeforeOpeningAStore(string option, string value, string errorCode)
    {
        var output = new StringWriter();
        int opened = 0;

        int exitCode = await RunAsync(new[] { "--database", "oasis", option, value }, output,
            _ => "mongodb://unused", (_, _) =>
            {
                opened++;
                return new FakeBackfillStore();
            });

        Assert.Equal(2, exitCode);
        Assert.Equal(errorCode, Read(output).GetProperty("code").GetString());
        Assert.Equal(0, opened);
    }

    [Fact]
    public async Task MissingNamedConnectionFailsWithoutOpeningAStore()
    {
        var output = new StringWriter();
        string? requestedName = null;

        int exitCode = await RunAsync(
            new[] { "--database", "oasis", "--connection-env", "MONGO_RELEASE_URI" }, output,
            name => { requestedName = name; return null; }, (_, _) => throw new InvalidOperationException());

        Assert.Equal(2, exitCode);
        Assert.Equal("MONGO_RELEASE_URI", requestedName);
        Assert.Equal("MONGO_CONNECTION_REQUIRED", Read(output).GetProperty("code").GetString());
    }

    [Fact]
    public async Task SuccessfulBackfillUsesRequestedDatabaseBatchAndStructuredResult()
    {
        var output = new StringWriter();
        var store = new FakeBackfillStore(new OASISResult<HostedDomainBackfillResult>
        {
            IsSaved = true,
            Message = "completed",
            Result = new HostedDomainBackfillResult
            {
                ProjectedCount = 17,
                RejectedCount = 0,
                CaptureInitialized = true
            }
        });
        string? connection = null;
        string? database = null;

        int exitCode = await RunAsync(new[] { "--database", "oasis", "--batch-size", "37" }, output,
            _ => "mongodb://replica-set", (value, name) =>
            {
                connection = value;
                database = name;
                return store;
            });

        JsonElement json = Read(output);
        Assert.Equal(0, exitCode);
        Assert.Equal("mongodb://replica-set", connection);
        Assert.Equal("oasis", database);
        Assert.Equal(37, store.BatchSize);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal(17, json.GetProperty("projectedCount").GetInt32());
        Assert.True(json.GetProperty("captureInitialized").GetBoolean());
    }

    [Fact]
    public async Task BackfillErrorIsReturnedAsStructuredFailure()
    {
        var output = new StringWriter();
        var store = new FakeBackfillStore(new OASISResult<HostedDomainBackfillResult>
        {
            IsError = true,
            ErrorCode = "MONGO_DOMAIN_BACKFILL_REJECTED_DOCUMENTS",
            Message = "repair dead letters",
            Result = new HostedDomainBackfillResult { ProjectedCount = 3, RejectedCount = 1 }
        });

        int exitCode = await RunAsync(new[] { "--database", "oasis" }, output,
            _ => "mongodb://replica-set", (_, _) => store);

        JsonElement json = Read(output);
        Assert.Equal(1, exitCode);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal("MONGO_DOMAIN_BACKFILL_REJECTED_DOCUMENTS", json.GetProperty("code").GetString());
        Assert.Equal(1, json.GetProperty("rejectedCount").GetInt32());
    }

    private static Task<int> RunAsync(string[] args, StringWriter output,
        Func<string, string?> environment,
        Func<string, string, IHostedHyperDriveDomainBackfillStore> factory) =>
        HyperDriveMigrationProgram.RunAsync(args, output, new StringWriter(), environment, factory,
            CancellationToken.None);

    private static JsonElement Read(StringWriter output) =>
        JsonDocument.Parse(output.ToString()).RootElement.Clone();

    private sealed class FakeBackfillStore : IHostedHyperDriveDomainBackfillStore
    {
        private readonly OASISResult<HostedDomainBackfillResult> _result;

        public FakeBackfillStore(OASISResult<HostedDomainBackfillResult>? result = null) =>
            _result = result ?? new OASISResult<HostedDomainBackfillResult>
            {
                IsSaved = true,
                Result = new HostedDomainBackfillResult { CaptureInitialized = true }
            };

        public int BatchSize { get; private set; }

        public Task<OASISResult<HostedDomainBackfillResult>> BackfillDomainStateAsync(
            int batchSize, CancellationToken cancellationToken)
        {
            BatchSize = batchSize;
            return Task.FromResult(_result);
        }
    }
}
