using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public partial class CqlHostedSourcesTests
{
    private static Task<OASISResult<CqlSourceRecoveryResult>> Recover(CqlHostedSources sources,
        ProviderType provider, Guid operation, long generation, CancellationToken token)
        => sources.RecoverReadSourceAsync(provider, operation, generation, sources.HostInstanceId, token);

    [Fact]
    public async Task CancellationCannotReleaseGateWhileNativeActivationStillOwnsItsCall()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        registry.Fail = false;
        registry.IgnoreCancellation = true;
        registry.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var pending = Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 1, cancellation.Token);
        cancellation.Cancel();
        Assert.False(pending.IsCompleted);
        Assert.Equal("CQL_SOURCE_RECOVERY_BUSY", (await Recover(sources,
            ProviderType.ActivityPubOASIS, Guid.NewGuid(), 1, default)).ErrorCode);
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
        registry.Release.SetResult();
        var completed = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("CQL_SOURCE_RECOVERY_CANCELLED", completed.ErrorCode);
        Assert.Equal(2, registry.Activations.Count);
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
    }

    [Fact]
    public async Task HostShutdownCancelsTheSameOwningRecoveryWithoutReplacement()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        registry.Fail = false;
        registry.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 1, default);
        await sources.StopAsync(default);
        var completed = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("CQL_SOURCE_RECOVERY_CANCELLED", completed.ErrorCode);
        Assert.Equal(2, registry.Activations.Count);
    }

    [Fact]
    public void RecoveryRequestRejectsUnspecifiedOptions()
        => Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<CqlSourceRecoveryRequest>(
            "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"expectedGeneration\":1,\"hostInstanceId\":\"00000000-0000-0000-0000-000000000002\",\"retryForever\":true}"));

    [Fact]
    public async Task PreviousHostRequestCannotStartFreshActivationAfterRestart()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var oldHost = Sources(registry);
        using var newHost = Sources(registry);
        await newHost.StartAsync(default);
        await newHost.ExecuteTask!;
        var response = await newHost.RecoverReadSourceAsync(ProviderType.ActivityPubOASIS,
            Guid.NewGuid(), 1, oldHost.HostInstanceId, default);
        Assert.Equal("CQL_SOURCE_HOST_INSTANCE_CONFLICT", response.ErrorCode);
        Assert.Single(registry.Activations);
        Assert.Equal(newHost.HostInstanceId, newHost.Describe().Single(row => row.QueryOnly).ActivationHostInstance);
    }

    [Fact]
    public async Task WizardRouteUsesCapturedGenerationAndReturnsOwnedReceipt()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        registry.Fail = false;
        var controller = new CqlSourceRecoveryController(sources) { ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext() } };
        controller.HttpContext.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar {
            Id = Guid.NewGuid(), AvatarType = new EnumValue<AvatarType>(AvatarType.Wizard) };
        var operation = Guid.NewGuid();
        var result = Assert.IsType<OkObjectResult>(await controller.Recover("ActivityPubOASIS",
            new(operation, 1, sources.HostInstanceId)));
        var receipt = Assert.IsType<OASISResult<CqlSourceRecoveryResult>>(result.Value);
        Assert.Equal(operation, receipt.Result.OperationId);
        Assert.Equal(2, receipt.Result.Generation);
        Assert.True(receipt.Result.Available);
    }

    [Fact]
    public async Task ExplicitRecoveryRepairsOnlyFailedEnrolledSourceAndReplaysReceiptWithoutActivation()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.MongoDBOASIS, true);
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        var failed = sources.Describe().Single(row => row.QueryOnly);
        Assert.Equal(1, failed.ActivationGeneration);
        Assert.Equal("CQL_SOURCE_UNAVAILABLE", failed.ErrorCode);

        registry.Fail = false;
        var id = Guid.NewGuid();
        var recovered = await Recover(sources, ProviderType.ActivityPubOASIS, id, 1, default);
        Assert.False(recovered.IsError);
        Assert.Equal(2, recovered.Result.Generation);
        Assert.True(sources.Describe().Single(row => row.QueryOnly).Available);
        var replay = await Recover(sources, ProviderType.ActivityPubOASIS, id, 1, default);
        Assert.Equal(recovered.Result, replay.Result);
        Assert.Equal("CQL_SOURCE_OPERATION_CONFLICT", (await Recover(sources,
            ProviderType.ActivityPubOASIS, id, 2, default)).ErrorCode);
        Assert.Equal(2, registry.Activations.Count);
        Assert.All(registry.Activations, type => Assert.Equal(ProviderType.ActivityPubOASIS, type));
        var stale = await Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 1, default);
        Assert.Equal("CQL_SOURCE_GENERATION_CONFLICT", stale.ErrorCode);
        var healthy = await Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 2, default);
        Assert.Equal("CQL_SOURCE_RECOVERY_NOT_FAILED", healthy.ErrorCode);
        Assert.Equal(2, registry.Activations.Count);
    }

    [Fact]
    public async Task RecoveryCannotOverlapStartupOrAnotherRecoveryAndPendingAdapterIsNotReady()
    {
        var registry = new Registry { Fail = true, Release = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        registry.Add(ProviderType.ActivityPubOASIS, true);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await registry.Attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var startup = await Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 0, default);
        Assert.Equal("CQL_SOURCE_RECOVERY_BUSY", startup.ErrorCode);
        registry.Release.SetResult();
        await sources.ExecuteTask!;

        registry.Fail = false;
        registry.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovery = Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 1, default);
        Assert.False(recovery.IsCompleted);
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
        var concurrent = await Recover(sources, ProviderType.ActivityPubOASIS, Guid.NewGuid(), 1, default);
        Assert.Equal("CQL_SOURCE_RECOVERY_BUSY", concurrent.ErrorCode);
        Assert.Equal(2, registry.Activations.Count);
        registry.Release.SetResult();
        Assert.False((await recovery).IsError);
    }

    [Fact]
    public async Task FailedRecoveryKeepsSafeFailureAndSameOperationCannotRetryIt()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        var id = Guid.NewGuid();
        var failed = await Recover(sources, ProviderType.ActivityPubOASIS, id, 1, default);
        Assert.True(failed.IsError);
        Assert.Equal("CQL_SOURCE_UNAVAILABLE", failed.ErrorCode);
        Assert.Equal(2, failed.Result.Generation);
        registry.Fail = false;
        var replay = await Recover(sources, ProviderType.ActivityPubOASIS, id, 1, default);
        Assert.Equal(failed.Result, replay.Result);
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
        Assert.Equal(2, registry.Activations.Count);
        Assert.DoesNotContain("private-native-token", System.Text.Json.JsonSerializer.Serialize(replay));
        Assert.Null(replay.Exception);
    }

    [Fact]
    public async Task CancelledOwningRecoveryIsAwaitedAndFencedBeforeNextAttempt()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        registry.Fail = false;
        registry.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var id = Guid.NewGuid();
        var pending = Recover(sources, ProviderType.ActivityPubOASIS, id, 1, cancellation.Token);
        cancellation.Cancel();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("CQL_SOURCE_RECOVERY_CANCELLED", result.ErrorCode);
        Assert.Equal(2, result.Result.Generation);
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
        Assert.Equal(result.Result, (await Recover(sources,
            ProviderType.ActivityPubOASIS, id, 1, default)).Result);
        Assert.Equal(2, registry.Activations.Count);
    }

    [Fact]
    public async Task CanonicalStorageAndUnknownSourcesCannotBeRecoveredThroughReadOnlyRoute()
    {
        var registry = new Registry();
        registry.Add(ProviderType.MongoDBOASIS, true);
        using var sources = Sources(registry);
        foreach (var type in new[] { ProviderType.MongoDBOASIS, ProviderType.Default, ProviderType.None, ProviderType.NostrOASIS })
            Assert.Equal("CQL_SOURCE_RECOVERY_NOT_ENROLLED", (await Recover(sources, type, Guid.NewGuid(), 0, default)).ErrorCode);
        Assert.Empty(registry.Activations);
    }

    [Theory]
    [InlineData(false, 401)]
    [InlineData(true, 403)]
    public async Task RecoveryRequiresAuthenticatedWizardEvenWhenInvokedWithoutMvcFilter(bool authenticated, int status)
    {
        var controller = new CqlSourceRecoveryController(null) { ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext() } };
        if (authenticated)
            controller.HttpContext.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = Guid.NewGuid(), AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
        var result = Assert.IsAssignableFrom<ObjectResult>(await controller.Recover("NostrOASIS", new(Guid.NewGuid(), 1, Guid.NewGuid())));
        Assert.Equal(status, result.StatusCode);
    }
}
