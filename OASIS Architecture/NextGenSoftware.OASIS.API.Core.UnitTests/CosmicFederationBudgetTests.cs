using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicFederationBudgetTests
{
    private static readonly ProviderType Mongo = ProviderType.MongoDBOASIS;
    private static readonly ProviderType Sqlite = ProviderType.SQLLiteDBOASIS;

    private static void Fails(string code, Action action)
        => Assert.Equal(code, Assert.Throws<CosmicFederationBudgetException>(action).Code);

    [Fact]
    public void InputCountersAreCumulativeAcrossTargetsAndFailureIsTerminal()
    {
        var options = CosmicFederationOptions.Create(maximumInputRows: 3,
            maximumRowsPerProvider: 2, maximumInputBytes: 10).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default);
        budget.ChargeInput(Mongo, 2, 6);
        budget.ChargeInput(Sqlite, 1, 4);
        Fails("CQL_FEDERATION_INPUT_LIMIT", () => budget.ChargeInput(Sqlite, 1, 0));
        Fails("CQL_FEDERATION_INPUT_LIMIT", budget.Checkpoint);
        Fails("CQL_FEDERATION_INPUT_LIMIT", () => budget.ReserveAttempt(Mongo));
    }

    [Fact]
    public void PerProviderRowsCannotBorrowAnotherProvidersUnusedAllowance()
    {
        var options = CosmicFederationOptions.Create(maximumRowsPerProvider: 1).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default);
        budget.ChargeInput(Mongo, 1, 0);
        Fails("CQL_FEDERATION_INPUT_LIMIT", () => budget.ChargeInput(Mongo, 1, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OversizedClaimsCannotOverflowAndResetBudgets(int dimension)
    {
        var options = CosmicFederationOptions.Create().Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        Fails(dimension == 0 ? "CQL_FEDERATION_INPUT_LIMIT"
                : dimension == 1 ? "CQL_FEDERATION_WORK_LIMIT" : "CQL_FEDERATION_OUTPUT_LIMIT",
            () => { if (dimension == 0) budget.ChargeInput(Mongo, 0, long.MaxValue);
                else if (dimension == 1) budget.ChargeWork(long.MaxValue);
                else budget.ChargeOutput(long.MaxValue); });
    }

    [Fact]
    public void AttemptWorkAndOutputAllowExactBoundsButNeverReset()
    {
        var options = CosmicFederationOptions.Create(maximumProviderAttempts: 2,
            maximumWork: 2, maximumOutputBytes: 2).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default);
        budget.ReserveAttempt(Mongo);
        budget.ReserveAttempt(Sqlite);
        budget.ChargeWork(1);
        budget.ChargeWork(1);
        budget.ChargeOutput(1);
        budget.ChargeOutput(1);
        budget.Checkpoint();
        Fails("CQL_FEDERATION_ATTEMPT_LIMIT", () => budget.ReserveAttempt(Mongo));
    }

    [Fact]
    public void CancellationRemainsTerminalAcrossAllBudgetOperations()
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CosmicFederationExecutionBudget(CosmicFederationOptions.Create().Result,
            new[] { Mongo }, cancellation.Token);
        cancellation.Cancel();
        Fails("CQL_QUERY_CANCELLED", budget.Checkpoint);
        Fails("CQL_QUERY_CANCELLED", () => budget.ChargeOutput(0));
    }

    [Fact]
    public void UnknownTargetsAndNegativeProofsFailClosed()
    {
        var options = CosmicFederationOptions.Create().Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        Fails("CQL_FEDERATION_TARGET_INVALID", () => budget.ReserveAttempt(Sqlite));
        var other = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        Fails("CQL_FEDERATION_PROOF_INVALID", () => other.ChargeInput(Mongo, -1, 0));
        Assert.Throws<ArgumentException>(() => new CosmicFederationExecutionBudget(options,
            new[] { Mongo, Mongo }, default));
    }

    [Fact]
    public void ParallelChargesCannotOverspendTheSharedBudget()
    {
        var options = CosmicFederationOptions.Create(maximumWork: 10).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        var successes = 0;
        Parallel.For(0, 100, _ =>
        {
            try { budget.ChargeWork(1); Interlocked.Increment(ref successes); }
            catch (CosmicFederationBudgetException error) { Assert.Equal("CQL_FEDERATION_WORK_LIMIT", error.Code); }
        });
        Assert.Equal(10, successes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorkAndOutputAreCumulativeAtTheExactBoundary(bool output)
    {
        var options = CosmicFederationOptions.Create(maximumWork: 2, maximumOutputBytes: 2).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        void Charge(long value) { if (output) budget.ChargeOutput(value); else budget.ChargeWork(value); }
        Charge(1);
        Charge(1);
        Charge(0);
        Fails(output ? "CQL_FEDERATION_OUTPUT_LIMIT" : "CQL_FEDERATION_WORK_LIMIT", () => Charge(1));
    }

    [Fact]
    public void InputBytesAreSharedEvenWhenRowAllowanceRemains()
    {
        var options = CosmicFederationOptions.Create(maximumInputBytes: 2).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default);
        budget.ChargeInput(Mongo, 1, 2);
        Fails("CQL_FEDERATION_INPUT_LIMIT", () => budget.ChargeInput(Sqlite, 1, 1));
    }

    [Fact]
    public async Task DeadlineIncludesTimeBetweenProviderOperations()
    {
        var options = CosmicFederationOptions.Create(maximumDurationMilliseconds: 1).Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo }, default);
        await Task.Delay(20);
        Fails("CQL_FEDERATION_DEADLINE", budget.Checkpoint);
        Fails("CQL_FEDERATION_DEADLINE", () => budget.ReserveAttempt(Mongo));
    }
}
