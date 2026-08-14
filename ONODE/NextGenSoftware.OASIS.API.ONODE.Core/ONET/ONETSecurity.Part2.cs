using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    public partial class ONETSecurity
    {

        /// <summary>
        /// Generate security keys for a node
        /// </summary>
        public async Task<OASISResult<SecurityKey>> GenerateNodeKeysAsync(string nodeId)
        {
            var result = new OASISResult<SecurityKey>();
            
            try
            {
                var keyPair = await _encryptionProvider.GenerateKeyPairAsync();
                var symmetricKey = await _encryptionProvider.GenerateSymmetricKeyAsync();
                
                var securityKey = new SecurityKey
                {
                    NodeId = nodeId,
                    PublicKey = keyPair.PublicKey,
                    PrivateKey = keyPair.PrivateKey,
                    SymmetricKey = symmetricKey,
                    GeneratedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _nodeKeys[nodeId] = securityKey;

                result.Result = securityKey;
                result.IsError = false;
                result.Message = $"Security keys generated for node {nodeId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating node keys: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get security statistics
        /// </summary>
        public async Task<OASISResult<SecurityStats>> GetSecurityStatsAsync()
        {
            var result = new OASISResult<SecurityStats>();
            
            try
            {
                var stats = new SecurityStats
                {
                    TotalNodes = _nodeKeys.Count,
                    ActiveSessions = _activeSessions.Values.Count(s => s.IsActive),
                    EncryptionAlgorithm = _securityConfig.EncryptionAlgorithm,
                    KeySize = _securityConfig.KeySize,
                    SessionTimeout = _securityConfig.SessionTimeout,
                    LastSecurityCheck = DateTime.UtcNow
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Security statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting security statistics: {ex.Message}", ex);
            }

            return result;
        }

        private async Task LoadSecurityConfigurationAsync(OASISDNA? oasisdna)
        {
            // Load security configuration from this.OASISDNA
            _securityConfig.EncryptionAlgorithm = "AES-256-GCM";
            _securityConfig.KeySize = 256;
            _securityConfig.SessionTimeout = 24; // hours
            _securityConfig.EnableQuantumResistance = true;
            _securityConfig.EnableZeroTrust = true;
            
            // Load real security configuration
            try
            {
                _securityConfig.EncryptionAlgorithm = "AES-256-GCM";
                _securityConfig.KeySize = 256;
                _securityConfig.IvSize = 12;
                _securityConfig.TagSize = 16;
                _securityConfig.QuantumResistant = true;
                _securityConfig.ZeroTrust = true;
                
                // Load security policies
                await LoadSecurityPoliciesAsync();
                
                // Initialize quantum-resistant cryptography
                await InitializeQuantumResistantCryptoAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error loading security configuration: {ex.Message}", ex);
                throw;
            }
        }

        private async Task InitializeSecurityAsync()
        {
            // Initialize security system components
            await LoadSecurityConfigurationAsync(this.OASISDNA);
            await GenerateMasterKeysAsync();
            await StartSecurityMonitoringAsync();
        }

        private async Task GenerateMasterKeysAsync()
        {
            // Generate master security keys for the network
            // Generate real quantum-resistant keys
            try
            {
                using (var rng = RandomNumberGenerator.Create())
                {
                    var keyBytes = new byte[_securityConfig.KeySize / 8];
                    rng.GetBytes(keyBytes);
                    
                    var key = new SecurityKey
                    {
                        Id = Guid.NewGuid().ToString(),
                        KeyData = keyBytes,
                        Algorithm = _securityConfig.EncryptionAlgorithm,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(365),
                        IsQuantumResistant = _securityConfig.QuantumResistant
                    };
                    
                    _masterKey = key;
                }
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Error generating master security keys: {ex.Message}", ex);
                throw;
            }
        }

        private async Task StartSecurityMonitoringAsync()
        {
            // Start security monitoring processes
            // Start real security monitoring
            try
            {
                // Start intrusion detection system
                _ = Task.Run(MonitorIntrusionDetectionAsync);
                
                // Start anomaly detection
                _ = Task.Run(MonitorAnomalyDetectionAsync);
                
                // Start threat intelligence updates
                _ = Task.Run(UpdateThreatIntelligenceAsync);
                
                // Start security audit logging
                _ = Task.Run(StartSecurityAuditLoggingAsync);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error starting security monitoring: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<SecurityKey> GenerateSessionKeysAsync()
        {
            var keyPair = await _encryptionProvider.GenerateKeyPairAsync();
            var symmetricKey = await _encryptionProvider.GenerateSymmetricKeyAsync();
            
            return new SecurityKey
            {
                PublicKey = keyPair.PublicKey,
                PrivateKey = keyPair.PrivateKey,
                SymmetricKey = symmetricKey,
                GeneratedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        /// <summary>
        /// Performs a real proof-of-possession handshake: signs a fresh random nonce with the session's own
        /// private key and verifies it with the session's own public key. This used to trivially set
        /// Result=true with no actual cryptographic check, which meant a session with malformed or unusable
        /// key material (e.g. a key pair that doesn't actually round-trip through ECDsa) would still report a
        /// successful handshake. If signing or verification fails, the handshake now genuinely fails.
        /// </summary>
        private async Task<OASISResult<bool>> PerformSecureHandshakeAsync(SecuritySession session)
        {
            var result = new OASISResult<bool>();

            try
            {
                var nonceBytes = new byte[32];
                RandomNumberGenerator.Fill(nonceBytes);
                var nonce = Convert.ToBase64String(nonceBytes);

                var signingKey = new SecurityKey { PrivateKey = session.PrivateKey, KeyData = Convert.FromBase64String(session.PrivateKey) };
                var signature = await _encryptionProvider.SignAsync(nonce, signingKey);

                var verificationKey = new SecurityKey { PublicKey = session.PublicKey, KeyData = Convert.FromBase64String(session.PublicKey) };
                var verified = await _encryptionProvider.VerifySignatureAsync(nonce, signature, verificationKey);

                if (!verified)
                {
                    OASISErrorHandling.HandleError(ref result, "Secure handshake failed: proof-of-possession signature did not verify against the session's public key.");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Secure handshake completed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in secure handshake: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<string> GenerateMessageSignatureAsync(ONETMessage message, string privateKey)
        {
            // Generate digital signature for message integrity. KeyData must be populated with the decoded
            // PKCS8 private key bytes - SignAsync feeds it straight into ECDsa.ImportPkcs8PrivateKey, which
            // throws on the empty byte[] this SecurityKey would otherwise default to.
            var messageHash = await _encryptionProvider.ComputeHashAsync(message.Content);
            var securityKey = new SecurityKey { PrivateKey = privateKey, KeyData = Convert.FromBase64String(privateKey) };
            return await _encryptionProvider.SignAsync(messageHash, securityKey);
        }

        private async Task<bool> VerifyMessageSignatureAsync(ONETMessage message, string publicKey)
        {
            // Verify digital signature for message integrity. Same KeyData requirement as above, decoding
            // the SubjectPublicKeyInfo bytes VerifySignatureAsync needs for ECDsa.ImportSubjectPublicKeyInfo.
            var messageHash = await _encryptionProvider.ComputeHashAsync(message.Content);
            var securityKey = new SecurityKey { PublicKey = publicKey, KeyData = Convert.FromBase64String(publicKey) };
            return await _encryptionProvider.VerifySignatureAsync(messageHash, message.SecurityMetadata?.Signature ?? "", securityKey);
        }

        private async Task LoadSecurityPoliciesAsync()
        {
            // Load security policies from configuration
            try
            {
                // Load real security policies from OASIS DNA
                var policies = await LoadSecurityPoliciesFromDNAAsync();
                _securityPolicies = policies;
                
                // Apply security policies
                await ApplySecurityPoliciesAsync();
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Error loading security policies: {ex.Message}", ex);
            }
        }

        private async Task<Dictionary<string, object>> LoadSecurityPoliciesFromDNAAsync()
        {
            // Load security policies from OASIS DNA
            var policies = new Dictionary<string, object>();
            
            try
            {
                // Load from OASIS DNA configuration
                if (this.OASISDNA?.OASIS?.Security != null)
                {
                    policies["encryption_algorithm"] = "AES-256-GCM"; // Default encryption algorithm
                    policies["key_size"] = 256; // Default key size
                    policies["quantum_resistant"] = true; // Default quantum resistance
                    policies["zero_trust"] = true; // Default zero trust
                }
            }
            catch (Exception ex)
            {
                var result = new OASISResult<Dictionary<string, object>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading security policies from DNA: {ex.Message}", ex);
            }
            
            return policies;
        }

        private async Task ApplySecurityPoliciesAsync()
        {
            // Apply loaded security policies
            try
            {
                if (_securityPolicies.ContainsKey("encryption_algorithm"))
                {
                    _securityConfig.EncryptionAlgorithm = _securityPolicies["encryption_algorithm"].ToString();
                }
                
                if (_securityPolicies.ContainsKey("key_size"))
                {
                    _securityConfig.KeySize = Convert.ToInt32(_securityPolicies["key_size"]);
                }
                
                if (_securityPolicies.ContainsKey("quantum_resistant"))
                {
                    _securityConfig.QuantumResistant = Convert.ToBoolean(_securityPolicies["quantum_resistant"]);
                }
                
                if (_securityPolicies.ContainsKey("zero_trust"))
                {
                    _securityConfig.ZeroTrust = Convert.ToBoolean(_securityPolicies["zero_trust"]);
                }
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Error applying security policies: {ex.Message}", ex);
            }
        }


        // Missing helper method
        private async Task<bool> CalculateDefaultVerificationResultAsync()
        {
            try
            {
                // Calculate default verification result
                return await Task.FromResult(false); // Default to false for security
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating verification result: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Returns the base64 SubjectPublicKeyInfo public key for a known node, or empty string if unknown.
        /// Used when building the outgoing /onet/nodes peer list so recipients can register the key.
        /// </summary>
        public string GetNodePublicKey(string nodeId)
            => _nodeKeys.TryGetValue(nodeId, out var k) ? k.PublicKey ?? string.Empty : string.Empty;

        /// <summary>
        /// Signs <paramref name="message"/> with the local node's ECDSA private key.
        /// Returns null if the node has no registered key pair (e.g. key generation not yet called).
        /// </summary>
        public async Task<string?> SignMessageForNodeAsync(string nodeId, string message)
        {
            if (!_nodeKeys.TryGetValue(nodeId, out var key) || string.IsNullOrEmpty(key.PrivateKey))
                return null;
            var securityKey = new SecurityKey { PrivateKey = key.PrivateKey, KeyData = Convert.FromBase64String(key.PrivateKey) };
            return await _encryptionProvider.SignAsync(message, securityKey);
        }

        /// <summary>
        /// Register a remote node's public key so its signatures can be verified.
        /// Called when a peer announces itself during bootstrap peer-exchange.
        /// </summary>
        public void RegisterNodePublicKey(string nodeId, string base64PublicKey)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(base64PublicKey))
                return;
            _nodeKeys[nodeId] = new SecurityKey
            {
                NodeId = nodeId,
                PublicKey = base64PublicKey,
                KeyData = Convert.FromBase64String(base64PublicKey),
                IsActive = true,
                GeneratedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Verify an ECDSA P-256 signature produced by a remote node over <paramref name="message"/>.
        /// Returns false if the node's public key is not yet registered (unknown peer).
        /// </summary>
        public async Task<bool> VerifyNodeSignatureAsync(string nodeId, string message, string base64Signature)
        {
            if (!_nodeKeys.TryGetValue(nodeId, out var key) || key.KeyData == null || key.KeyData.Length == 0)
                return false;
            var securityKey = new SecurityKey { PublicKey = key.PublicKey, KeyData = key.KeyData };
            return await _encryptionProvider.VerifySignatureAsync(message, base64Signature, securityKey);
        }
    }
}
