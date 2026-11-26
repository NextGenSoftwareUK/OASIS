extern alias StacksCrypto;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StacksCrypto::Org.BouncyCastle.Asn1.Sec;
using StacksCrypto::Org.BouncyCastle.Crypto.Parameters;
using StacksCrypto::Org.BouncyCastle.Crypto.Signers;
using BigInteger = StacksCrypto::Org.BouncyCastle.Math.BigInteger;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.UnitTests
{
    public class BlockStackOASISTests
    {
        private const string KeyOne = "0000000000000000000000000000000000000000000000000000000000000001";

        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            var provider = new BlockStackOASIS();
            Assert.Equal("BlockStackOASIS", provider.ProviderName);
            Assert.Equal("BlockStack Provider", provider.ProviderDescription);
            Assert.Equal(ProviderType.BlockStackOASIS, provider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
        }

        [Fact]
        public async Task UnconfiguredProvider_RefusesActivationAndStorage()
        {
            var provider = new BlockStackOASIS();
            var activated = await provider.ActivateProviderAsync();
            Assert.True(activated.IsError);
            Assert.Contains("Gaia hub URL, bucket key and record encryption key", activated.Message + activated.Exception);
            var all = await provider.LoadAllAvatarsAsync();
            Assert.True(all.IsError);
        }

        [Fact]
        public void BucketAddress_IsTheP2pkhAddressOfTheCompressedKey()
        {
            using var client = new BlockStackClient("http://127.0.0.1:1", KeyOne);
            Assert.Equal("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798", client.PublicKeyHex);
            Assert.Equal("1BgGZ9tcN4rm9KBzDn7KprQz87SZ26SAMH", client.BucketAddress);
            using var walletFormat = new BlockStackClient("http://127.0.0.1:1", KeyOne + "01");
            Assert.Equal(client.BucketAddress, walletFormat.BucketAddress);
        }

        [Fact]
        public void AuthToken_IsAVerifiableLowSEs256kJwtOverTheHubChallenge()
        {
            var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            using var client = new BlockStackClient("http://127.0.0.1:4000/", key);
            const string challenge = "[\"gaiahub\",\"0\",\"127.0.0.1:4000\",\"blockstack_storage_please_sign\"]";
            var token = client.CreateAuthToken(challenge, DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
            Assert.StartsWith("v1:", token);
            var parts = token[3..].Split('.');
            Assert.Equal(3, parts.Length);

            using var header = JsonDocument.Parse(FromBase64Url(parts[0]));
            Assert.Equal("ES256K", header.RootElement.GetProperty("alg").GetString());
            using var claims = JsonDocument.Parse(FromBase64Url(parts[1]));
            Assert.Equal(challenge, claims.RootElement.GetProperty("gaiaChallenge").GetString());
            Assert.Equal(client.PublicKeyHex, claims.RootElement.GetProperty("iss").GetString());
            Assert.Equal("http://127.0.0.1:4000", claims.RootElement.GetProperty("hubUrl").GetString());
            Assert.Equal(1_800_000_000, claims.RootElement.GetProperty("iat").GetInt64());
            Assert.True(claims.RootElement.GetProperty("exp").GetInt64() > 1_800_000_000);
            Assert.Equal(32, claims.RootElement.GetProperty("salt").GetString()!.Length);

            var signature = FromBase64Url(parts[2]);
            Assert.Equal(64, signature.Length);
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            var r = new BigInteger(1, signature[..32]);
            var s = new BigInteger(1, signature[32..]);
            Assert.True(s.CompareTo(curve.N.ShiftRight(1)) <= 0, "Signature s must be low.");
            var verifier = new ECDsaSigner();
            verifier.Init(false, new ECPublicKeyParameters(curve.Curve.DecodePoint(Convert.FromHexString(client.PublicKeyHex)), domain));
            var digest = SHA256.HashData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));
            Assert.True(verifier.VerifySignature(digest, r, s));
        }

        [Theory]
        [InlineData("")]
        [InlineData("/abs")]
        [InlineData("a/../b")]
        public async Task Client_RefusesUnsafeFileNames(string name)
        {
            using var client = new BlockStackClient("http://127.0.0.1:1", KeyOne);
            await Assert.ThrowsAsync<ArgumentException>(() => client.GetTextAsync(name));
        }

        [Fact]
        public void Client_RejectsOutOfRangeKeys()
        {
            Assert.Throws<ArgumentException>(() => new BlockStackClient("http://127.0.0.1:1", new string('0', 64)));
            Assert.Throws<ArgumentException>(() => new BlockStackClient("http://127.0.0.1:1", "abcd"));
        }

        private static byte[] FromBase64Url(string text)
        {
            var padded = text.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
        }
    }
}
