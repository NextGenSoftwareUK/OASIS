using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HyperDriveOfflineSessionGrantIssuerTests
{
    [Fact]
    public async Task IssuedGrantIsAuthenticatedDeviceBoundScopeBoundAndVerifiable()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "world.read", "quest.progress");
        Guid avatarId = Guid.NewGuid();
        Guid deviceId = Guid.NewGuid();

        var result = await issuer.IssueAsync(avatarId, new IssueHyperDriveOfflineSessionGrantRequest
        {
            DeviceId = deviceId,
            RequestedLifetimeMinutes = 30,
            RequestedScopes = new[] { "quest.progress", "world.read", "world.read" }
        }, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(avatarId, result.Result.AvatarId);
        Assert.Equal(deviceId, result.Result.DeviceId);
        Assert.Equal(new[] { "quest.progress", "world.read" }, result.Result.Scopes);
        Assert.True(HyperDriveOfflineSessionGrantProof.Verify(result.Result, key.ExportSubjectPublicKeyInfo()));
    }

    [Fact]
    public async Task RequestedLifetimeIsCappedByServerPolicy()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "world.read");
        var result = await issuer.IssueAsync(Guid.NewGuid(), new IssueHyperDriveOfflineSessionGrantRequest
        {
            DeviceId = Guid.NewGuid(), RequestedLifetimeMinutes = 9999, RequestedScopes = new[] { "world.read" }
        }, CancellationToken.None);

        Assert.InRange(result.Result.ExpiresUtc - result.Result.IssuedUtc,
            TimeSpan.FromMinutes(59.99), TimeSpan.FromMinutes(60.01));
    }

    [Fact]
    public async Task UnapprovedScopeIsRejectedInsteadOfSilentlyReduced()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "world.read");
        var result = await issuer.IssueAsync(Guid.NewGuid(), new IssueHyperDriveOfflineSessionGrantRequest
        {
            DeviceId = Guid.NewGuid(), RequestedLifetimeMinutes = 30,
            RequestedScopes = new[] { "world.read", "avatar.admin" }
        }, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("OFFLINE_GRANT_SCOPE_FORBIDDEN", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task MissingAuthenticatedAvatarIsRejected()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "world.read");
        var result = await issuer.IssueAsync(Guid.Empty, new IssueHyperDriveOfflineSessionGrantRequest
        {
            DeviceId = Guid.NewGuid(), RequestedLifetimeMinutes = 30, RequestedScopes = new[] { "world.read" }
        }, CancellationToken.None);
        Assert.Equal("OFFLINE_GRANT_AUTH_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task SyncScopedGrantAuthenticatesOnlyItsBoundAvatarAndDevice()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "hyperdrive.sync");
        Guid avatar = Guid.NewGuid();
        Guid device = Guid.NewGuid();
        var issued = await issuer.IssueAsync(avatar, new IssueHyperDriveOfflineSessionGrantRequest
        { DeviceId = device, RequestedLifetimeMinutes = 30, RequestedScopes = new[] { "hyperdrive.sync" } }, default);

        var validated = await issuer.ValidateAsync(issued.Result, "hyperdrive.sync", device, default);

        Assert.False(validated.IsError, validated.Message);
        Assert.Equal(avatar, validated.Result);
    }

    [Fact]
    public async Task OfflineGrantCannotAuthenticateADifferentDevice()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "hyperdrive.sync");
        var issued = await issuer.IssueAsync(Guid.NewGuid(), new IssueHyperDriveOfflineSessionGrantRequest
        { DeviceId = Guid.NewGuid(), RequestedLifetimeMinutes = 30, RequestedScopes = new[] { "hyperdrive.sync" } }, default);
        var validated = await issuer.ValidateAsync(issued.Result, "hyperdrive.sync", Guid.NewGuid(), default);
        Assert.Equal("OFFLINE_GRANT_DEVICE_MISMATCH", validated.ErrorCode);
    }

    [Fact]
    public async Task OfflineGrantCannotEscalateBeyondIssuedScopes()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "world.read");
        Guid device = Guid.NewGuid();
        var issued = await issuer.IssueAsync(Guid.NewGuid(), new IssueHyperDriveOfflineSessionGrantRequest
        { DeviceId = device, RequestedLifetimeMinutes = 30, RequestedScopes = new[] { "world.read" } }, default);
        var validated = await issuer.ValidateAsync(issued.Result, "hyperdrive.sync", device, default);
        Assert.Equal("OFFLINE_GRANT_SCOPE_FORBIDDEN", validated.ErrorCode);
    }

    [Fact]
    public async Task TamperedOfflineGrantSignatureIsRejected()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var issuer = CreateIssuer(key, "hyperdrive.sync");
        Guid device = Guid.NewGuid();
        var issued = await issuer.IssueAsync(Guid.NewGuid(), new IssueHyperDriveOfflineSessionGrantRequest
        { DeviceId = device, RequestedLifetimeMinutes = 30, RequestedScopes = new[] { "hyperdrive.sync" } }, default);
        issued.Result.Signature = Convert.ToBase64String(new byte[64]);
        var validated = await issuer.ValidateAsync(issued.Result, "hyperdrive.sync", device, default);
        Assert.Equal("OFFLINE_GRANT_SIGNATURE_INVALID", validated.ErrorCode);
    }

    [Fact]
    public void EnabledIssuerFailsClosedWhenSigningKeyIsMissing()
    {
        var settings = Settings("world.read");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new HyperDriveOfflineSessionGrantIssuer(settings, _ => null));
        Assert.Contains(settings.SigningPrivateKeyEnvironmentVariable, exception.Message);
    }

    [Fact]
    public void EnabledIssuerFailsClosedWhenScopeAllowListIsEmpty()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var settings = Settings(Array.Empty<string>(), key);
        Assert.Throws<InvalidOperationException>(() => new HyperDriveOfflineSessionGrantIssuer(settings,
            _ => Convert.ToBase64String(key.ExportPkcs8PrivateKey())));
    }

    [Fact]
    public void EnabledIssuerFailsClosedWhenPinnedPublicKeyDoesNotMatchPrivateKey()
    {
        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var differentKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var settings = Settings("world.read");
        settings.SigningPublicKey = Convert.ToBase64String(differentKey.ExportSubjectPublicKeyInfo());

        var exception = Assert.Throws<InvalidOperationException>(() => new HyperDriveOfflineSessionGrantIssuer(settings,
            _ => Convert.ToBase64String(signingKey.ExportPkcs8PrivateKey())));

        Assert.Contains("does not match", exception.Message);
    }

    private static HyperDriveOfflineSessionGrantIssuer CreateIssuer(ECDsa key, params string[] scopes) =>
        new(Settings(scopes, key), _ => Convert.ToBase64String(key.ExportPkcs8PrivateKey()));

    private static OfflineSessionGrantSettings Settings(params string[] scopes) => Settings(scopes, null);

    private static OfflineSessionGrantSettings Settings(string[] scopes, ECDsa? key) => new()
    {
        Enabled = true,
        MaximumLifetimeMinutes = 60,
        SigningPrivateKeyEnvironmentVariable = "TEST_OFFLINE_GRANT_KEY",
        SigningPublicKey = key == null ? "AA==" : Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
        AllowedScopes = scopes.ToList()
    };
}
