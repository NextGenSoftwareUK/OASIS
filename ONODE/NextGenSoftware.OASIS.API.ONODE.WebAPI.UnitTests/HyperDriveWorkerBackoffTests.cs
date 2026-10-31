using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HyperDriveWorkerBackoffTests
{
    [Fact]
    public void IdleAndContendedWorkersAlwaysWaitAndStopIncreasingAtThirtySeconds()
    {
        var backoff = new HyperDriveWorkerBackoff();
        foreach (var seconds in new[] { 1, 2, 4, 8, 16, 30, 30, 30 })
            Assert.Equal(TimeSpan.FromSeconds(seconds), backoff.NextDelay(false));
    }

    [Fact]
    public void UsefulWorkRunsWithoutPollingDelayAndResetsIdleBackoff()
    {
        var backoff = new HyperDriveWorkerBackoff();
        for (var i = 0; i < 10; i++) backoff.NextDelay(false);
        Assert.Equal(TimeSpan.Zero, backoff.NextDelay(true));
        Assert.Equal(TimeSpan.FromSeconds(1), backoff.NextDelay(false));
    }
}
