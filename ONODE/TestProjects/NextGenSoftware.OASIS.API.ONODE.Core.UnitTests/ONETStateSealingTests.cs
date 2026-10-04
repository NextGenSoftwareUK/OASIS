using System;
using System.Reflection;
using System.Security.Cryptography;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// The persisted ONET identity is sealed with AES-GCM bound to the instance id; these lock in that it cannot be
    /// read, moved to another instance, tampered with, or swapped for a mismatched keypair.
    /// </summary>
    public class ONETStateSealingTests
    {
        private static readonly Type IdentityType =
            typeof(ONETManager).GetNestedType("SealedONETIdentity", BindingFlags.NonPublic)!;

        private static object Invoke(string method, params object[] args) =>
            typeof(ONETManager).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args)!;

        private static object NewIdentity(string nodeId, string publicKey, string privateKey)
        {
            var identity = Activator.CreateInstance(IdentityType, nonPublic: true)!;
            IdentityType.GetProperty("NodeId")!.SetValue(identity, nodeId);
            IdentityType.GetProperty("PublicKey")!.SetValue(identity, publicKey);
            IdentityType.GetProperty("PrivateKey")!.SetValue(identity, privateKey);
            return identity;
        }

        private static object RealIdentity()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
            return NewIdentity(ONETSecurity.DeriveNodeId(publicKey)!, publicKey, Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()));
        }

        private static string Get(object identity, string property) => (string)IdentityType.GetProperty(property)!.GetValue(identity)!;

        [Fact]
        public void SealThenOpen_SameKeyAndInstance_RoundTrips()
        {
            var key = RandomNumberGenerator.GetBytes(32);
            var identity = RealIdentity();

            var sealedIdentity = (string)Invoke("SealIdentity", key, "railway:svc", identity);
            var opened = Invoke("OpenIdentity", key, "railway:svc", sealedIdentity);

            Get(opened, "PrivateKey").Should().Be(Get(identity, "PrivateKey"));
            sealedIdentity.Should().NotContain(Get(identity, "PrivateKey"));
            ((bool)Invoke("IsConsistentIdentity", opened)).Should().BeTrue();
        }

        [Fact]
        public void Open_DifferentInstanceId_Fails()
        {
            var key = RandomNumberGenerator.GetBytes(32);
            var sealedIdentity = (string)Invoke("SealIdentity", key, "instance-a", RealIdentity());

            Action act = () => Invoke("OpenIdentity", key, "instance-b", sealedIdentity);

            act.Should().Throw<TargetInvocationException>().WithInnerException<CryptographicException>();
        }

        [Fact]
        public void Open_TamperedCiphertext_Fails()
        {
            var key = RandomNumberGenerator.GetBytes(32);
            var bytes = Convert.FromBase64String((string)Invoke("SealIdentity", key, "i", RealIdentity()));
            bytes[^1] ^= 0x01;

            Action act = () => Invoke("OpenIdentity", key, "i", Convert.ToBase64String(bytes));

            act.Should().Throw<TargetInvocationException>().WithInnerException<CryptographicException>();
        }

        [Fact]
        public void IsConsistentIdentity_PrivateKeyFromOtherPair_IsFalse()
        {
            var a = RealIdentity();
            var b = RealIdentity();
            var mixed = NewIdentity(Get(a, "NodeId"), Get(a, "PublicKey"), Get(b, "PrivateKey"));

            ((bool)Invoke("IsConsistentIdentity", mixed)).Should().BeFalse();
        }
    }
}
