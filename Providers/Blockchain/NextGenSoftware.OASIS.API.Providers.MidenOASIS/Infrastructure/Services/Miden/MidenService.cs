using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden
{
    public class MidenService : IMidenService
    {
        private readonly MidenAPIClient _apiClient;
        private const string BRIDGE_PROGRAM_HASH = "bridge_program_hash"; // TODO: Set from config

        public MidenService(MidenAPIClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<PrivateNote> CreatePrivateNoteAsync(decimal value, string ownerPublicKey, string assetId = null, string metadata = null)
        {
            var payload = new
            {
                value = value,
                owner = ownerPublicKey,
                assetId = assetId ?? "ZEC", // Default to ZEC for bridge
                metadata = metadata
            };

            var response = await _apiClient.PostAsync<PrivateNote>("/notes", payload);
            if (response.IsError)
            {
                throw new Exception($"Miden note creation failed: {response.Message}");
            }

            response.Result.CreatedAt = DateTime.UtcNow;
            response.Result.Status = "created";
            return response.Result;
        }

        public async Task<STARKProof> GenerateSTARKProofAsync(string programHash, object inputs, object outputs)
        {
            var request = new
            {
                programHash = programHash ?? BRIDGE_PROGRAM_HASH,
                inputs = inputs,
                outputs = outputs
            };

            var response = await _apiClient.PostAsync<STARKProof>("/proofs/stark", request);
            if (response.IsError)
            {
                throw new Exception($"Miden STARK proof generation failed: {response.Message}");
            }

            response.Result.CreatedAt = DateTime.UtcNow;
            response.Result.ProofType = "STARK";
            return response.Result;
        }

        public async Task<bool> VerifySTARKProofAsync(STARKProof proof)
        {
            var payload = new
            {
                proofData = proof.ProofData,
                publicInputs = proof.PublicInputs,
                programHash = proof.ProgramHash
            };

            var response = await _apiClient.PostAsync<dynamic>("/proofs/stark/verify", payload);
            if (response.IsError)
            {
                return false;
            }

            return response.Result?.verified == true;
        }

        public async Task<PrivateNote> NullifyNoteAsync(string noteId, STARKProof proof)
        {
            var payload = new
            {
                noteId = noteId,
                proof = new
                {
                    proofData = proof.ProofData,
                    publicInputs = proof.PublicInputs,
                    programHash = proof.ProgramHash
                }
            };

            var response = await _apiClient.PostAsync<PrivateNote>($"/notes/{noteId}/nullify", payload);
            if (response.IsError)
            {
                throw new Exception($"Miden note nullification failed: {response.Message}");
            }

            response.Result.Status = "nullified";
            response.Result.CreatedAt = DateTime.UtcNow;
            return response.Result;
        }

        #region Bridge Operations for Zcash ↔ Miden

        /// <summary>
        /// Mint on Miden after Zcash lock (Zcash → Miden bridge)
        /// </summary>
        public async Task<OASISResult<string>> MintOnMidenAsync(string midenAddress, decimal amount, string zcashTxHash, string viewingKey)
        {
            var result = new OASISResult<string>();
            try
            {
                // Create private note for the bridged amount
                var note = await CreatePrivateNoteAsync(amount, midenAddress, "ZEC", $"Bridge from Zcash: {zcashTxHash}");
                
                // Generate STARK proof for minting
                var proofInputs = new
                {
                    zcashTxHash = zcashTxHash,
                    viewingKey = viewingKey,
                    amount = amount,
                    recipient = midenAddress
                };
                
                var proofOutputs = new
                {
                    noteId = note.NoteId,
                    amount = amount
                };
                
                var proof = await GenerateSTARKProofAsync(BRIDGE_PROGRAM_HASH, proofInputs, proofOutputs);
                
                // Verify proof before submitting
                var verified = await VerifySTARKProofAsync(proof);
                if (!verified)
                {
                    throw new Exception("STARK proof verification failed");
                }
                
                // Submit transaction
                var txPayload = new
                {
                    proof = proof.ProofData,
                    publicInputs = proof.PublicInputs,
                    noteId = note.NoteId
                };
                
                var txResponse = await _apiClient.PostAsync<dynamic>("/transactions", txPayload);
                if (txResponse.IsError)
                {
                    throw new Exception($"Miden transaction submission failed: {txResponse.Message}");
                }
                
                result.Result = txResponse.Result?.transactionHash?.ToString() ?? note.NoteId;
                result.IsError = false;
                result.Message = "Minted on Miden successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            
            return result;
        }

        /// <summary>
        /// Lock on Miden for Zcash withdrawal (Miden → Zcash bridge)
        /// </summary>
        public async Task<OASISResult<string>> LockOnMidenAsync(string midenAddress, decimal amount, string zcashAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                // Generate STARK proof for locking
                var proofInputs = new
                {
                    action = "lock",
                    amount = amount,
                    midenAddress = midenAddress,
                    zcashAddress = zcashAddress
                };
                
                var proof = await GenerateSTARKProofAsync(BRIDGE_PROGRAM_HASH, proofInputs, new { locked = true });
                
                // Verify proof
                var verified = await VerifySTARKProofAsync(proof);
                if (!verified)
                {
                    throw new Exception("STARK proof verification failed");
                }
                
                // Submit lock transaction
                var txPayload = new
                {
                    proof = proof.ProofData,
                    publicInputs = proof.PublicInputs,
                    action = "lock",
                    amount = amount,
                    zcashAddress = zcashAddress
                };
                
                var txResponse = await _apiClient.PostAsync<dynamic>("/bridge/lock", txPayload);
                if (txResponse.IsError)
                {
                    throw new Exception($"Miden lock failed: {txResponse.Message}");
                }
                
                result.Result = txResponse.Result?.lockId?.ToString() ?? Guid.NewGuid().ToString();
                result.IsError = false;
                result.Message = "Locked on Miden successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            
            return result;
        }

        /// <summary>
        /// Release from Miden after Zcash mint (Miden → Zcash bridge completion)
        /// </summary>
        public async Task<OASISResult<string>> ReleaseFromMidenAsync(string midenAddress, decimal amount, string zcashAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                // Nullify the note on Miden
                var proofInputs = new
                {
                    action = "release",
                    amount = amount,
                    zcashAddress = zcashAddress
                };
                
                var proof = await GenerateSTARKProofAsync(BRIDGE_PROGRAM_HASH, proofInputs, new { released = true });
                
                // Submit release transaction
                var txPayload = new
                {
                    proof = proof.ProofData,
                    publicInputs = proof.PublicInputs,
                    action = "release",
                    amount = amount,
                    zcashAddress = zcashAddress
                };
                
                var txResponse = await _apiClient.PostAsync<dynamic>("/bridge/release", txPayload);
                if (txResponse.IsError)
                {
                    throw new Exception($"Miden release failed: {txResponse.Message}");
                }
                
                result.Result = txResponse.Result?.releaseId?.ToString() ?? Guid.NewGuid().ToString();
                result.IsError = false;
                result.Message = "Released from Miden successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            
            return result;
        }

        #endregion
    }
}

