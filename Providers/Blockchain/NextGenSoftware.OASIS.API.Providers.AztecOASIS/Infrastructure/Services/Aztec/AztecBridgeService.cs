using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec
{
    public class AztecBridgeService : IAztecBridgeService, IOASISBridge
    {
        private readonly AztecAPIClient _apiClient;
        private readonly AztecTestnetClient _testnetClient;
        private readonly AztecCLIService _cliService;

        public AztecBridgeService(AztecAPIClient apiClient, AztecTestnetClient testnetClient = null, AztecCLIService cliService = null)
        {
            _apiClient = apiClient;
            // Use real Aztec testnet client - NO MOCKS
            _testnetClient = testnetClient ?? new AztecTestnetClient();
            // Use Aztec CLI for real transactions - NO MOCKS
            _cliService = cliService ?? new AztecCLIService();
        }

        #region IOASISBridge Implementation

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                // Get private notes from Aztec testnet - REAL API CALL
                var notesResult = await _testnetClient.GetPrivateNotesAsync(accountAddress);
                if (notesResult.IsError)
                {
                    result.IsError = true;
                    result.Message = notesResult.Message;
                    return result;
                }

                // Sum up all note values to get total balance
                decimal totalBalance = 0m;
                if (notesResult.Result != null)
                {
                    foreach (var note in notesResult.Result)
                    {
                        totalBalance += note.Value;
                    }
                }

                result.Result = totalBalance;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting Aztec account balance: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                // Generate a unique alias for this account
                var alias = $"oasis_{Guid.NewGuid():N}";
                return await _cliService.CreateAccountAsync(alias, token);
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error creating Aztec account: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    result.IsError = true;
                    result.Message = "Seed phrase cannot be empty.";
                    return result;
                }

                // Derive Aztec secret key from BIP-39 mnemonic using PBKDF2 (standard BIP-39 seed derivation).
                // Aztec uses Grumpkin curve scalars (32 bytes); we derive the seed and take the first 32 bytes.
                var mnemonicBytes = Encoding.UTF8.GetBytes(seedPhrase.Normalize(NormalizationForm.FormKD));
                var saltBytes = Encoding.UTF8.GetBytes("mnemonic"); // BIP-39 standard salt prefix
                using var pbkdf2 = new Rfc2898DeriveBytes(mnemonicBytes, saltBytes, 2048, HashAlgorithmName.SHA512);
                var seed = pbkdf2.GetBytes(64);
                var secretKeyBytes = seed.Take(32).ToArray();
                var secretKey = "0x" + BitConverter.ToString(secretKeyBytes).Replace("-", "").ToLowerInvariant();

                var alias = $"oasis_restored_{Guid.NewGuid():N}";
                return await _cliService.RestoreAccountAsync(secretKey, alias, token);
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error restoring Aztec account: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                var bridgeContractAddress = Environment.GetEnvironmentVariable("AZTEC_BRIDGE_CONTRACT_ADDRESS");
                if (string.IsNullOrWhiteSpace(bridgeContractAddress))
                {
                    result.IsError = true;
                    result.Message = "AZTEC_BRIDGE_CONTRACT_ADDRESS environment variable is not set. Deploy the Aztec bridge contract and set this variable to its address.";
                    return result;
                }

                // Restore the sender account in the CLI wallet so it can sign the transaction.
                var restoreResult = await RestoreAccountAsync(senderPrivateKey);
                if (restoreResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to load sender account into CLI wallet: {restoreResult.Message}";
                    return result;
                }
                var accountAlias = $"oasis_sender_{senderAccountAddress[..Math.Min(8, senderAccountAddress.Length)]}";

                var txResult = await _cliService.SendTransactionAsync(
                    accountAlias: accountAlias,
                    contractAddress: bridgeContractAddress,
                    functionName: "withdraw",
                    functionArgs: new object[] { senderAccountAddress, amount.ToString() }
                );

                if (txResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to submit withdrawal transaction: {txResult.Message}";
                    return result;
                }

                var txHash = txResult.Result;
                await StoreBridgeEventAsync("withdraw", senderAccountAddress, null, txHash);

                result.Result = new BridgeTransactionResponse(
                    txHash,
                    txHash,
                    true,
                    "Aztec withdrawal transaction submitted",
                    BridgeTransactionStatus.Pending);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error in withdrawal: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                var bridgeContractAddress = Environment.GetEnvironmentVariable("AZTEC_BRIDGE_CONTRACT_ADDRESS");
                if (string.IsNullOrWhiteSpace(bridgeContractAddress))
                {
                    result.IsError = true;
                    result.Message = "AZTEC_BRIDGE_CONTRACT_ADDRESS environment variable is not set. Deploy the Aztec bridge contract and set this variable to its address.";
                    return result;
                }

                // Use the OASIS bridge operator account (alias stored in AZTEC_OPERATOR_ACCOUNT_ALIAS env var or default).
                var operatorAlias = Environment.GetEnvironmentVariable("AZTEC_OPERATOR_ACCOUNT_ALIAS") ?? "oasis_operator";

                var txResult = await _cliService.SendTransactionAsync(
                    accountAlias: operatorAlias,
                    contractAddress: bridgeContractAddress,
                    functionName: "deposit",
                    functionArgs: new object[] { receiverAccountAddress, amount.ToString() }
                );

                if (txResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to submit deposit transaction: {txResult.Message}";
                    return result;
                }

                var txHash = txResult.Result;
                await StoreBridgeEventAsync("deposit", receiverAccountAddress, null, txHash);

                result.Result = new BridgeTransactionResponse(
                    txHash,
                    txHash,
                    true,
                    "Aztec deposit transaction submitted",
                    BridgeTransactionStatus.Pending);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error in deposit: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                // Get real transaction status from Aztec testnet - NO MOCKS
                var txStatusResult = await _testnetClient.GetTransactionStatusAsync(transactionHash);
                if (txStatusResult.IsError)
                {
                    result.IsError = true;
                    result.Message = txStatusResult.Message;
                    result.Result = BridgeTransactionStatus.NotFound;
                    return result;
                }

                // Map Aztec transaction status to bridge status
                var aztecStatus = txStatusResult.Result?.Status?.ToLowerInvariant();
                BridgeTransactionStatus bridgeStatus = aztecStatus switch
                {
                    "pending" => BridgeTransactionStatus.Pending,
                    "mined" or "confirmed" => BridgeTransactionStatus.Completed,
                    "failed" or "reverted" => BridgeTransactionStatus.Canceled,
                    _ => BridgeTransactionStatus.Pending
                };

                result.Result = bridgeStatus;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting Aztec transaction status: {ex.Message}";
                result.Exception = ex;
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

        #endregion

        #region Bridge Helpers

        public async Task<AztecTransaction> DepositFromZcashAsync(decimal amount, string zcashTxId, PrivateNote aztecNote)
        {
            var payload = new
            {
                amount,
                zcashTxId,
                noteId = aztecNote.NoteId,
                owner = aztecNote.OwnerPublicKey
            };

            var response = await _apiClient.PostAsync<AztecTransaction>("/bridge/deposit", payload);
            if (response.IsError)
            {
                throw new Exception($"Aztec deposit failed: {response.Message}");
            }

            await StoreBridgeEventAsync("deposit", zcashTxId, aztecNote.NoteId, response.Result.TransactionId);
            return response.Result;
        }

        public async Task<AztecTransaction> WithdrawToZcashAsync(PrivateNote note, AztecProof proof, string destinationAddress)
        {
            var payload = new
            {
                noteId = note.NoteId,
                value = note.Value,
                destinationAddress,
                proof = new
                {
                    type = proof.ProofType,
                    data = proof.ProofData,
                    publicInputs = proof.PublicInputs
                }
            };

            var response = await _apiClient.PostAsync<AztecTransaction>("/bridge/withdraw", payload);
            if (response.IsError)
            {
                throw new Exception($"Aztec withdrawal failed: {response.Message}");
            }

            await StoreBridgeEventAsync("withdraw", destinationAddress, note.NoteId, response.Result.TransactionId);
            return response.Result;
        }

        public async Task<AztecTransaction> SyncBridgeEventAsync(string eventId)
        {
            var response = await _apiClient.GetAsync<AztecTransaction>($"/bridge/events/{eventId}");
            if (response.IsError)
            {
                throw new Exception($"Failed to sync bridge event: {response.Message}");
            }

            return response.Result;
        }

        private async Task StoreBridgeEventAsync(string eventType, string reference, string noteId, string aztecTxId)
        {
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = $"Aztec Bridge Event - {eventType}",
                Description = $"Event reference: {reference}",
                HolonType = Core.Enums.HolonType.Default,
                MetaData = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "EventType", eventType },
                    { "Reference", reference },
                    { "NoteId", noteId },
                    { "AztecTransactionId", aztecTxId }
                }
            };

            await HolonManager.Instance.SaveHolonAsync(holon);
        }

        private async Task<PrivateNote> CreatePrivateNoteAsync(decimal value, string owner)
        {
            var response = await _apiClient.PostAsync<PrivateNote>("/notes", new { value, owner });
            if (response.IsError)
            {
                throw new Exception($"Failed to create Aztec note: {response.Message}");
            }

            response.Result.CreatedAt = DateTime.UtcNow;
            response.Result.OwnerPublicKey = owner;
            return response.Result;
        }

        private async Task<OASISResult<AztecProof>> GenerateProofAsync(string type, object payload)
        {
            var response = await _apiClient.PostAsync<AztecProof>("/proofs", new { type, payload });
            if (response.IsError)
            {
                var result = new OASISResult<AztecProof>();
                result.IsError = true;
                result.Message = response.Message;
                return result;
            }

            response.Result.CreatedAt = DateTime.UtcNow;
            return new OASISResult<AztecProof>(response.Result);
        }

        #endregion
    }
}

