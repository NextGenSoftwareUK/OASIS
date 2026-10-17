using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class WindowsCredentialSecureSessionStoreTests
{
    [Fact]
    public async Task RoundTripsAndDeletesGrantInWindowsCredentialManager()
    {
        if (!OperatingSystem.IsWindows()) return;

        Guid avatarId = Guid.NewGuid();
        Guid deviceId = Guid.NewGuid();
        var store = new WindowsCredentialSecureSessionStore(avatarId, deviceId);
        var grant = new HyperDriveOfflineSessionGrant
        {
            GrantId = Guid.NewGuid().ToString("N"),
            AvatarId = avatarId,
            DeviceId = deviceId,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddHours(1),
            Scopes = new[] { "hyperdrive.sync", "quest.progress" },
            Signature = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        };

        try
        {
            var saved = await store.SaveAsync(grant, CancellationToken.None);
            saved.IsError.Should().BeFalse(saved.Message);

            var loaded = await store.LoadAsync(CancellationToken.None);
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Should().NotBeNull();
            loaded.Result.GrantId.Should().Be(grant.GrantId);
            loaded.Result.AvatarId.Should().Be(avatarId);
            loaded.Result.DeviceId.Should().Be(deviceId);
            loaded.Result.Scopes.Should().Equal(grant.Scopes);
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
