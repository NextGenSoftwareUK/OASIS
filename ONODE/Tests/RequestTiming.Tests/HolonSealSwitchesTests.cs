using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal;
using Xunit;

public class HolonSealSwitchesTests
{
    [Fact]
    public void RefreshPublishesBothSwitchSetsAndRemovesOldEntries()
    {
        var switches = new HolonSealSwitches();
        var oldOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        Assert.True(switches.TryReplace(switches.Revision, new[] { oldOwner }, new[] { "old" }));
        Assert.True(switches.TryReplace(switches.Revision, new[] { newOwner }, new[] { "APP", "", null! }));
        Assert.False(switches.IsEnabled(oldOwner, "old"));
        Assert.True(switches.IsEnabled(newOwner, null!));
        Assert.True(switches.IsEnabled(Guid.Empty, "app"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RefreshCannotOverwriteNewerOwnerSetting(bool enabled)
    {
        var switches = new HolonSealSwitches();
        var owner = Guid.NewGuid();
        switches.SetOwner(owner, !enabled);
        var revision = switches.Revision;
        switches.SetOwner(owner, enabled);
        Assert.False(switches.TryReplace(revision, new[] { owner }, new[] { "stale-app" }));
        Assert.Equal(enabled, switches.OwnerEnabled(owner));
        Assert.False(switches.AppEnabled("stale-app"));
    }

    [Fact]
    public void FailedAppEnumerationDoesNotPublishOwners()
    {
        var switches = new HolonSealSwitches();
        var owner = Guid.NewGuid();
        switches.SetOwner(owner, true);
        var revision = switches.Revision;
        Assert.Throws<InvalidOperationException>(() => switches.TryReplace(revision, Array.Empty<Guid>(), BrokenApps()));
        Assert.True(switches.OwnerEnabled(owner));
        Assert.Equal(revision, switches.Revision);
    }

    [Fact]
    public void SameSnapshotCannotBePublishedTwice()
    {
        var switches = new HolonSealSwitches();
        var revision = switches.Revision;
        Assert.True(switches.TryReplace(revision, Array.Empty<Guid>(), new[] { "current" }));
        Assert.False(switches.TryReplace(revision, Array.Empty<Guid>(), new[] { "stale" }));
        Assert.True(switches.AppEnabled("current"));
        Assert.False(switches.AppEnabled("stale"));
    }

    private static IEnumerable<string> BrokenApps()
    {
        yield return "partial";
        throw new InvalidOperationException("Enumeration failed.");
    }
}
