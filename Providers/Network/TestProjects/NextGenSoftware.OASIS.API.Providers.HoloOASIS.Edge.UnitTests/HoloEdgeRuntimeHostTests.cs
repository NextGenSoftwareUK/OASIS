using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.UnitTests;

public sealed class HoloEdgeRuntimeHostTests
{
    [Fact]
    public async Task Start_CreatesRepositoryOnlyAfterAuthenticatedSessionConnects()
    {
        var bridge = new Bridge();
        var host = new HoloEdgeRuntimeHost(bridge, Options(), new Factory());

        var result = await host.StartAsync();

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(host.Repository);
        bridge.StopCount.Should().Be(0);
        await host.DisposeAsync();
        bridge.StopCount.Should().Be(1);
        bridge.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task ConnectFailure_StopsOwnedForegroundService()
    {
        var bridge = new Bridge();
        var host = new HoloEdgeRuntimeHost(bridge, Options(),
            new Factory { Result = ErrorClient("AUTH_REJECTED") });

        var result = await host.StartAsync();

        result.ErrorCode.Should().Be("AUTH_REJECTED");
        result.Result.Should().BeNull();
        bridge.StopCount.Should().Be(1);
        await host.DisposeAsync();
        bridge.StopCount.Should().Be(1);
    }

    [Fact]
    public async Task CleanupFailure_IsSurfacedInsteadOfHidingOrphanService()
    {
        var bridge = new Bridge { StopResult = ErrorBool("STOP_FAILED") };
        var host = new HoloEdgeRuntimeHost(bridge, Options(),
            new Factory { Result = ErrorClient("AUTH_REJECTED") });

        var result = await host.StartAsync();

        result.ErrorCode.Should().Be("HOLO_EDGE_CONNECT_CLEANUP_FAILED");
        result.Message.Should().Be("STOP_FAILED");
    }

    [Fact]
    public async Task SuspendAndResumeKeepStableRepositoryWhileReplacingSession()
    {
        var bridge = new Bridge();
        var host = new HoloEdgeRuntimeHost(bridge, Options(), new Factory());
        var started = await host.StartAsync();
        var repository = started.Result;

        (await host.SuspendAsync()).IsError.Should().BeFalse();
        var unavailable = await repository.ApplyLocalMutationAsync(new SyncOperation
        {
            OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
            EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
            PayloadJson = "{}"
        }, default);
        unavailable.ErrorCode.Should().Be("HOLO_EDGE_SESSION_UNAVAILABLE");

        var resumed = await host.ResumeAsync();
        resumed.Result.Should().BeSameAs(repository);
        bridge.SuspendCount.Should().Be(1);
        bridge.ResumeCount.Should().Be(1);
        await host.DisposeAsync();
    }

    private static HolochainAndroidRuntimeOptions Options() => new()
    {
        AppBundle = new byte[] { 1 }, InstalledAppId = "oasis"
    };

    private static OASISResult<IHoloEdgeAppClient> ErrorClient(string code) => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = code, Message = code
    };

    private static OASISResult<bool> ErrorBool(string code) => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = code, Message = code
    };

    private sealed class Factory : IHoloEdgeSessionFactory
    {
        public OASISResult<IHoloEdgeAppClient> Result { get; set; } =
            new(new AppClient());

        public Task<OASISResult<IHoloEdgeAppClient>> ConnectAsync(string installedAppId,
            HolochainAndroidAppSession session, CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }

    private sealed class Bridge : IHolochainAndroidServiceBridge
    {
        public int StopCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int SuspendCount { get; private set; }
        public int ResumeCount { get; private set; }
        public OASISResult<bool> StopResult { get; set; } = new(true);

        public Task<OASISResult<HolochainAndroidAppSession>> StartAndSetupAsync(
            HolochainAndroidRuntimeOptions options, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<HolochainAndroidAppSession>(new()
            {
                Port = 1234, AuthenticationToken = new byte[] { 2 }
            }));

        public Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Suspend());

        private OASISResult<bool> Suspend()
        {
            SuspendCount++;
            return new OASISResult<bool>(true);
        }

        public Task<OASISResult<HolochainAndroidAppSession>> ResumeAsync(
            string installedAppId, CancellationToken cancellationToken)
        {
            ResumeCount++;
            return StartAndSetupAsync(Options(), cancellationToken);
        }

        public Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            return Task.FromResult(StopResult);
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AppClient : IHoloEdgeAppClient
    {
        public Task<OASISResult<bool>> CallAsync(string zome, string function,
            IReadOnlyDictionary<string, object> payload, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<bool>(true));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
