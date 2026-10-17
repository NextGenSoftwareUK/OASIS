using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HyperDriveOfflineSessionGrantProofTests
{
    [Fact]
    public void SignedGrantVerifiesWithCanonicalScopeOrdering()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var grant = NewGrant(new[] { "world.read", "quest.progress" });
        grant.Signature = HyperDriveOfflineSessionGrantProof.Sign(grant, key.ExportPkcs8PrivateKey());
        grant.Scopes = new[] { "quest.progress", "world.read" };

        Assert.True(HyperDriveOfflineSessionGrantProof.Verify(grant, key.ExportSubjectPublicKeyInfo()));
    }

    [Fact]
    public void MutatingDeviceScopeOrExpiryInvalidatesGrant()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var grant = NewGrant(new[] { "world.read" });
        grant.Signature = HyperDriveOfflineSessionGrantProof.Sign(grant, key.ExportPkcs8PrivateKey());

        grant.DeviceId = Guid.NewGuid();
        Assert.False(HyperDriveOfflineSessionGrantProof.Verify(grant, key.ExportSubjectPublicKeyInfo()));
        grant = NewGrant(new[] { "world.read" });
        grant.Signature = HyperDriveOfflineSessionGrantProof.Sign(grant, key.ExportPkcs8PrivateKey());
        grant.Scopes = new[] { "purchase.create" };
        Assert.False(HyperDriveOfflineSessionGrantProof.Verify(grant, key.ExportSubjectPublicKeyInfo()));
        grant = NewGrant(new[] { "world.read" });
        grant.Signature = HyperDriveOfflineSessionGrantProof.Sign(grant, key.ExportPkcs8PrivateKey());
        grant.ExpiresUtc = grant.ExpiresUtc.AddMinutes(1);
        Assert.False(HyperDriveOfflineSessionGrantProof.Verify(grant, key.ExportSubjectPublicKeyInfo()));
    }

    private static HyperDriveOfflineSessionGrant NewGrant(IReadOnlyList<string> scopes) => new()
    {
        GrantId = "grant-1", AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(),
        IssuedUtc = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc),
        ExpiresUtc = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc), Scopes = scopes
    };
}
