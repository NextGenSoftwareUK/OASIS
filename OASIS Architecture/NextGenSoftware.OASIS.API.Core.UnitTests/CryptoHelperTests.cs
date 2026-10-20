using NextGenSoftware.OASIS.API.Core.Helpers.Crypto;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CryptoHelperTests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    // BIP-173 valid test strings: decoding and re-encoding must round-trip exactly.
    [Theory]
    [InlineData("a12uel5l")]
    [InlineData("abcdef1qpzry9x8gf2tvdw0s3jn54khce6mua7lmqqqxw")]
    [InlineData("split1checkupstagehandshakeupstreamerranterredcaperred2y9e3w")]
    [InlineData("?1ezyfcl")]
    public void Bech32RoundTripsBip173Vectors(string valid)
    {
        var (hrp, data) = Bech32.Decode(valid);
        Assert.Equal(valid, Bech32.Encode(hrp, data));
    }

    [Theory]
    [InlineData("a12uel5m")]                 // checksum altered
    [InlineData("A12uEL5L")]                 // mixed case
    [InlineData("pzry9x0s0muk")]             // no separator
    [InlineData("1pzry9x0s0muk")]            // empty hrp
    public void Bech32RejectsInvalidStrings(string invalid) => Assert.ThrowsAny<FormatException>(() => Bech32.Decode(invalid));

    // SLIP-0010 ed25519 test vector 1.
    [Fact]
    public void Slip10MatchesPublishedEd25519Vector()
    {
        var seed = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
        Assert.Equal("2b4be7f19ee27bbf30c667b642d5f4aa69fd169872f8fc3059c08ebae2eb19e7", Hex(Slip10Ed25519.Master(seed).Key));
        Assert.Equal("68e0fe46dfb67e368c75379acec591dad19df3cde26e63b93a8e704f1dade7a3", Hex(Slip10Ed25519.DerivePrivateKey(seed, 0)));
        Assert.Equal("b1d0bad404bf35da785a64ca1ac54b2617211d2777696fbffaf208f746ae84f2", Hex(Slip10Ed25519.DerivePrivateKey(seed, 0, 1)));
    }
}
