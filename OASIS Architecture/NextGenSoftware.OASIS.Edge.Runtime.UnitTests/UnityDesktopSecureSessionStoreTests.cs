using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Edge.Unity;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class UnityDesktopSecureSessionStoreTests
{
    [Fact]
    public void RejectsEmptyIdentity()
    {
        Action create = () => new UnityPlatformSecureSessionStore(Guid.Empty, Guid.NewGuid());
        create.Should().Throw<ArgumentException>();
        create = () => new UnityPlatformSecureSessionStore(Guid.NewGuid(), Guid.Empty);
        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task RejectsMissingGrantBeforeNativeStorage()
    {
        var store = new UnityPlatformSecureSessionStore(Guid.NewGuid(), Guid.NewGuid());
        var result = await store.SaveAsync(null, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("EDGE_SECURE_SESSION_GRANT_REQUIRED");
    }

    [Fact]
    public async Task CancellationPreventsNativeAccess()
    {
        var store = new UnityPlatformSecureSessionStore(Guid.NewGuid(), Guid.NewGuid());
        var token = new CancellationToken(true);
        Func<Task> load = async () => await store.LoadAsync(token);
        await load.Should().ThrowAsync<OperationCanceledException>();
        Func<Task> delete = async () => await store.DeleteAsync(token);
        await delete.Should().ThrowAsync<OperationCanceledException>();
    }

    [WindowsStoreFact]
    public async Task UnityAdapterRoundTripsIsolatesAndDeletesWindowsGrant()
    {
        var avatar = Guid.NewGuid();
        var device = Guid.NewGuid();
        var store = new UnityPlatformSecureSessionStore(avatar, device);
        var isolated = new UnityPlatformSecureSessionStore(avatar, Guid.NewGuid());
        var grant = new HyperDriveOfflineSessionGrant
        {
            GrantId = Guid.NewGuid().ToString("N"), AvatarId = avatar, DeviceId = device,
            IssuedUtc = DateTime.UtcNow, ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
            Scopes = new[] { "edge:play" }, Signature = "test-not-a-valid-signature"
        };
        try
        {
            var saved = await store.SaveAsync(grant, CancellationToken.None);
            saved.IsError.Should().BeFalse(saved.Message);
            var loaded = await store.LoadAsync(CancellationToken.None);
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.GrantId.Should().Be(grant.GrantId);
            loaded.Result.DeviceId.Should().Be(device);
            var other = await isolated.LoadAsync(CancellationToken.None);
            other.IsError.Should().BeFalse(other.Message);
            other.Result.Should().BeNull();
        }
        finally
        {
            var deleted = await store.DeleteAsync(CancellationToken.None);
            deleted.IsError.Should().BeFalse(deleted.Message);
        }
        var missing = await store.LoadAsync(CancellationToken.None);
        missing.IsError.Should().BeFalse(missing.Message);
        missing.Result.Should().BeNull();
    }
}

public sealed class WindowsStoreFactAttribute : FactAttribute
{
    public WindowsStoreFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "Windows Credential Manager integration requires Windows; not desktop cross-platform qualification.";
    }
}
