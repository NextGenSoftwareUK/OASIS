using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass]
public sealed class CosmosFeeModelReviewTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 18, 4, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static CosmosFeeModelReview Review(Clock clock) => CosmosFeeModelReview.ForWasmd0703("review-test", "isolated-chain",
        new string('A', 64), new string('B', 64), 10, 100, clock.Now.AddMinutes(-1), clock.Now.AddMinutes(1), TimeSpan.FromSeconds(15), clock);
    private static CosmosDeploymentObservation Observation(Clock clock) => new("isolated-chain", new string('A', 64), new string('B', 64),
        CosmosFeeModelReview.WasmdVersion, CosmosFeeModelReview.WasmdCommit, CosmosFeeModelReview.SdkVersion, 50, clock.Now);

    [TestMethod]
    public void ExactFreshReviewedObservationAccepted() { var clock = new Clock(); Review(clock).ValidateObservation(Observation(clock), 50); }

    [DataTestMethod]
    [DataRow(10L)] [DataRow(100L)]
    public void InclusiveHeightAndObservationAgeBoundariesAccepted(long originalHeight)
    {
        var clock = new Clock();
        Review(clock).ValidateObservation(Observation(clock) with { CommittedHeight = 100, ObservedAtUtc = clock.Now.AddSeconds(-15) }, originalHeight);
    }

    [TestMethod]
    public void ClockRollbackBeforeReviewRejected()
    {
        var clock = new Clock(); var review = Review(clock); var observation = Observation(clock);
        clock.Now = review.ReviewedAtUtc.AddTicks(-1);
        Assert.ThrowsException<InvalidDataException>(() => review.ValidateObservation(observation, 50));
    }

    [DataTestMethod]
    [DataRow("chain")] [DataRow("genesis")] [DataRow("binary")] [DataRow("version")] [DataRow("commit")] [DataRow("sdk")]
    [DataRow("old-height")] [DataRow("upgraded-height")] [DataRow("future-time")] [DataRow("stale-time")] [DataRow("before-review")]
    [DataRow("non-utc")]
    public void DeploymentDriftOrUntrustedTimeRejected(string field)
    {
        var clock = new Clock(); var review = Review(clock); var observation = Observation(clock);
        observation = field switch {
            "chain" => observation with { ChainId = "other-chain" }, "genesis" => observation with { GenesisSha256 = new string('C', 64) },
            "binary" => observation with { BinarySha256 = new string('C', 64) }, "version" => observation with { ApplicationVersion = "0.70.4" },
            "commit" => observation with { ApplicationCommit = new string('c', 40) }, "sdk" => observation with { SdkVersion = "v0.54.1" },
            "old-height" => observation with { CommittedHeight = 49 }, "upgraded-height" => observation with { CommittedHeight = 101 },
            "future-time" => observation with { ObservedAtUtc = clock.Now.AddTicks(1) }, "stale-time" => observation with { ObservedAtUtc = clock.Now.AddSeconds(-16) },
            "before-review" => observation with { ObservedAtUtc = clock.Now.AddMinutes(-2) }, "non-utc" => observation with { ObservedAtUtc = clock.Now.ToOffset(TimeSpan.FromHours(1)) },
            _ => throw new ArgumentOutOfRangeException(nameof(field)) };
        Assert.ThrowsException<InvalidDataException>(() => review.ValidateObservation(observation, 50));
    }

    [DataTestMethod]
    [DataRow(9L)] [DataRow(101L)] [DataRow(0L)]
    public void OriginalOutsideReviewedHeightRangeRejected(long height)
    {
        var clock = new Clock(); Assert.ThrowsException<InvalidDataException>(() => Review(clock).ValidateObservation(Observation(clock), height));
    }

    [TestMethod]
    public void ExpiryCheckedAfterObservationReturns()
    {
        var clock = new Clock(); var review = Review(clock); var observation = Observation(clock);
        clock.Now = review.ExpiresAtUtc;
        Assert.ThrowsException<InvalidDataException>(() => review.ValidateObservation(observation, 50));
    }

    [DataTestMethod]
    [DataRow("hash")] [DataRow("chain")] [DataRow("range")] [DataRow("expired")] [DataRow("future-review")]
    [DataRow("age-zero")] [DataRow("age-unbounded")]
    public void InvalidReviewConfigurationRejected(string field)
    {
        var clock = new Clock();
        Assert.ThrowsException<ArgumentException>(() => CosmosFeeModelReview.ForWasmd0703("review-test", field == "chain" ? "bad chain" : "isolated-chain",
            field == "hash" ? new string('a', 64) : new string('A', 64), new string('B', 64), 10, field == "range" ? 9 : 100,
            field == "future-review" ? clock.Now.AddSeconds(1) : clock.Now.AddMinutes(-1), field == "expired" ? clock.Now : clock.Now.AddMinutes(1),
            field == "age-zero" ? TimeSpan.Zero : field == "age-unbounded" ? TimeSpan.FromMinutes(6) : TimeSpan.FromSeconds(15), clock));
    }
}
