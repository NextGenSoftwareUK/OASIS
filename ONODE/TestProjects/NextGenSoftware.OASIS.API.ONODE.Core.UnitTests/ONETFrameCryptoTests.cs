using System;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class ONETFrameCryptoTests
    {
        private static (string Public, string Private) NewKey()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return (Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), Convert.ToBase64String(key.ExportPkcs8PrivateKey()));
        }

        private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("{\"content\":\"hello\"}");

        [Fact]
        public void SealThenOpen_WithRecipientKey_ReturnsPlaintext()
        {
            var recipient = NewKey();

            var sealedMessage = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);

            sealedMessage.Version.Should().Be(ONETFrameCrypto.Version);
            Convert.FromBase64String(sealedMessage.Ciphertext).Should().NotEqual(Plaintext);
            ONETFrameCrypto.Open(recipient.Private, sealedMessage).Should().Equal(Plaintext);
        }

        [Fact]
        public void Open_WithAnotherNodesKey_Fails()
        {
            var recipient = NewKey();
            var other = NewKey();
            var sealedMessage = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);

            Action open = () => ONETFrameCrypto.Open(other.Private, sealedMessage);

            open.Should().Throw<CryptographicException>();
        }

        [Fact]
        public void Open_TamperedCiphertext_Fails()
        {
            var recipient = NewKey();
            var sealedMessage = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);
            var bytes = Convert.FromBase64String(sealedMessage.Ciphertext);
            bytes[0] ^= 0x01;
            sealedMessage.Ciphertext = Convert.ToBase64String(bytes);

            Action open = () => ONETFrameCrypto.Open(recipient.Private, sealedMessage);

            open.Should().Throw<CryptographicException>();
        }

        [Theory]
        [InlineData("source")]
        [InlineData("target")]
        [InlineData("issued")]
        public void Open_ChangedHeader_Fails(string field)
        {
            var recipient = NewKey();
            var sealedMessage = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);
            switch (field)
            {
                case "source": sealedMessage.SourceNodeId = "attacker"; break;
                case "target": sealedMessage.TargetNodeId = "other"; break;
                default: sealedMessage.IssuedUnixSeconds++; break;
            }

            Action open = () => ONETFrameCrypto.Open(recipient.Private, sealedMessage);

            open.Should().Throw<CryptographicException>();
        }

        [Fact]
        public void Open_OlderFrameVersion_IsRejected()
        {
            var recipient = NewKey();
            var sealedMessage = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);
            sealedMessage.Version = 1;

            Action open = () => ONETFrameCrypto.Open(recipient.Private, sealedMessage);

            open.Should().Throw<CryptographicException>().WithMessage("*version 1*");
        }

        [Fact]
        public void Seal_SamePlaintextTwice_ProducesDifferentCiphertext()
        {
            var recipient = NewKey();

            var first = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);
            var second = ONETFrameCrypto.Seal(recipient.Public, "src", "tgt", 1700000000, Plaintext);

            second.Ciphertext.Should().NotBe(first.Ciphertext);
            second.EphemeralPublicKey.Should().NotBe(first.EphemeralPublicKey);
        }
    }
}
