using System;
using Nethereum.Hex.HexConvertors.Extensions;
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
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public partial class FantomOASIS_Legacy
    {
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SendNFT is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SendNFT: {ex.Message}");
            }
            return response;
        }


        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                // Real Fantom ERC-721 NFT minting using Nethereum SDK
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Request is required");
                    return response;
                }

                // IMintWeb3NFTRequest inherits from MintNFTRequestBase which has MetaData
                var nftTokenAddress = request.MetaData?.ContainsKey("NFTTokenAddress") == true
                    ? request.MetaData["NFTTokenAddress"]?.ToString()
                    : "";

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "NFT token address is required in MetaData");
                    return response;
                }

                var mintToAddress = !string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)
                    ? request.SendToAddressAfterMinting
                    : await GetWalletAddressForAvatarAsync(request.MintedByAvatarId);

                if (string.IsNullOrWhiteSpace(mintToAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Mint to address is required");
                    return response;
                }

                // ERC-721 mint ABI - real implementation
                var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_tokenId\",\"type\":\"uint256\"},{\"name\":\"_uri\",\"type\":\"string\"}],\"name\":\"mint\",\"outputs\":[],\"type\":\"function\"}]";
                var contract = _web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
                var mintFunction = contract.GetFunction("mint");
                var tokenId = request.MetaData?.ContainsKey("TokenId") == true &&
                    int.TryParse(request.MetaData["TokenId"]?.ToString(), out var tid)
                    ? tid : (int)DateTime.UtcNow.Ticks;
                var tokenUri = request.JSONMetaDataURL ?? "";

                var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    new HexBigInteger(100000),
                    null,
                    null,
                    mintToAddress,
                    new BigInteger(tokenId),
                    tokenUri);

                response.Result = new Web3NFTTransactionResponse
                {
                    TransactionResult = receipt.TransactionHash,
                    Web3NFT = new Web3NFT
                    {
                        NFTTokenAddress = nftTokenAddress,
                        Title = request.Title,
                        Description = request.Description,
                        MintTransactionHash = receipt.TransactionHash
                    },
                    SendNFTTransactionResult = "NFT minted successfully on Fantom"
                };
                response.IsError = false;
                response.Message = "Fantom NFT minted successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in MintNFTAsync: {ex.Message}");
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }
                // Real Fantom ERC-721 NFT burning using Nethereum SDK
                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // ERC-721 burn ABI - real implementation
                var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
                var contract = _web3Client.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
                var burnFunction = contract.GetFunction("burn");

                // Get token ID from Web3NFTId (convert Guid to BigInteger hash)
                BigInteger tokenId = BigInteger.Zero;
                if (request.Web3NFTId != Guid.Empty)
                {
                    // Use Web3NFTId hash as token ID (consistent with other providers)
                    var tokenIdString = request.Web3NFTId.ToString().Replace("-", "");
                    if (BigInteger.TryParse(tokenIdString.Substring(0, Math.Min(32, tokenIdString.Length)), System.Globalization.NumberStyles.HexNumber, null, out var tid))
                    {
                        tokenId = tid;
                    }
                    else
                    {
                        // Fallback: use hash code
                        tokenId = new BigInteger(Math.Abs(request.Web3NFTId.GetHashCode()));
                    }
                }

                if (tokenId == BigInteger.Zero)
                {
                    OASISErrorHandling.HandleError(ref result, "Token ID is required. Please provide Web3NFTId.");
                    return result;
                }

                var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    new HexBigInteger(100000),
                    null,
                    null,
                    tokenId);

                result.Result = new Web3NFTTransactionResponse
                {
                    TransactionResult = receipt.TransactionHash,
                    Web3NFT = new Web3NFT
                    {
                        NFTTokenAddress = request.NFTTokenAddress
                    },
                    SendNFTTransactionResult = "NFT burned successfully on Fantom"
                };
                result.IsError = false;
                result.Message = "Fantom NFT burned successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in BurnNFTAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                // Real Fantom ERC-721 NFT metadata querying using Nethereum SDK
                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "NFT token address is required");
                    return response;
                }

                // ERC-721 metadata ABI - real implementation
                var erc721Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"}]";
                var contract = _web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);

                // Get NFT metadata
                var nameFunction = contract.GetFunction("name");
                var symbolFunction = contract.GetFunction("symbol");
                var name = await nameFunction.CallAsync<string>();
                var symbol = await symbolFunction.CallAsync<string>();

                var web3NFT = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    Title = name ?? "Fantom NFT",
                    Symbol = symbol ?? "FTM",
                    Description = $"ERC-721 NFT on Fantom blockchain"
                };

                response.Result = web3NFT;
                response.IsError = false;
                response.Message = "NFT data loaded successfully from Fantom";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadOnChainNFTDataAsync: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }
                // Real Fantom ERC-721 NFT bridge withdrawal using Nethereum SDK
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender account address, and private key are required");
                    return result;
                }

                // Transfer NFT to bridge contract using ERC-721 transferFrom
                var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[],\"type\":\"function\"}]";
                var senderAccount = new Account(senderPrivateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);
                var contract = web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
                var transferFunction = contract.GetFunction("transferFrom");
                var bridgeContractAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";

                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccountAddress,
                    new HexBigInteger(100000),
                    null,
                    null,
                    senderAccountAddress,
                    bridgeContractAddress,
                    BigInteger.Parse(tokenId));

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = receipt.TransactionHash,
                    Status = BridgeTransactionStatus.Completed,
                    IsSuccessful = true
                };
                result.IsError = false;
                result.Message = "NFT withdrawn to bridge successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }
                // Real Fantom ERC-721 NFT bridge deposit using Nethereum SDK
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, and receiver account address are required");
                    return result;
                }

                // Transfer NFT from bridge contract to receiver using ERC-721 transferFrom
                var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[],\"type\":\"function\"}]";
                var bridgeAccount = new Account(_privateKey ?? "");
                var web3Client = new Web3(bridgeAccount, _rpcEndpoint);
                var contract = web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
                var transferFunction = contract.GetFunction("transferFrom");
                var bridgeContractAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";

                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    bridgeAccount.Address,
                    new HexBigInteger(100000),
                    null,
                    null,
                    bridgeContractAddress,
                    receiverAccountAddress,
                    BigInteger.Parse(tokenId));

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = receipt.TransactionHash,
                    Status = BridgeTransactionStatus.Completed,
                    IsSuccessful = true
                };
                result.IsError = false;
                result.Message = "NFT deposited from bridge successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Save avatar to smart contract
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
                    response.Message = $"Avatar saved to Fantom successfully. Transaction hash: {transactionReceipt.TransactionHash}";

                    // Store transaction hash in avatar metadata
                    avatar.ProviderMetaData[Core.Enums.ProviderType.FantomOASIS]["transactionHash"] = transactionReceipt.TransactionHash;
                    avatar.ProviderMetaData[Core.Enums.ProviderType.FantomOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Fantom");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Fantom: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Save avatar detail to smart contract
                var ad = avatarDetail as AvatarDetail;
                var avatarDetailData = new
                {
                    avatarId = avatarDetail.Id.ToString(),
                    username = avatarDetail.Username,
                    email = avatarDetail.Email,
                    firstName = ad?.FirstName ?? "",
                    lastName = ad?.LastName ?? "",
                    avatarType = ad?.AvatarType?.Value.ToString() ?? "",
                    metadata = JsonSerializer.Serialize(avatarDetail.MetaData)
                };

                // Call smart contract function to create/update avatar detail
                var createAvatarDetailFunction = _contract.GetFunction("createAvatarDetail");
                var gasEstimate = await createAvatarDetailFunction.EstimateGasAsync(
                    avatarDetailData.avatarId,
                    avatarDetailData.username,
                    avatarDetailData.email,
                    avatarDetailData.firstName,
                    avatarDetailData.lastName,
                    avatarDetailData.avatarType,
                    avatarDetailData.metadata
                );

                var transactionReceipt = await createAvatarDetailFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarDetailData.avatarId,
                    avatarDetailData.username,
                    avatarDetailData.email,
                    avatarDetailData.firstName,
                    avatarDetailData.lastName,
                    avatarDetailData.avatarType,
                    avatarDetailData.metadata
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    response.Result = avatarDetail;
                    response.IsError = false;
                    response.Message = $"Avatar detail saved to Fantom successfully. Transaction hash: {transactionReceipt.TransactionHash}";

                    // Store transaction hash in avatar detail metadata
                    if (avatarDetail.ProviderMetaData == null)
                        avatarDetail.ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>();
                    if (!avatarDetail.ProviderMetaData.ContainsKey(Core.Enums.ProviderType.FantomOASIS))
                        avatarDetail.ProviderMetaData[Core.Enums.ProviderType.FantomOASIS] = new Dictionary<string, string>();
                    avatarDetail.ProviderMetaData[Core.Enums.ProviderType.FantomOASIS]["transactionHash"] = transactionReceipt.TransactionHash;
                    avatarDetail.ProviderMetaData[Core.Enums.ProviderType.FantomOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Fantom");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Fantom: {ex.Message}");
            }
            return response;
        }

    }
}
