using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicStorageIdentityTests
{
    [Fact]
    public void BindingDistinguishesAuthorityEndpointAndStoreWithoutExposingAddress()
    {
        var store = Guid.NewGuid();
        var first = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), store);
        Assert.Equal(first.BindingFingerprint, new CosmicStorageIdentity(first.Authority, first.EndpointFingerprint, store).BindingFingerprint);
        Assert.NotEqual(first.BindingFingerprint, new CosmicStorageIdentity(ProviderType.Neo4jOASIS, first.EndpointFingerprint, store).BindingFingerprint);
        Assert.NotEqual(first.BindingFingerprint, new CosmicStorageIdentity(first.Authority, new string('B', 64), store).BindingFingerprint);
        Assert.NotEqual(first.BindingFingerprint, new CosmicStorageIdentity(first.Authority, first.EndpointFingerprint, Guid.NewGuid()).BindingFingerprint);
        Assert.Equal(64, first.BindingFingerprint.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void NonCanonicalEndpointFingerprintIsRejected(string fingerprint)
        => Assert.Throws<ArgumentException>(() => new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, fingerprint, Guid.NewGuid()));

    [Fact]
    public void EmptyStoreIdentityIsRejected()
        => Assert.Throws<ArgumentException>(() => new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.Empty));
}
