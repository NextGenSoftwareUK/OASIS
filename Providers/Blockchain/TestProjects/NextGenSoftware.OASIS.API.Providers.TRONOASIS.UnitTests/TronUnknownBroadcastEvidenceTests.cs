using System;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronUnknownBroadcastEvidenceTests
{
    [Fact]
    public void OriginalUnknownIdentityAndSignedWindowRemainStructuredWithoutGuessingOutcome()
    {
        var original = new TimeoutException("controlled broadcast interruption");
        var evidence = new TronBroadcastOutcomeUnknownException(new string('a', 64), new string('B', 64),
            1791812970000, 1791813030000, original);
        Assert.Equal(new string('a', 64), evidence.TransactionId);
        Assert.Equal(new string('b', 64), evidence.ChainGenesisId);
        Assert.Equal(1791812970000, evidence.TransactionTimestampMilliseconds);
        Assert.Equal(1791813030000, evidence.TransactionExpirationMilliseconds);
        Assert.Same(original, evidence.InnerException);
        Assert.True(evidence.HasOriginalValidityWindow);
        Assert.Contains("unknown", evidence.Message);
    }

    [Theory]
    [InlineData("invalid", 1, 2)]
    public void MalformedEvidenceCannotStandInForAnOriginalSubmission(string id, long timestamp, long expiry)
        => Assert.Throws<ArgumentException>(() => new TronBroadcastOutcomeUnknownException(id, new string('b', 64),
            timestamp, expiry, new TimeoutException()));

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 2)]
    public void MissingValidityEvidenceNeverMasksOriginalUnknownBroadcast(long timestamp, long expiry)
    {
        var original = new TimeoutException();
        var evidence = new TronBroadcastOutcomeUnknownException(new string('a', 64), new string('b', 64), timestamp, expiry, original);
        Assert.False(evidence.HasOriginalValidityWindow);
        Assert.Same(original, evidence.InnerException);
        Assert.Equal(timestamp, evidence.TransactionTimestampMilliseconds);
    }
}
