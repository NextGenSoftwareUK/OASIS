using System;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive
{
    public class HyperDrivePeerBindingProofTests
    {
        [Fact]
        public void Verify_AcceptsProofBoundToAvatarDeviceAndNode()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = key.ExportSubjectPublicKeyInfo();
            var avatarId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var nodeId = HyperDrivePeerBindingProof.DeriveNodeId(publicKey);
            var signature = key.SignData(Encoding.UTF8.GetBytes(
                HyperDrivePeerBindingProof.BuildMessage(avatarId, deviceId, nodeId)), HashAlgorithmName.SHA256);

            Assert.True(HyperDrivePeerBindingProof.Verify(avatarId, deviceId, nodeId, publicKey, signature));
        }

        [Fact]
        public void Verify_RejectsProofReplayedForAnotherAvatarOrDevice()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = key.ExportSubjectPublicKeyInfo();
            var avatarId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var nodeId = HyperDrivePeerBindingProof.DeriveNodeId(publicKey);
            var signature = key.SignData(Encoding.UTF8.GetBytes(
                HyperDrivePeerBindingProof.BuildMessage(avatarId, deviceId, nodeId)), HashAlgorithmName.SHA256);

            Assert.False(HyperDrivePeerBindingProof.Verify(Guid.NewGuid(), deviceId, nodeId, publicKey, signature));
            Assert.False(HyperDrivePeerBindingProof.Verify(avatarId, Guid.NewGuid(), nodeId, publicKey, signature));
        }

        [Fact]
        public void Verify_RejectsNodeIdThatDoesNotFingerprintPublicKey()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = key.ExportSubjectPublicKeyInfo();
            var avatarId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var realNodeId = HyperDrivePeerBindingProof.DeriveNodeId(publicKey);
            var signature = key.SignData(Encoding.UTF8.GetBytes(
                HyperDrivePeerBindingProof.BuildMessage(avatarId, deviceId, realNodeId)), HashAlgorithmName.SHA256);

            Assert.False(HyperDrivePeerBindingProof.Verify(avatarId, deviceId, new string('0', 64), publicKey, signature));
        }
    }
}
