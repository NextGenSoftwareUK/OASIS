using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return SaveAvatarDetailAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var response = new OASISResult<IAvatarDetail>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (avatar == null || avatar.Id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref response, "A nonempty avatar detail identity is required for Bitcoin storage.");
                    return response;
                }

                return await CommitEncryptedRecordAsync<IAvatarDetail>(avatar, AvatarDetailRecordScope, BitcoinRecordKind.AvatarDetail);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarId = id.ToString(),
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            txid = "", // Will be filled by UTXO lookup
                            vout = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarEmail = avatarEmail,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            txid = "", // Will be filled by UTXO lookup
                            vout = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarUsername = avatarUsername,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            txid = "", // Will be filled by UTXO lookup
                            vout = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    providerKey = providerKey,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            txid = "", // Will be filled by UTXO lookup
                            vout = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (holon == null || holon.Id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref response, "A nonempty holon identity is required for Bitcoin storage.");
                    return response;
                }

                if (saveChildren && saveChildrenOnProvider && holon.Children?.Count > 0)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin storage requires each child to be published as its own record.");
                    return response;
                }

                response = await CommitEncryptedRecordAsync<IHolon>(holon, HolonRecordScope);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        private async Task<OASISResult<T>> CommitEncryptedRecordAsync<T>(T record, string scope, BitcoinRecordKind kind = BitcoinRecordKind.Holon)
            where T : class, IHolonBase
        {
            var result = new OASISResult<T>();
            var signer = new NBitcoin.BitcoinSecret(_privateKey, SigningNetwork);
            var source = signer.GetAddress(NBitcoin.ScriptPubKeyType.Segwit).ToString();
            var snapshot = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<T>.Capture(record);
            using var client = CreateRecordBlobClient();
            using var codec = CreateRecordCodec();
            var store = new BitcoinRecordBlobStore(client, codec, kind);
            var json = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(snapshot.Proposal);
            var commitment = await store.PutAsync(scope, json);
            var publication = await SendSignedTransactionAsync(source, null, 0m, commitment, null, commitmentOnly: true,
                preparePublication: transactionId =>
                {
                    snapshot.ValidateUnchanged();
                    var publicationName = GetPublicationPinName(SigningNetwork, source, transactionId);
                    return store.PinAsync(commitment, publicationName);
                });
            result.MetaData = publication.MetaData;
            result.ErrorCode = publication.ErrorCode;
            if (publication.IsError)
            {
                OASISErrorHandling.HandleError(ref result, publication.Message, publication.Exception);
                return result;
            }

            var stored = snapshot.Proposal;
            stored.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
            stored.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS] = publication.Result.TransactionResult;
            result.Result = stored;
            return result;
        }

        private const string AvatarDetailRecordScope = "bitcoin-oasis:avatar-detail:v1";

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByTransactionIdAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!NBitcoin.uint256.TryParse(providerKey, out var transactionId))
                    throw new ArgumentException("A Bitcoin transaction ID is required.", nameof(providerKey));

                if (!_isActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, activation.Message, activation.Exception);
                        return result;
                    }
                }

                var transaction = await ReadVerifiedTransactionAsync(transactionId);
                byte[] commitment = null;
                foreach (var output in transaction.Outputs)
                {
                    var pushes = new NBitcoin.TxNullDataTemplate(output.ScriptPubKey.Length)
                        .ExtractScriptPubKeyParameters(output.ScriptPubKey);
                    if (pushes?.Length != 1 || !BitcoinRecordBlobStore.IsCommitment(pushes[0], BitcoinRecordKind.AvatarDetail))
                        continue;
                    if (commitment != null)
                        throw new InvalidOperationException("The Bitcoin transaction contains multiple avatar detail commitments.");
                    commitment = pushes[0];
                }

                if (commitment != null)
                {
                    using var client = CreateRecordBlobClient();
                    using var codec = CreateRecordCodec();
                    var json = await new BitcoinRecordBlobStore(client, codec, BitcoinRecordKind.AvatarDetail)
                        .GetAsync(AvatarDetailRecordScope, commitment);
                    var detail = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Deserialize<AvatarDetail>(json);
                    if (detail == null || detail.Id == Guid.Empty)
                        throw new InvalidOperationException("The committed Bitcoin avatar detail has no valid identity.");
                    if (version == 0 || detail.Version == version)
                    {
                        detail.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                        detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS] = transactionId.ToString();
                        result.Result = detail;
                        return result;
                    }
                }

                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, "The Bitcoin transaction does not contain the requested avatar detail version.");
            }
            catch (BitcoinTransactionNotFoundException ex)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Bitcoin: {ex.Message}", ex);
            }

            return result;
        }

        private const string HolonRecordScope = "bitcoin-oasis:holon:v1";

        private static HttpClient CreateRecordBlobClient()
        {
            var endpoint = Environment.GetEnvironmentVariable("OASIS_BITCOIN_KUBO_API");
            if (!Uri.TryCreate(endpoint?.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("Configure OASIS_BITCOIN_KUBO_API with the Kubo RPC root.");
            return new HttpClient { BaseAddress = uri };
        }

        private static NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.EncryptedRecordCodec CreateRecordCodec()
        {
            var key = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend
                .ResolveEnvironmentKey("OASIS_BITCOIN_STORAGE_KEY");
            try
            {
                return new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.EncryptedRecordCodec(key, compress: true);
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            }
        }

        private static async Task<Holon> ReadEncryptedHolonAsync(byte[] commitment)
        {
            using var client = CreateRecordBlobClient();
            using var codec = CreateRecordCodec();
            var json = await new BitcoinRecordBlobStore(client, codec).GetAsync(HolonRecordScope, commitment);
            var holon = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Deserialize<Holon>(json);
            if (holon == null || holon.Id == Guid.Empty)
                throw new InvalidOperationException("The committed Bitcoin holon has no valid identity.");
            return holon;
        }

    }
}
