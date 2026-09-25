using NextGenSoftware.OASIS.Edge.Runtime;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class EdgeRuntimeNotificationStateMachineTests
{
    [Fact]
    public void OfflineReconnectAndDrainPublishExactlyOneSemanticNotificationEach()
    {
        var machine = new EdgeRuntimeNotificationStateMachine();

        var offline = Observe(machine, EdgeConnectivityState.Offline, EdgeSynchronizationState.Pending, 2);
        Assert.Equal(EdgeRuntimeNotificationKind.WorkingOffline, offline.Kind);
        Assert.Null(Observe(machine, EdgeConnectivityState.Offline, EdgeSynchronizationState.Pending, 3));

        var reconnect = Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronizing, 3);
        Assert.Equal(EdgeRuntimeNotificationKind.BackOnlineSynchronizing, reconnect.Kind);
        Assert.Null(Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronizing, 1));

        var complete = Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronized, 0);
        Assert.Equal(EdgeRuntimeNotificationKind.SynchronizationComplete, complete.Kind);
        Assert.Null(Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronized, 0));
    }

    [Fact]
    public void SynchronizedStatusWithPendingOperationsDoesNotClaimCompletion()
    {
        var machine = new EdgeRuntimeNotificationStateMachine();
        Observe(machine, EdgeConnectivityState.Offline, EdgeSynchronizationState.Pending, 1);
        Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronizing, 1);

        Assert.Null(Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronized, 1));
        Assert.Equal(EdgeRuntimeNotificationKind.SynchronizationComplete,
            Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronized, 0).Kind);
    }

    [Fact]
    public void InitialOnlineSynchronizationDoesNotPretendItRecoveredFromOffline()
    {
        var machine = new EdgeRuntimeNotificationStateMachine();
        Assert.Null(Observe(machine, EdgeConnectivityState.Connecting, EdgeSynchronizationState.Synchronizing, 0));
        Assert.Null(Observe(machine, EdgeConnectivityState.Online, EdgeSynchronizationState.Synchronized, 0));
    }

    private static EdgeRuntimeNotification Observe(EdgeRuntimeNotificationStateMachine machine,
        EdgeConnectivityState connectivity, EdgeSynchronizationState synchronization, long pending) =>
        machine.Observe(connectivity, synchronization, pending);
}
