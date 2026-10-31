using System;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    public class NativeConnectionVaultTests
    {
        private const string Key = "a-long-random-test-key-0123456789";

        private static NativeConnection Sample() => new NativeConnection { ["uri"] = "mongodb://user:s3cretpass@db.example.com", ["database"] = "shop" };

        [Fact]
        public void SealedConnectionDoesNotContainSecretsAndOpensAgain()
        {
            var sealedText = NativeConnectionVault.Seal(Sample(), Key);
            Assert.DoesNotContain("s3cretpass", sealedText);
            Assert.DoesNotContain("db.example.com", sealedText);
            var opened = NativeConnectionVault.Open(sealedText, Key);
            Assert.Equal("mongodb://user:s3cretpass@db.example.com", opened.Require("uri"));
            Assert.Equal("shop", opened.Require("database"));
        }

        [Fact]
        public void SealingTwiceGivesDifferentCiphertext() =>
            Assert.NotEqual(NativeConnectionVault.Seal(Sample(), Key), NativeConnectionVault.Seal(Sample(), Key));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void NoKeyRefusesToSealOrOpen(string key)
        {
            Assert.Throws<InvalidOperationException>(() => NativeConnectionVault.Seal(Sample(), key));
            Assert.Throws<InvalidOperationException>(() => NativeConnectionVault.Open(NativeConnectionVault.Seal(Sample(), Key), key));
        }

        [Fact]
        public void WrongKeyDoesNotOpen() =>
            Assert.ThrowsAny<Exception>(() => NativeConnectionVault.Open(NativeConnectionVault.Seal(Sample(), Key), "a-different-key-9876543210"));

        [Fact]
        public void PlainTextIsRefusedRatherThanTrusted() =>
            Assert.Throws<InvalidOperationException>(() => NativeConnectionVault.Open("{\"uri\":\"mongodb://x\"}", Key));
    }
}
