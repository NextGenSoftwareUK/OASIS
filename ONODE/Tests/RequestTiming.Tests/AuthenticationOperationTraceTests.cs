using Trace = NextGenSoftware.OASIS.API.Core.Helpers.AuthenticationOperationTrace;
using Xunit;

public sealed class AuthenticationOperationTraceTests
{
    [Fact]
    public async Task ChildContinuationsUpdateTheObservedHolderWithoutChangingEarlierSnapshots()
    {
        using var trace = Trace.Begin();
        Trace.Mark(Trace.AuthenticationStage.LoadingUsername);
        var earlier = trace.Snapshot;
        await Task.Run(() => Trace.MarkRouting(Trace.RoutingStage.ProviderOperation,
            "MongoDBOASIS", "LoadAvatarByUsername"));
        Assert.Equal(Trace.RoutingStage.NotStarted, earlier.Routing);
        Assert.Null(earlier.Provider);
        Assert.Equal(Trace.AuthenticationStage.LoadingUsername, trace.Snapshot.Authentication);
        Assert.Equal(Trace.RoutingStage.ProviderOperation, trace.Snapshot.Routing);
        Assert.Equal("MongoDBOASIS", trace.Snapshot.Provider);
        Trace.Mark(Trace.AuthenticationStage.LoadingEmail);
        Assert.Equal(Trace.RoutingStage.NotStarted, trace.Snapshot.Routing);
        Assert.Null(trace.Snapshot.Provider);
        Assert.Null(trace.Snapshot.Operation);
    }

    [Fact]
    public async Task ConcurrentRequestScopesDoNotReplaceEachOthersProgress()
    {
        using var parent = Trace.Begin();
        Trace.Mark(Trace.AuthenticationStage.WaitingForLoginScope);
        var bothReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        async Task Observe(Trace.AuthenticationStage stage, string provider)
        {
            using var own = Trace.Begin();
            Trace.Mark(stage);
            Trace.MarkRouting(Trace.RoutingStage.ProviderOperation, provider, "LoadAvatarByUsername");
            if (Interlocked.Increment(ref arrivals) == 2) bothReady.SetResult();
            await bothReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(stage, own.Snapshot.Authentication);
            Assert.Equal(provider, own.Snapshot.Provider);
        }
        await Task.WhenAll(Task.Run(() => Observe(Trace.AuthenticationStage.LoadingUsername, "MongoDBOASIS")),
            Task.Run(() => Observe(Trace.AuthenticationStage.LoadingEmail, "Neo4jOASIS")));
        Assert.Equal(Trace.AuthenticationStage.WaitingForLoginScope, parent.Snapshot.Authentication);
        Assert.Null(parent.Snapshot.Provider);
    }

    [Fact]
    public void NestedDisposalRestoresTheParentAndDisposedObserversStopChanging()
    {
        using var parent = Trace.Begin();
        Trace.Mark(Trace.AuthenticationStage.LoadingUsername);
        var child = Trace.Begin();
        Trace.Mark(Trace.AuthenticationStage.SavingSession);
        child.Dispose();
        child.Dispose();
        Trace.Mark(Trace.AuthenticationStage.LoadingEmail);
        Assert.Equal(Trace.AuthenticationStage.SavingSession, child.Snapshot.Authentication);
        Assert.Equal(Trace.AuthenticationStage.LoadingEmail, parent.Snapshot.Authentication);
    }

    [Fact]
    public void OutsideARequestTheReportsDoNotCreateAnObservation()
    {
        Assert.False(Trace.IsEnabled);
        Trace.Mark(Trace.AuthenticationStage.LoadingUsername);
        Trace.MarkRouting(Trace.RoutingStage.ProviderOperation, "MongoDBOASIS", "LoadAvatarByUsername");
        Assert.False(Trace.IsEnabled);
    }
}
