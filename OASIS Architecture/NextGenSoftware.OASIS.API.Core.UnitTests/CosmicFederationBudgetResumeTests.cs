using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicFederationBudgetResumeTests
{
    private static readonly ProviderType Mongo = ProviderType.MongoDBOASIS;
    private static readonly ProviderType Sqlite = ProviderType.SQLLiteDBOASIS;

    [Fact]
    public void ResumePreservesEveryConsumedDimensionAndProviderVector()
    {
        var options = CosmicFederationOptions.Create(maximumInputRows: 10,
            maximumRowsPerProvider: 6, maximumInputBytes: 100, maximumOutputBytes: 80,
            maximumWork: 50, maximumProviderAttempts: 8).Result;
        var first = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default);
        first.ReserveAttempt(Mongo);
        first.ChargeInput(Mongo, 3, 20);
        first.ChargeInput(Sqlite, 1, 10);
        first.ChargeOutput(7);
        first.ChargeWork(5);
        var state = first.CaptureRemaining();
        Assert.Equal(6, state.InputRows);
        Assert.Equal(70, state.InputBytes);
        Assert.Equal(73, state.OutputBytes);
        Assert.Equal(45, state.Work);
        Assert.Equal(7, state.Attempts);
        Assert.Equal(3, state.ProviderRows[Mongo]);
        Assert.Equal(5, state.ProviderRows[Sqlite]);
        var second = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default, state);
        second.ChargeInput(Mongo, 3, 10);
        var next = second.CaptureRemaining();
        Assert.Equal(0, next.ProviderRows[Mongo]);
        Assert.Equal(5, next.ProviderRows[Sqlite]);
        Assert.Equal(3, next.InputRows);
        Assert.Equal(60, next.InputBytes);
        Assert.True(next.DurationMilliseconds <= state.DurationMilliseconds);
        Assert.Equal(3, state.ProviderRows[Mongo]);
        Assert.Equal("CQL_FEDERATION_INPUT_LIMIT", Assert.Throws<CosmicFederationBudgetException>(
            () => second.ChargeInput(Mongo, 1, 0)).Code);
        Assert.Throws<CosmicFederationBudgetException>(() => second.CaptureRemaining());
    }

    [Fact]
    public void ResumeCannotChangeScopeOrOriginalBudgetContract()
    {
        var options = CosmicFederationOptions.Create().Result;
        var first = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        var state = first.CaptureRemaining();
        Assert.Throws<ArgumentException>(() => new CosmicFederationExecutionBudget(options,
            new[] { Sqlite }, default, state));
        var changed = CosmicFederationOptions.Create(maximumWork: options.MaximumWork - 1).Result;
        Assert.Throws<ArgumentException>(() => new CosmicFederationExecutionBudget(changed,
            new[] { Mongo }, default, state));
    }

    [Fact]
    public void ZeroRemainingAttemptsCannotProduceAnotherProviderRead()
    {
        var options = CosmicFederationOptions.Create(maximumProviderAttempts: 1).Result;
        var first = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        first.ReserveAttempt(Mongo);
        var second = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default,
            first.CaptureRemaining());
        Assert.Equal("CQL_FEDERATION_ATTEMPT_LIMIT", Assert.Throws<CosmicFederationBudgetException>(
            () => second.ReserveAttempt(Mongo)).Code);
    }

    [Fact]
    public async Task ResumeExcludesClientThinkTimeButChargesPriorActiveTime()
    {
        var options = CosmicFederationOptions.Create(maximumDurationMilliseconds: 30000).Result;
        var first = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        await Task.Delay(20);
        var state = first.CaptureRemaining();
        Assert.InRange(state.DurationMilliseconds, 1, 29999);
        await Task.Delay(20);
        var second = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default, state);
        var next = second.CaptureRemaining();
        Assert.True(next.DurationMilliseconds <= state.DurationMilliseconds);
        Assert.True(next.DurationMilliseconds >= state.DurationMilliseconds - 1000);
    }

    [Theory]
    [InlineData("rows")]
    [InlineData("input-bytes")]
    [InlineData("output-bytes")]
    [InlineData("work")]
    [InlineData("duration")]
    public void ExhaustedDimensionRemainsExhaustedAfterResume(string dimension)
    {
        var options = CosmicFederationOptions.Create().Result;
        var first = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        if (dimension == "rows") first.ChargeInput(Mongo, options.MaximumRowsPerProvider, 0);
        if (dimension == "input-bytes") first.ChargeInput(Mongo, 0, options.MaximumInputBytes);
        if (dimension == "output-bytes") first.ChargeOutput(options.MaximumOutputBytes);
        if (dimension == "work") first.ChargeWork(options.MaximumWork);
        var state = first.CaptureRemaining();
        if (dimension == "duration")
            state = new CosmicFederationRemainingBudget(options, state.InputRows, state.InputBytes,
                state.OutputBytes, state.Work, state.Attempts, 0,
                new Dictionary<ProviderType, int>(state.ProviderRows));
        var second = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default, state);
        var error = Assert.Throws<CosmicFederationBudgetException>(() =>
        {
            if (dimension == "rows") second.ChargeInput(Mongo, 1, 0);
            if (dimension == "input-bytes") second.ChargeInput(Mongo, 0, 1);
            if (dimension == "output-bytes") second.ChargeOutput(1);
            if (dimension == "work") second.ChargeWork(1);
            if (dimension == "duration") second.Checkpoint();
        });
        Assert.Equal(dimension switch
        {
            "rows" or "input-bytes" => "CQL_FEDERATION_INPUT_LIMIT",
            "output-bytes" => "CQL_FEDERATION_OUTPUT_LIMIT",
            "work" => "CQL_FEDERATION_WORK_LIMIT",
            _ => "CQL_FEDERATION_DEADLINE"
        }, error.Code);
    }

    [Fact]
    public void InconsistentGlobalAndProviderAccountingCannotResume()
    {
        var options = CosmicFederationOptions.Create().Result;
        var state = new CosmicFederationRemainingBudget(options, options.MaximumInputRows - 1,
            options.MaximumInputBytes, options.MaximumOutputBytes, options.MaximumWork,
            options.MaximumProviderAttempts, options.MaximumDurationMilliseconds,
            new Dictionary<ProviderType, int> { [Mongo] = options.MaximumRowsPerProvider });
        Assert.Throws<ArgumentException>(() => new CosmicFederationExecutionBudget(options,
            new[] { Mongo }, default, state));
    }

    [Fact]
    public void CancelledExecutionCannotPublishAContinuationSnapshot()
    {
        using var cancellation = new CancellationTokenSource();
        var first = new CosmicFederationExecutionBudget(CosmicFederationOptions.Create().Result,
            new[] { Mongo }, cancellation.Token);
        cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED", Assert.Throws<CosmicFederationBudgetException>(
            () => first.CaptureRemaining()).Code);
    }
}
