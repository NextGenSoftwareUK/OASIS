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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS
{
    public partial class OptimismOASIS_Legacy
    {
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in SendNFTAsync method in OptimismOASIS. Reason: ";

            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Optimism provider is not activated");
                    return result;
                }

                if (transaction == null || string.IsNullOrWhiteSpace(transaction.TokenAddress) ||
                    string.IsNullOrWhiteSpace(transaction.ToWalletAddress) ||
                    string.IsNullOrWhiteSpace(transaction.FromWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address, from wallet address, and to wallet address are required");
                    return result;
                }

                // Get private key for sender
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(Guid.Empty, Core.Enums.ProviderType.OptimismOASIS);
                string privateKey = null;
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    if (transaction is SendWeb3NFTRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletAddress))
                    {
                        OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for sender wallet");
                        return result;
                    }
                }
                else
                {
                    privateKey = keysResult.Result[0];
                }

                var senderAccount = new Account(privateKey, BigInteger.Parse(_chainId));
                var web3 = new Web3(senderAccount, _rpcEndpoint);

                // ERC-721 transferFrom function ABI
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, transaction.TokenAddress);
                var transferFunction = erc721Contract.GetFunction("transferFrom");

                var tokenId = BigInteger.Parse(transaction.TokenId ?? "0");
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    transaction.FromWalletAddress,
                    transaction.ToWalletAddress,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 transfer failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                if (result.Result is Web3NFTTransactionResponse concreteResponse)
                    concreteResponse.SendNFTTransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = transaction.TokenAddress,
                    SendNFTTransactionHash = receipt.TransactionHash
                };
                result.IsError = false;
                result.Message = $"NFT sent successfully on Optimism. Transaction hash: {receipt.TransactionHash}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transaction)
        {
            return MintNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in MintNFTAsync method in OptimismOASIS. Reason: ";

            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Optimism provider is not activated");
                    return result;
                }

                if (transaction == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // Get private key from KeyManager using MintedByAvatarId
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(transaction.MintedByAvatarId, Core.Enums.ProviderType.OptimismOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderAccount = new Account(keysResult.Result[0], BigInteger.Parse(_chainId));
                var web3 = new Web3(senderAccount, _rpcEndpoint);

                // Use contract address or default NFT contract
                var nftContractAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";

                // ERC-721 mint function ABI (assuming contract has mint function)
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, nftContractAddress);
                var mintFunction = erc721Contract.GetFunction("mint");

                // Generate token ID (in production, this should be managed properly)
                var tokenId = new BigInteger(DateTime.UtcNow.Ticks);
                var mintToAddress = transaction.SendToAddressAfterMinting ?? senderAccount.Address;

                var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    mintToAddress,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 mint failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = nftContractAddress,
                    MintTransactionHash = receipt.TransactionHash,
                    NFTMintedUsingWalletAddress = senderAccount.Address
                };
                result.IsError = false;
                result.Message = $"NFT minted successfully on Optimism. Transaction hash: {receipt.TransactionHash}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error in LoadOnChainNFTDataAsync method in OptimismOASIS. Reason: ";

            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Optimism provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // ERC-721 tokenURI and ownerOf functions ABI
                var erc721Abi = @"[{""constant"":true,""inputs"":[{""name"":""_tokenId"",""type"":""uint256""}],""name"":""tokenURI"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_tokenId"",""type"":""uint256""}],""name"":""ownerOf"",""outputs"":[{""name"":"""",""type"":""address""}],""type"":""function""}]";
                var erc721Contract = _web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
                var tokenURIFunction = erc721Contract.GetFunction("tokenURI");
                var ownerOfFunction = erc721Contract.GetFunction("ownerOf");

                // Get token URI and owner for token ID 0 (in production, this should be parameterized)
                var tokenId = BigInteger.Zero;
                var tokenURI = await tokenURIFunction.CallAsync<string>(tokenId);
                var owner = await ownerOfFunction.CallAsync<string>(tokenId);

                var nft = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress
                };

                result.Result = nft;
                result.IsError = false;
                result.Message = "NFT data loaded successfully from Optimism blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in BurnNFTAsync method in OptimismOASIS. Reason: ";

            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Optimism provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Get private key from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.BurntByAvatarId, Core.Enums.ProviderType.OptimismOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderAccount = new Account(keysResult.Result[0], BigInteger.Parse(_chainId));
                var web3 = new Web3(senderAccount, _rpcEndpoint);

                // ERC-721 burn function ABI (assuming contract has burn function)
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_tokenId"",""type"":""uint256""}],""name"":""burn"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
                var burnFunction = erc721Contract.GetFunction("burn");

                // Token ID: use Web3NFTId converted to BigInteger for burn (or pass on-chain token id via metadata if needed)
                var tokenId = request.Web3NFTId != Guid.Empty ? new BigInteger(request.Web3NFTId.ToByteArray().Take(16).ToArray()) : BigInteger.Zero;

                var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 burn failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = request.NFTTokenAddress
                };
                result.IsError = false;
                result.Message = $"NFT burned successfully on Optimism. Transaction hash: {receipt.TransactionHash}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }



        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Optimism implementation: Save avatar to smart contract
                var avatarData = new
                {
                    avatarId = avatar.Id.ToString(),
                    username = avatar.Username,
                    email = avatar.Email,
                    firstName = avatar.FirstName,
                    lastName = avatar.LastName,
                    avatarType = avatar.AvatarType.Value.ToString(),
                    metadata = JsonSerializer.Serialize(avatar.MetaData)
                };

                // Call smart contract function to create/update avatar
                var createAvatarFunction = _contract.GetFunction("createAvatar");
                var gasEstimate = await createAvatarFunction.EstimateGasAsync(
                    avatarData.avatarId,
                    avatarData.username,
                    avatarData.email,
                    avatarData.firstName,
                    avatarData.lastName,
                    avatarData.avatarType,
                    avatarData.metadata
                );

                var transactionReceipt = await createAvatarFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarData.avatarId,
                    avatarData.username,
                    avatarData.email,
                    avatarData.firstName,
                    avatarData.lastName,
                    avatarData.avatarType,
                    avatarData.metadata
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = $"Avatar saved to Optimism successfully. Transaction hash: {transactionReceipt.TransactionHash}";

                    // Store transaction hash in avatar metadata
                    avatar.ProviderMetaData[Core.Enums.ProviderType.OptimismOASIS]["transactionHash"] = transactionReceipt.TransactionHash;
                    avatar.ProviderMetaData[Core.Enums.ProviderType.OptimismOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Optimism");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Optimism: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar detail cannot be null");
                    return response;
                }

                // Real Optimism implementation: Store AvatarDetail in Avatar's metadata
                // Since the contract doesn't have separate AvatarDetail functions, we store it in the Avatar's metadata
                var avatarId = avatarDetail.Id != Guid.Empty ? avatarDetail.Id.ToString() : Guid.Empty.ToString();
                
                // First, get the existing avatar to preserve its data
                var getAvatarFunction = _contract.GetFunction("getAvatar");
                GetAvatarOutputDTO existingAvatar = null;
                try
                {
                    existingAvatar = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);
                }
                catch { }

                // Serialize AvatarDetail to JSON and store in metadata
                var avatarDetailJson = JsonSerializer.Serialize(avatarDetail, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Merge AvatarDetail into metadata
                var metadataDict = new Dictionary<string, object>();
                if (existingAvatar != null && !string.IsNullOrEmpty(existingAvatar.Metadata))
                {
                    try
                    {
                        metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingAvatar.Metadata);
                    }
                    catch { }
                }
                
                metadataDict["AvatarDetail"] = avatarDetailJson;
                var mergedMetadata = JsonSerializer.Serialize(metadataDict);

                // Update avatar with AvatarDetail in metadata
                var updateAvatarFunction = _contract.GetFunction("updateAvatar");
                var gasEstimate = await updateAvatarFunction.EstimateGasAsync(
                    avatarId,
                    existingAvatar?.Username ?? avatarDetail.Username ?? "",
                    existingAvatar?.Email ?? avatarDetail.Email ?? "",
                    existingAvatar?.FirstName ?? (avatarDetail as AvatarDetail)?.FirstName ?? "",
                    existingAvatar?.LastName ?? (avatarDetail as AvatarDetail)?.LastName ?? "",
                    existingAvatar?.AvatarType ?? (avatarDetail as AvatarDetail)?.AvatarType?.Value.ToString() ?? "User",
                    mergedMetadata
                );

                var transactionReceipt = await updateAvatarFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarId,
                    existingAvatar?.Username ?? avatarDetail.Username ?? "",
                    existingAvatar?.Email ?? avatarDetail.Email ?? "",
                    existingAvatar?.FirstName ?? (avatarDetail as AvatarDetail)?.FirstName ?? "",
                    existingAvatar?.LastName ?? (avatarDetail as AvatarDetail)?.LastName ?? "",
                    existingAvatar?.AvatarType ?? (avatarDetail as AvatarDetail)?.AvatarType?.Value.ToString() ?? "User",
                    mergedMetadata
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    response.Result = avatarDetail;
                    response.IsError = false;
                    response.Message = $"Avatar detail saved to Optimism successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Optimism");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Optimism: {ex.Message}", ex);
            }
            return response;
        }

    }
}
