using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                TronNativeTransferClient.NativeUnits(amount);
                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                    throw new ArgumentException("Sender address and explicit private signing key are required.");
                if (string.IsNullOrWhiteSpace(_bridgePoolWalletAddress))
                    throw new InvalidOperationException("BridgePoolWalletAddress must be configured separately from token contracts.");
                if (string.Equals(senderAccountAddress,_bridgePoolWalletAddress,StringComparison.Ordinal))
                    throw new ArgumentException("Bridge withdrawal sender must differ from the pool wallet.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(senderPrivateKey,"\\A[0-9a-fA-F]{64}\\z")
                    || !string.Equals(new TronNet.TronECKey(senderPrivateKey,TronNet.TronNetwork.MainNet).GetPublicAddress(),senderAccountAddress,StringComparison.Ordinal))
                    throw new ArgumentException("Signing key does not own the canonical TRON sender address.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                var hash = await ExecuteRuntimeAsync(client => client.SendAsync(senderAccountAddress, _bridgePoolWalletAddress, amount, senderPrivateKey, null));
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = hash, IsSuccessful = true, Status = BridgeTransactionStatus.Completed
                };
                result.Message = "Native TRX transfer to bridge pool confirmed; cross-chain settlement is separate.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing TRX to bridge pool: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                TronNativeTransferClient.NativeUnits(amount);
                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                    throw new ArgumentException("Receiver address is required.");
                if (string.IsNullOrWhiteSpace(_bridgePoolWalletAddress))
                    throw new InvalidOperationException("BridgePoolWalletAddress must be configured separately from token contracts.");
                if (string.Equals(receiverAccountAddress,_bridgePoolWalletAddress,StringComparison.Ordinal))
                    throw new ArgumentException("Bridge deposit recipient must differ from the pool wallet.");
                if (_nativeSigningKeyResolver == null)
                    throw new InvalidOperationException("Bridge pool signing credentials are not configured.");
                var key = await _nativeSigningKeyResolver(_bridgePoolWalletAddress);
                if (!System.Text.RegularExpressions.Regex.IsMatch(key ?? "","\\A[0-9a-fA-F]{64}\\z")
                    || !string.Equals(new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress(),_bridgePoolWalletAddress,StringComparison.Ordinal))
                    throw new ArgumentException("Signing key does not own the canonical TRON pool address.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                var hash = await ExecuteRuntimeAsync(client => client.SendAsync(_bridgePoolWalletAddress, receiverAccountAddress, amount, key, null));
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = hash, IsSuccessful = true, Status = BridgeTransactionStatus.Completed
                };
                result.Message = "Native TRX transfer from bridge pool confirmed; cross-chain settlement is separate.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing TRX from bridge pool: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(transactionHash) || !System.Text.RegularExpressions.Regex.IsMatch(transactionHash, "\\A[0-9a-fA-F]{64}\\z"))
                    throw new ArgumentException("A 32-byte hexadecimal TRON transaction ID is required.");
                result.Result = await ExecuteNativeRuntimeAsync(client => client.GetTransactionStatusAsync(transactionHash, token), token);
                result.Message = "Native-chain transaction status queried through genesis-verified SDK nodes; cross-chain settlement is separate.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(transaction);
                if (transaction.Amount != 1) throw new ArgumentException("TRC721 transfer amount must be exactly one.");
                if (string.IsNullOrWhiteSpace(transaction.TokenAddress) || string.IsNullOrWhiteSpace(transaction.TokenId)
                    || !System.Text.RegularExpressions.Regex.IsMatch(transaction.TokenId, "\\A[0-9]+\\z")
                    || !System.Numerics.BigInteger.TryParse(transaction.TokenId, out var tokenId))
                    throw new ArgumentException("An explicit TRC721 contract and unsigned decimal token ID are required.");
                var selectedAsset = TronNativeTransferClient.ParseNftAddress(transaction.TokenAddress + "/" + transaction.TokenId);
                if (!string.IsNullOrWhiteSpace(transaction.FromNFTTokenAddress))
                {
                    var sourceAsset = TronNativeTransferClient.ParseNftAddress(transaction.FromNFTTokenAddress);
                    if (!string.Equals(sourceAsset.Contract, selectedAsset.Contract, StringComparison.Ordinal)
                        || sourceAsset.TokenId != selectedAsset.TokenId)
                        throw new ArgumentException("TRC721 NFT asset identifiers must agree.");
                }
                if (string.IsNullOrWhiteSpace(transaction.FromWalletAddress) || string.IsNullOrWhiteSpace(transaction.ToWalletAddress)
                    || string.IsNullOrWhiteSpace(transaction.OwnerPrivateKey))
                    throw new ArgumentException("Sender, recipient and private signing key are required.");
                if (!string.IsNullOrWhiteSpace(transaction.OwnerSeedPhrase))
                    throw new ArgumentException("Recover an explicit signing key before sending a TRC721 NFT.");
                if (!string.IsNullOrWhiteSpace(transaction.OwnerPublicKey))
                {
                    var actual = Convert.ToHexString(new EthECKey(transaction.OwnerPrivateKey).GetPubKey());
                    var supplied = transaction.OwnerPublicKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? transaction.OwnerPublicKey[2..] : transaction.OwnerPublicKey;
                    if (!string.Equals(actual, supplied, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("NFT signing key does not match the supplied public key.");
                }
                var hash = await ExecuteNativeRuntimeAsync(client => client.SendNftAsync(transaction.FromWalletAddress,
                    transaction.ToWalletAddress, transaction.TokenAddress, tokenId, transaction.OwnerPrivateKey, transaction.MemoText));
                response.Result = new Web3NFTTransactionResponse { TransactionResult = hash, SendNFTTransactionResult = hash };
                response.Message = "TRC721 safe transfer confirmed by solidified contract receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error sending TRON NFT: {ex.Message}", ex);
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var asset = TronNativeTransferClient.ParseNftAddress(request.NFTTokenAddress);
                if (string.IsNullOrWhiteSpace(request.OwnerPrivateKey) || !string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                    throw new ArgumentException("An explicit owner private signing key is required; seed phrases are not accepted.");
                if (!string.IsNullOrWhiteSpace(request.OwnerPublicKey))
                {
                    var actual = Convert.ToHexString(new EthECKey(request.OwnerPrivateKey).GetPubKey());
                    var supplied = request.OwnerPublicKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? request.OwnerPublicKey[2..] : request.OwnerPublicKey;
                    if (!string.Equals(actual, supplied, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("NFT signing key does not match the supplied public key.");
                }
                var hash = await ExecuteNativeRuntimeAsync(client => client.BurnNftAsync(asset.Contract, asset.TokenId, request.OwnerPrivateKey));
                result.Result = new Web3NFTTransactionResponse { TransactionResult = hash };
                result.Message = "TRC721 burn extension confirmed by solidified contract receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning TRON NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(transaction);
                if (transaction.Amount != 1 || (transaction.NumberToMint.HasValue && transaction.NumberToMint != 1))
                    throw new ArgumentException("This TRC721 collection extension mints exactly one explicit token ID per transaction.");
                if (transaction.MintedByAvatarId == Guid.Empty)
                    throw new ArgumentException("MintedByAvatarId is required for minter signing-wallet resolution.");
                if (transaction.MetaData == null
                    || !transaction.MetaData.TryGetValue("MintFunctionSignature", out var signature)
                    || signature != "mint(address,uint256,string)"
                    || !transaction.MetaData.TryGetValue("TokenAddress", out var contract)
                    || string.IsNullOrWhiteSpace(contract)
                    || !transaction.MetaData.TryGetValue("TokenId", out var id)
                    || !System.Text.RegularExpressions.Regex.IsMatch(id ?? "", "\\A[0-9]+\\z")
                    || !System.Numerics.BigInteger.TryParse(id, out var tokenId)
                    || tokenId >= (System.Numerics.BigInteger.One << 256))
                    throw new ArgumentException("Explicit mint(address,uint256,string) extension, TokenAddress and uint256 TokenId are required in MetaData.");
                if (string.IsNullOrWhiteSpace(transaction.JSONMetaDataURL))
                    throw new ArgumentException("JSONMetaDataURL must identify the NFT metadata; no URI is generated implicitly.");
                if (transaction.StoreNFTMetaDataOnChain == true || transaction.FreezeMetadata == true
                    || (transaction.RoyaltyPercentage.HasValue && transaction.RoyaltyPercentage != 0)
                    || transaction.SendToAvatarAfterMintingId != Guid.Empty
                    || !string.IsNullOrWhiteSpace(transaction.SendToAvatarAfterMintingUsername)
                    || !string.IsNullOrWhiteSpace(transaction.SendToAvatarAfterMintingEmail))
                    throw new ArgumentException("On-chain metadata, freeze, royalties and avatar-recipient resolution require explicit collection support.");
                var mintedByAvatarId = transaction.MintedByAvatarId;
                var metadataUri = transaction.JSONMetaDataURL;
                var memo = transaction.MemoText;
                var requestedRecipient = transaction.SendToAddressAfterMinting;
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(mintedByAvatarId,
                    Core.Enums.ProviderType.TRONOASIS, showPrivateKeys: true);
                if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the avatar's TRON minter signing wallet.");
                var recipient = string.IsNullOrWhiteSpace(requestedRecipient) ? wallet.Result.WalletAddress : requestedRecipient;
                var hash = await ExecuteNativeRuntimeAsync(client => client.MintNftAsync(wallet.Result.WalletAddress,
                    recipient, contract, tokenId, metadataUri, wallet.Result.PrivateKey, memo));
                var nft = new Web3NFT { NFTTokenAddress = contract + "/" + tokenId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    JSONMetaDataURL = metadataUri, MintedByAvatarId = mintedByAvatarId,
                    OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS) };
                response.Result = new Web3NFTTransactionResponse { TransactionResult = hash, Web3NFT = nft,
                    Web3NFTs = new List<IWeb3NFT> { nft } };
                response.Message = "TRC721 mint extension confirmed by solidified contract receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error minting TRON NFT: {ex.Message}", ex);
            }

            return response;
        }

        //public OASISResult<IWeb3NFT> LoadNFT(Guid id)
        //{
        //    return new OASISResult<IWeb3NFT> { Message = "LoadNFT is not implemented yet for TRON provider." };
        //}

        //public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(Guid id)
        //{
        //    var response = new OASISResult<IWeb3NFT>();

        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
        //            return response;
        //        }

        //        // Query TRON blockchain for NFT data
        //        var httpResponse = await _httpClient.GetAsync($"{TRON_API_BASE_URL}/v1/accounts/{id}/tokens");
        //        if (httpResponse.IsSuccessStatusCode)
        //        {
        //            var content = await httpResponse.Content.ReadAsStringAsync();
        //            var nftData = JsonSerializer.Deserialize<JsonElement>(content);

        //            response.Result = new OASISNFT
        //            {
        //                Id = id,
        //                Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "TRON NFT",
        //                Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : "NFT from TRON blockchain",
        //                ImageUrl = nftData.TryGetProperty("imageUrl", out var img) ? img.GetString() : "",
        //                NFTTokenAddress = nftData.TryGetProperty("tokenId", out var tokenId) ? tokenId.GetString() : id.ToString(),
        //                OASISMintWalletAddress = nftData.TryGetProperty("contractAddress", out var contract) ? contract.GetString() : "TRC721_CONTRACT"
        //            };
        //            response.IsError = false;
        //            response.Message = "TRON NFT loaded successfully";
        //        }
        //        else
        //        {
        //            OASISErrorHandling.HandleError(ref response, $"Failed to load TRON NFT: {httpResponse.StatusCode}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref response, $"Error loading TRON NFT: {ex.Message}");
        //    }

        //    return response;
        //}

        //public OASISResult<IWeb3NFT> LoadNFT(string hash)
        //{
        //    return LoadNFTAsync(hash).Result;
        //}

        //public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(string hash)
        //{
        //    var result = new OASISResult<IWeb3NFT>();
        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
        //            return result;
        //        }

        //        // Query TRON blockchain for NFT by hash
        //        var nftData = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/nft/{hash}");
        //        if (string.IsNullOrEmpty(nftData))
        //        {
        //            OASISErrorHandling.HandleError(ref result, "Error loading NFT from TRON: No data returned");
        //            return result;
        //        }

        //        if (!string.IsNullOrEmpty(nftData))
        //        {
        //            // Parse JSON response from TRON API
        //            var nftResponse = JsonSerializer.Deserialize<TRONNFTResponse>(nftData);
        //            if (nftResponse != null)
        //        {
        //            var nft = new OASISNFT
        //            {
        //                    Id = Guid.NewGuid(),
        //                    Title = "TRON NFT",
        //                    Description = "TRON NFT Description",
        //                    ImageUrl = "",
        //                    NFTTokenAddress = nftResponse.TokenId ?? "",
        //                    OASISMintWalletAddress = nftResponse.ContractAddress ?? "",
        //                    NFTMintedUsingWalletAddress = nftResponse.OwnerAddress ?? "",
        //                    MintedOn = nftResponse.CreatedDate,
        //                    ImportedOn = nftResponse.ModifiedDate,
        //                    MetaData = new Dictionary<string, object>
        //                {
        //                    ["TRONHash"] = hash,
        //                        ["TRONContractAddress"] = nftResponse.ContractAddress ?? "",
        //                        ["TRONOwnerAddress"] = nftResponse.OwnerAddress ?? "",
        //                        ["TRONTokenId"] = nftResponse.TokenId ?? "",
        //                    ["Provider"] = "TRONOASIS"
        //                }
        //            };
        //            result.Result = nft;
        //            result.IsError = false;
        //            result.Message = "NFT loaded successfully from TRON";
        //            }
        //            else
        //            {
        //                OASISErrorHandling.HandleError(ref result, "Error parsing NFT data from TRON");
        //            }
        //        }
        //        else
        //        {
        //            OASISErrorHandling.HandleError(ref result, "NFT not found on TRON blockchain");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading NFT from TRON: {ex.Message}", ex);
        //    }
        //    return result;
        //}

        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        //{
        //    return LoadAllGeoNFTsForAvatarAsync(avatarId).Result;
        //}

        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        //{
        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
        //            return result;
        //        }

        //        // Get avatar's TRON address
        //        var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, avatarId);
        //        if (walletResult.IsError)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"Error getting wallet address for avatar: {walletResult.Message}");
        //            return result;
        //        }

        //        // Query TRON blockchain for all GeoNFTs owned by this address
        //        var address = walletResult.Result is IProviderWallet w ? w.WalletAddress : walletResult.Result?.ToString();
        //        var responseJson = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/geo-nfts/{address}");
        //        var geoArray = Newtonsoft.Json.Linq.JArray.Parse(responseJson);

        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
        //        foreach (var item in geoArray)
        //        {
        //            var title = item["name"]?.ToString() ?? "TRON GeoSpatial NFT";
        //            var description = item["description"]?.ToString() ?? string.Empty;
        //            var imageUrl = item["imageUrl"]?.ToString() ?? string.Empty;
        //            var tokenId = item["tokenId"]?.ToString() ?? string.Empty;
        //            var contractAddress = item["contractAddress"]?.ToString() ?? string.Empty;
        //            var ownerAddress = item["ownerAddress"]?.ToString() ?? string.Empty;
        //            var lat = item["latitude"] != null ? (long)(item["latitude"].Value<double>() * 1_000_000d) : 0L;
        //            var lon = item["longitude"] != null ? (long)(item["longitude"].Value<double>() * 1_000_000d) : 0L;
        //            var mintedOn = item["createdDate"] != null ? System.DateTime.Parse(item["createdDate"].ToString()) : System.DateTime.UtcNow;
        //            var importedOn = item["modifiedDate"] != null ? System.DateTime.Parse(item["modifiedDate"].ToString()) : System.DateTime.UtcNow;

        //            var geoNFT = new OASISGeoSpatialNFT
        //            {
        //                Id = System.Guid.NewGuid(),
        //                Title = title,
        //                Description = description,
        //                ImageUrl = imageUrl,
        //                NFTTokenAddress = tokenId,
        //                OASISMintWalletAddress = contractAddress,
        //                NFTMintedUsingWalletAddress = ownerAddress,
        //                Lat = lat,
        //                Long = lon,
        //                MintedOn = mintedOn,
        //                ImportedOn = importedOn,
        //                MetaData = new Dictionary<string, object>
        //                {
        //                    ["TRONContractAddress"] = contractAddress,
        //                    ["TRONOwnerAddress"] = ownerAddress,
        //                    ["TRONTokenId"] = tokenId,
        //                    ["Latitude"] = item["latitude"]?.ToString(),
        //                    ["Longitude"] = item["longitude"]?.ToString(),
        //                    ["Altitude"] = item["altitude"]?.ToString(),
        //                    ["Provider"] = "TRONOASIS"
        //                }
        //            };
        //            geoNFTs.Add(geoNFT);
        //        }

        //        result.Result = geoNFTs;
        //        result.IsError = false;
        //        result.Message = $"Successfully loaded {geoNFTs.Count} GeoNFTs for avatar from TRON";
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading GeoNFTs for avatar from TRON: {ex.Message}", ex);
        //    }
        //    return result;
        //}

        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        //{
        //    return LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress).Result;
        //}

        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        //{
        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
        //    string errorMessage = "Error in LoadAllGeoNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

        //    try
        //    {
        //        if (string.IsNullOrEmpty(mintWalletAddress))
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
        //            return result;
        //        }
        //
        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
        //
        //        // Query TRON network for NFTs owned by the mint address
        //        var nftQuery = new
        //        {
        //            owner_address = mintWalletAddress,
        //            limit = 200,
        //            offset = 0
        //        };
        //
        //        var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
        //            new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));
        //
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);
        //
        //            if (nftData != null)
        //            {
        //                // Parse TRON NFT data directly from response
        //                var geoNFT = new OASISGeoSpatialNFT
        //                {
        //                    Id = Guid.NewGuid(),
        //                    Title = "TRON GeoSpatial NFT",
        //                    Description = "TRON GeoSpatial NFT Description",
        //                    ImageUrl = string.Empty,
        //                    Lat = 0,
        //                    Long = 0,
        //                    OASISMintWalletAddress = mintWalletAddress,
        //                    GeoNFTMetaDataProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
        //                };
        //                geoNFTs.Add(geoNFT);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //    }
        //    return result;
        //        result.Result = geoNFTs;
        //        result.IsError = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //    }
        //
        //    return result;
        //}

    }
}
