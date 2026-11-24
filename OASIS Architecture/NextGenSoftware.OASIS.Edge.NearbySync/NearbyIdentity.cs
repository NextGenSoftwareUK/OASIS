using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// This device's signing identity plus the registry of peers it trusts. A peer is trusted only after its
    /// public key has been bound to an avatar through an authenticated channel (for example the existing
    /// ONET peer binding while online, or an in-person QR/NFC pairing). Unknown peers are never obeyed.
    /// </summary>
    public sealed class NearbyIdentity : IDisposable
    {
        private readonly ECDsa _key;
        private readonly ConcurrentDictionary<string, TrustedPeer> _trusted = new ConcurrentDictionary<string, TrustedPeer>();

        private sealed class TrustedPeer
        {
            public ECDsa PublicKey;
            public Guid AvatarId;
        }

        /// <summary>Creates a new identity, or restores one from <paramref name="privateKeyPkcs8"/>.</summary>
        public NearbyIdentity(byte[] privateKeyPkcs8 = null)
        {
            _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            if (privateKeyPkcs8 != null)
                _key.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
            PublicKey = _key.ExportSubjectPublicKeyInfo();
            NodeId = ComputeNodeId(PublicKey);
        }

        public string NodeId { get; }
        public byte[] PublicKey { get; }

        /// <summary>Private key for secure-store persistence (Keychain/Keystore/Credential Manager). Never log it.</summary>
        public byte[] ExportPrivateKey() => _key.ExportPkcs8PrivateKey();

        /// <summary>Node id is derived from the public key, so a key cannot claim someone else's id.</summary>
        public static string ComputeNodeId(byte[] publicKey)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(publicKey);
                return Convert.ToBase64String(hash, 0, 16).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        public string TrustPeer(byte[] publicKey, Guid avatarId)
        {
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));
            if (avatarId == Guid.Empty) throw new ArgumentException("An avatar id is required.", nameof(avatarId));
            var peerKey = ECDsa.Create();
            peerKey.ImportSubjectPublicKeyInfo(publicKey, out _);
            var nodeId = ComputeNodeId(publicKey);
            _trusted[nodeId] = new TrustedPeer { PublicKey = peerKey, AvatarId = avatarId };
            return nodeId;
        }

        /// <summary>Finds the trusted peer bound to an avatar, so messages can be addressed by avatar id.</summary>
        public bool TryFindNodeByAvatar(Guid avatarId, out string nodeId)
        {
            foreach (var pair in _trusted)
                if (pair.Value.AvatarId == avatarId) { nodeId = pair.Key; return true; }
            nodeId = null;
            return false;
        }

        public bool RevokePeer(string nodeId) => _trusted.TryRemove(nodeId ?? string.Empty, out _);

        public bool IsTrusted(string nodeId) => nodeId != null && _trusted.ContainsKey(nodeId);

        public byte[] Sign(byte[] data) => _key.SignData(data, HashAlgorithmName.SHA256);

        /// <summary>Verifies a signature from a trusted peer and yields the avatar bound to that peer.</summary>
        public bool TryVerify(string senderNodeId, byte[] data, byte[] signature, out Guid avatarId)
        {
            avatarId = Guid.Empty;
            if (senderNodeId == null || !_trusted.TryGetValue(senderNodeId, out var peer))
                return false;
            try
            {
                if (!peer.PublicKey.VerifyData(data, signature, HashAlgorithmName.SHA256))
                    return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
            avatarId = peer.AvatarId;
            return true;
        }

        public void Dispose()
        {
            _key.Dispose();
            foreach (var peer in _trusted.Values) peer.PublicKey.Dispose();
        }
    }
}
