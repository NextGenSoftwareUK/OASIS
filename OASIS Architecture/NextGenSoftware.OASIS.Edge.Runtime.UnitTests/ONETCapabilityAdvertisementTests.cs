using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ONET;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class ONETCapabilityAdvertisementTests
{
    [Fact]
    public async Task SignedAdvertisementIsCanonicalAndVerifiable()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = key.ExportSubjectPublicKeyInfo();
        string nodeId;
        using (var sha = SHA256.Create())
            nodeId = Convert.ToHexString(sha.ComputeHash(publicKey)).ToLowerInvariant();
        DateTime issued = new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        var created = await ONETCapabilityProof.CreateAsync(nodeId, Convert.ToBase64String(publicKey),
            ONETNodeProfile.Edge, new[] { "sync-v3", "sync-v3", "offline" }, new[]
            {
                new ONETProviderCapability { ProviderType = "SQLiteOASIS", ProviderCategory = "Storage", Capabilities = new[] { "write", "read" } },
                new ONETProviderCapability { ProviderType = "HoloOASIS", ProviderCategory = "Network", Capabilities = new[] { "distributed" } }
            }, issued, TimeSpan.FromMinutes(10), (message, _) => Task.FromResult(new OASISResult<string>
            {
                Result = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256))
            }), default);

        created.IsError.Should().BeFalse();
        created.Result.Services.Should().Equal("offline", "sync-v3");
        created.Result.Providers.Select(x => x.ProviderType).Should().Equal("HoloOASIS", "SQLiteOASIS");
        ONETCapabilityProof.Verify(created.Result, issued.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public async Task VerificationRejectsTamperingAndExpiry()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = key.ExportSubjectPublicKeyInfo();
        string nodeId;
        using (var sha = SHA256.Create()) nodeId = Convert.ToHexString(sha.ComputeHash(publicKey)).ToLowerInvariant();
        DateTime issued = DateTime.UtcNow;
        var created = await ONETCapabilityProof.CreateAsync(nodeId, Convert.ToBase64String(publicKey),
            ONETNodeProfile.Full, new[] { "sync-v3" }, Array.Empty<ONETProviderCapability>(), issued,
            TimeSpan.FromMinutes(1), (message, _) => Task.FromResult(new OASISResult<string>
            {
                Result = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256))
            }), default);

        created.Result.Services = new[] { "forged-service" };
        ONETCapabilityProof.Verify(created.Result, issued.AddSeconds(1)).Should().BeFalse();
        created.Result.Services = new[] { "sync-v3" };
        ONETCapabilityProof.Verify(created.Result, issued.AddMinutes(2)).Should().BeFalse();
    }
}
