using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for ONETSecurity's cryptography. Previously, GenerateKeyPairAsync/GenerateSymmetricKeyAsync
    /// returned base64-encoded random GUIDs instead of real key material, and SecurityKey.KeyData was never
    /// populated by callers - so every SignAsync/VerifySignatureAsync call threw on import, and
    /// PerformSecureHandshakeAsync trivially returned success regardless, masking the broken crypto entirely.
    /// These tests exercise the real EncryptionProvider directly to confirm the crypto now actually works.
    /// </summary>
    public class ONETSecurityTests
    {
        [Fact]
        public async Task GenerateKeyPairAsync_ProducesRealEcdsaKeyMaterial_NotARandomGuid()
        {
            var provider = new EncryptionProvider();
            var keyPair = await provider.GenerateKeyPairAsync();

            // A real PKCS8 P-256 private key is well over 16 bytes once base64-decoded (a bare GUID is exactly
            // 16 bytes / ~24 base64 chars) - this distinguishes real key material from the old fake GUID keys.
            var privateKeyBytes = System.Convert.FromBase64String(keyPair.PrivateKey);
            var publicKeyBytes = System.Convert.FromBase64String(keyPair.PublicKey);

            privateKeyBytes.Length.Should().BeGreaterThan(16);
            publicKeyBytes.Length.Should().BeGreaterThan(16);
        }

        [Fact]
        public async Task SignAsync_ThenVerifySignatureAsync_RoundTripsSuccessfully()
        {
            var provider = new EncryptionProvider();
            var keyPair = await provider.GenerateKeyPairAsync();

            var privateKey = new SecurityKey { PrivateKey = keyPair.PrivateKey, KeyData = System.Convert.FromBase64String(keyPair.PrivateKey) };
            var publicKey = new SecurityKey { PublicKey = keyPair.PublicKey, KeyData = System.Convert.FromBase64String(keyPair.PublicKey) };

            var hash = await provider.ComputeHashAsync("a real ONET message payload");
            var signature = await provider.SignAsync(hash, privateKey);

            (await provider.VerifySignatureAsync(hash, signature, publicKey)).Should().BeTrue();
        }

        [Fact]
        public async Task VerifySignatureAsync_TamperedData_FailsVerification()
        {
            var provider = new EncryptionProvider();
            var keyPair = await provider.GenerateKeyPairAsync();

            var privateKey = new SecurityKey { PrivateKey = keyPair.PrivateKey, KeyData = System.Convert.FromBase64String(keyPair.PrivateKey) };
            var publicKey = new SecurityKey { PublicKey = keyPair.PublicKey, KeyData = System.Convert.FromBase64String(keyPair.PublicKey) };

            var signature = await provider.SignAsync(await provider.ComputeHashAsync("original message"), privateKey);
            var tamperedHash = await provider.ComputeHashAsync("tampered message");

            (await provider.VerifySignatureAsync(tamperedHash, signature, publicKey)).Should().BeFalse();
        }

        [Fact]
        public async Task EncryptAsync_ThenDecryptAsync_RoundTripsToOriginalPlaintext()
        {
            var provider = new EncryptionProvider();
            var symmetricKeyBase64 = await provider.GenerateSymmetricKeyAsync();
            var key = new SecurityKey { SymmetricKey = symmetricKeyBase64, KeyData = System.Convert.FromBase64String(symmetricKeyBase64) };

            const string plaintext = "a secret ONET P2P message";
            var encrypted = await provider.EncryptAsync(plaintext, key);
            encrypted.Should().NotBe(plaintext);

            var decrypted = await provider.DecryptAsync(encrypted, key);
            decrypted.Should().Be(plaintext);
        }

        [Fact]
        public async Task GenerateSymmetricKeyAsync_ProducesA32ByteKey_ValidForAes256Gcm()
        {
            var provider = new EncryptionProvider();
            var symmetricKeyBase64 = await provider.GenerateSymmetricKeyAsync();

            System.Convert.FromBase64String(symmetricKeyBase64).Length.Should().Be(32, "AES-256-GCM requires a 32-byte key");
        }
    }
}
