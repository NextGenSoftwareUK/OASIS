using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class EdgeOfflineSessionManagerTests
{
    private readonly Guid _avatarId = Guid.NewGuid();
    private readonly Guid _deviceId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ValidatedDeviceBoundGrantCanBeCachedAndResumedForAuthorizedScope()
    {
        var store = new MemorySecureStore();
        var manager = CreateManager(store, new FixedValidator(true));
        var grant = NewGrant();

        (await manager.CacheAuthenticatedSessionAsync(grant)).IsError.Should().BeFalse();
        var resumed = await manager.ResumeAsync(new[] { "world.read", "quest.progress" });

        resumed.IsError.Should().BeFalse(resumed.Message);
        resumed.Result.Should().BeSameAs(grant);
        store.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task InvalidSignatureIsNeverWrittenToSecureStorage()
    {
        var store = new MemorySecureStore();
        var result = await CreateManager(store, new FixedValidator(false))
            .CacheAuthenticatedSessionAsync(NewGrant());

        result.ErrorCode.Should().Be("EDGE_OFFLINE_GRANT_SIGNATURE_INVALID");
        store.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task GrantForAnotherDeviceCannotResumeEvenWhenSignatureIsValid()
    {
        var store = new MemorySecureStore { Grant = NewGrant() };
        store.Grant.DeviceId = Guid.NewGuid();

        var result = await CreateManager(store, new FixedValidator(true)).ResumeAsync(Array.Empty<string>());

        result.ErrorCode.Should().Be("EDGE_OFFLINE_SESSION_REJECTED");
        result.Message.Should().Contain("avatar and device");
    }

    [Fact]
    public async Task ExpiredGrantCannotResume()
    {
        var store = new MemorySecureStore { Grant = NewGrant() };
        store.Grant.ExpiresUtc = _now;

        var result = await CreateManager(store, new FixedValidator(true)).ResumeAsync(Array.Empty<string>());

        result.ErrorCode.Should().Be("EDGE_OFFLINE_SESSION_REJECTED");
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task OfflineSessionCannotInventAnUncachedAuthorizationScope()
    {
        var store = new MemorySecureStore { Grant = NewGrant() };

        var result = await CreateManager(store, new FixedValidator(true)).ResumeAsync(new[] { "purchase.create" });

        result.ErrorCode.Should().Be("EDGE_OFFLINE_SESSION_REJECTED");
        result.Message.Should().Contain("purchase.create");
    }

    private EdgeOfflineSessionManager CreateManager(IEdgeSecureSessionStore store,
        IEdgeOfflineGrantValidator validator) =>
        new(_avatarId, _deviceId, store, validator, new FixedClock(_now));

    private HyperDriveOfflineSessionGrant NewGrant() => new()
    {
        GrantId = Guid.NewGuid().ToString("N"), AvatarId = _avatarId, DeviceId = _deviceId,
        IssuedUtc = _now.AddMinutes(-1), ExpiresUtc = _now.AddDays(7),
        Scopes = new[] { "world.read", "quest.progress" }, Signature = "server-signature"
    };

    private sealed class FixedClock : IHyperDriveClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FixedValidator : IEdgeOfflineGrantValidator
    {
        private readonly bool _valid;
        public FixedValidator(bool valid) => _valid = valid;
        public Task<OASISResult<bool>> ValidateAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken) => Task.FromResult(new OASISResult<bool>(_valid));
    }

    private sealed class MemorySecureStore : IEdgeSecureSessionStore
    {
        public HyperDriveOfflineSessionGrant? Grant { get; set; }
        public int SaveCalls { get; private set; }
        public Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant, CancellationToken cancellationToken)
        {
            SaveCalls++; Grant = grant;
            return Task.FromResult(new OASISResult<bool>(true));
        }
        public Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant>(Grant!));
        public Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken)
        {
            Grant = null;
            return Task.FromResult(new OASISResult<bool>(true));
        }
    }
}
