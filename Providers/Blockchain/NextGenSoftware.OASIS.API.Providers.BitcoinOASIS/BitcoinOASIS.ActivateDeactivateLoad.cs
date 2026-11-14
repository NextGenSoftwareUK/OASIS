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
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Bitcoin provider is already activated";
                    return response;
                }

                using var testResponse = await _httpClient.GetAsync("blocks/tip/height").ConfigureAwait(false);
                if (testResponse.IsSuccessStatusCode)
                {
                    var body = await testResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!int.TryParse(body.Trim(), System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        OASISErrorHandling.HandleError(ref response, "Bitcoin explorer returned an invalid chain tip height.");
                        return response;
                    }
                    _isActivated = true;
                    IsProviderActivated = true;
                    response.Result = true;
                    response.Message = "Bitcoin provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to read Bitcoin explorer chain tip: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Bitcoin provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
            => Task.FromResult(DeActivateProvider());

        public override OASISResult<bool> DeActivateProvider()
        {
            _isActivated = false;
            IsProviderActivated = false;
            return new OASISResult<bool>(true) { Message = "Bitcoin provider deactivated successfully" };
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

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

                // Real Bitcoin implementation: Search for avatar data in OP_RETURN transactions
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { id.ToString(), true, 0, 100 }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

                    if (searchData.TryGetProperty("result", out var transactions))
                    {
                        foreach (var transaction in transactions.EnumerateArray())
                        {
                            if (transaction.TryGetProperty("vout", out var vouts))
                            {
                                foreach (var vout in vouts.EnumerateArray())
                                {
                                    if (vout.TryGetProperty("scriptPubKey", out var scriptPubKey) &&
                                        scriptPubKey.TryGetProperty("asm", out var asm))
                                    {
                                        var asmString = asm.GetString();
                                        if (asmString.StartsWith("OP_RETURN"))
                                        {
                                            // Extract OP_RETURN data
                                            var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                            try
                                            {
                                                var avatarBytes = Convert.FromHexString(opReturnData);
                                                var avatarJson = Encoding.UTF8.GetString(avatarBytes);
                                                var avatar = ParseBitcoinToAvatar(avatarJson);
                                                
                                                if (avatar != null && avatar.Id == id)
                                                {
                                                    response.Result = avatar;
                                                    response.IsError = false;
                                                    response.Message = "Avatar loaded from Bitcoin blockchain successfully";
                                                    return response;
                                                }
                                            }
                                            catch
                                            {
                                                // Skip invalid OP_RETURN data
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Bitcoin blockchain");
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No transactions found for avatar ID");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                if (!uint256.TryParse(providerKey, out var transactionId))
                {
                    OASISErrorHandling.HandleError(ref response, "A Bitcoin transaction ID is required to load an avatar.");
                    return response;
                }

                var transaction = await ReadVerifiedTransactionAsync(transactionId);

                Avatar avatar = null;
                foreach (var output in transaction.Outputs)
                {
                    var pushes = new TxNullDataTemplate(output.ScriptPubKey.Length)
                        .ExtractScriptPubKeyParameters(output.ScriptPubKey);
                    if (pushes?.Length != 1)
                        continue;

                    Avatar decoded;
                    try
                    {
                        if (BitcoinRecordBlobStore.IsCommitment(pushes[0], BitcoinRecordKind.Avatar))
                        {
                            using var client = CreateRecordBlobClient();
                            using var codec = CreateRecordCodec();
                            var json = await new BitcoinRecordBlobStore(client, codec, BitcoinRecordKind.Avatar)
                                .GetAsync(AvatarRecordScope, pushes[0]);
                            decoded = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Deserialize<Avatar>(json);
                            if (decoded == null || decoded.Id == Guid.Empty)
                                throw new InvalidOperationException("The committed Bitcoin avatar has no valid identity.");
                        }
                        else
                        {
                            decoded = ParseBitcoinToAvatar(Encoding.UTF8.GetString(pushes[0]));
                        }
                    }
                    catch (JsonException)
                    {
                        // Arbitrary data outputs are valid Bitcoin transactions.
                        continue;
                    }

                    if (decoded == null)
                        continue;
                    if (avatar != null)
                        throw new InvalidOperationException("The Bitcoin transaction contains multiple avatar records.");
                    avatar = decoded;
                }

                if (avatar == null || (version > 0 && avatar.Version != version))
                {
                    response.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    OASISErrorHandling.HandleError(ref response, "The Bitcoin transaction does not contain the requested avatar version.");
                    return response;
                }

                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS] = transactionId.ToString();
                response.Result = avatar;
            }
            catch (BitcoinTransactionNotFoundException ex)
            {
                response.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref response, ex.Message, ex);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                // Query Bitcoin blockchain for avatar by email using OP_RETURN data
                var queryUrl = $"/address/{avatarEmail}/txs";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for transactions containing email in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) &&
                                    script.TryGetProperty("asm", out var asm) &&
                                    asm.GetString().Contains(avatarEmail))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarEmail);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by email successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                // Query Bitcoin blockchain for avatar by username using OP_RETURN data
                var queryUrl = $"/address/{avatarUsername}/txs";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);

                    // Search for transactions containing username in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) &&
                                    script.TryGetProperty("asm", out var asm) &&
                                    asm.GetString().Contains(avatarUsername))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarUsername);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by username successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }

                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        private const string AvatarRecordScope = "bitcoin-oasis:avatar:v1";

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar == null || avatar.Id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "A nonempty avatar identity is required for Bitcoin storage.");
                    return result;
                }

                if (!_isActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, activation.Message, activation.Exception);
                        return result;
                    }
                }

                return await CommitEncryptedRecordAsync<IAvatar>(avatar, AvatarRecordScope, BitcoinRecordKind.Avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Bitcoin: {ex.Message}", ex);
                return result;
            }
        }

    }
}
