using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;
using Xunit;

public class AccessLogOverridesTests
{
    [Fact]
    public void EmptySnapshotFollowsGlobalSetting()
    {
        var overrides = new AccessLogOverrides();
        Assert.Null(overrides.For("app"));
        Assert.Null(overrides.For(null!));
        Assert.False(overrides.AnyEnabled);
    }

    [Fact]
    public void SummaryUsesFinalCaseInsensitiveChoice()
    {
        var overrides = new AccessLogOverrides();
        overrides.Replace(new (string, bool?)[] { ("APP", true), ("app", false), ("inherited", null), ("", true) });
        Assert.False(overrides.For("APP"));
        Assert.Null(overrides.For("inherited"));
        Assert.False(overrides.AnyEnabled);
    }

    [Fact]
    public void CompleteReplacementRemovesStaleOptIns()
    {
        var overrides = new AccessLogOverrides();
        overrides.Replace(new (string, bool?)[] { ("old", true) });
        Assert.True(overrides.AnyEnabled);
        overrides.Replace(Array.Empty<(string, bool?)>());
        Assert.Null(overrides.For("old"));
        Assert.False(overrides.AnyEnabled);
    }

    [Fact]
    public void FailedEnumerationPreservesBothChoicesAndSummary()
    {
        var overrides = new AccessLogOverrides();
        overrides.Replace(new (string, bool?)[] { ("current", true) });
        Assert.Throws<InvalidOperationException>(() => overrides.Replace(BrokenChoices()));
        Assert.True(overrides.For("current"));
        Assert.True(overrides.AnyEnabled);
        Assert.Null(overrides.For("partial"));
    }

    private static IEnumerable<(string, bool?)> BrokenChoices()
    {
        yield return ("partial", false);
        throw new InvalidOperationException("Enumeration failed.");
    }
}
