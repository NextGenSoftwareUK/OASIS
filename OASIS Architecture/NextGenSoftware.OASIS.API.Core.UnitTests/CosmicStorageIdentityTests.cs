using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicStorageIdentityTests
{
    [Fact]
    public void GuidInputRetainsItsOriginalV1FingerprintBytes()
    {
        var identity = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64),
            Guid.Parse("01234567-89ab-4cde-8f01-23456789abcd"));
        Assert.Equal("01234567-89ab-4cde-8f01-23456789abcd", identity.StoreId);
        Assert.Equal("E93775C8D9C425236F3829DDBC9F91210DA3A1513CF4B6E4946DDD11D8E09E5F", identity.BindingFingerprint);
    }

    [Fact]
    public void InvalidUnicodeCannotCollapseDifferentNativeIdentityTokens()
        => Assert.Throws<System.Text.EncoderFallbackException>(() =>
            new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('A', 64), "\uD800"));

    [Fact]
    public void OpaqueNativeIdentityIsPreservedInFullAndCaseIsNotGuessed()
    {
        var native = "native:database:" + new string('Q', 64);
        var identity = new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('A', 64), native);
        Assert.Equal(native, identity.StoreId);
        Assert.NotEqual(identity.BindingFingerprint,
            new CosmicStorageIdentity(identity.Authority, identity.EndpointFingerprint, native.ToLowerInvariant()).BindingFingerprint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData("line\nbreak")]
    public void InvalidOpaqueNativeIdentityIsRejected(string identity)
        => Assert.Throws<ArgumentException>(() => new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('A', 64), identity));

    [Fact]
    public void OversizedNativeIdentityIsRejected()
        => Assert.Throws<ArgumentException>(() => new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('A', 64), new string('X', 1025)));

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
