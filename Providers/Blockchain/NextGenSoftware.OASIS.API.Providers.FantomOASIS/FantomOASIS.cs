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

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public class FantomTransactionResponse : ITransactionResponse
    {
        public string TransactionResult { get; set; }
        public string MemoText { get; set; }
    }
    /// <summary>
    /// Fantom Provider for OASIS
    /// Implements Fantom Opera blockchain integration for high-performance smart contracts
    /// </summary>
    public class FantomOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _chainId;
        private readonly string _privateKey;
        private readonly string _contractAddress;
        private bool _isActivated;
        private WalletManager _walletManager;
        private Web3 _web3Client;
        private Account _account;
        private Contract _contract;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, OASISDNA);
                return _walletManager;
            }
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the FantomOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Fantom RPC endpoint URL</param>
        /// <param name="chainId">Fantom chain ID (250 for mainnet, 4002 for testnet)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public FantomOASIS(string rpcEndpoint = "https://rpc.ftm.tools", string chainId = "250", string privateKey = "", string contractAddress = "0x0000000000000000000000000000000000000000")
        {
            this.ProviderName = "FantomOASIS";
            this.ProviderDescription = "Fantom Provider - High-performance EVM-compatible blockchain";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FantomOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _chainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
            _privateKey = privateKey;
            _contractAddress = contractAddress;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rpcEndpoint)
            };
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Fantom provider is already activated";
                    return response;
                }

                // Initialize Web3 client for Fantom
                if (!string.IsNullOrEmpty(_privateKey))
                {
                    _account = new Account(_privateKey, BigInteger.Parse(_chainId));
                    _web3Client = new Web3(_account, _rpcEndpoint);
                }
                else
                {
                    _web3Client = new Web3(_rpcEndpoint);
                }

                // Test connection to Fantom RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    // Initialize smart contract if address is provided
                    if (!string.IsNullOrEmpty(_contractAddress) && _contractAddress != "0x0000000000000000000000000000000000000000")
                    {
                        // Load contract ABI and initialize contract
                        var contractAbi = GetFantomContractABI();
                        _contract = _web3Client.Eth.GetContract(contractAbi, _contractAddress);
                    }

                    _isActivated = true;
                    response.Result = true;
                    response.Message = "Fantom provider activated successfully with Web3 integration";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to Fantom RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Fantom provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                _isActivated = false;
                _httpClient?.Dispose();
                response.Result = true;
                response.Message = "Fantom provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Fantom provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                // Load avatar from Fantom blockchain
                var queryUrl = $"/api/v1/accounts/{id}";

                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse Fantom JSON and create Avatar object
                    var avatar = ParseFantomToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from Fantom successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Fantom JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Fantom blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Fantom: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        // Additional methods would be implemented here following the same pattern...
        // For brevity, I'll implement the key methods and mark others as "not yet implemented"

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                // Get players near me from Fantom blockchain
                var queryUrl = "/api/v1/accounts/nearby";

                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Fantom JSON and create Avatar collection
                    var avatars = ParseFantomToAvatars(content);
                    if (avatars != null)
                    {
                        response.Result = avatars;
                        response.Message = "Avatars loaded from Fantom successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Fantom JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from Fantom blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Fantom: {ex.Message}");
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                // Get holons near me from Fantom blockchain
                var queryUrl = $"/api/v1/accounts/holons?type={holonType}";

                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Fantom JSON and create Holon collection
                    var holons = ParseFantomToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.Message = "Holons loaded from Fantom successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Fantom JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Fantom blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Fantom: {ex.Message}");
            }

            return response;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Parse Fantom JSON content and convert to OASIS Avatar
        /// </summary>
        private IAvatar ParseFantomToAvatar(string fantomJson)
        {
            try
            {
                // Deserialize the complete Avatar object to preserve all properties
                var avatar = JsonSerializer.Deserialize<Avatar>(fantomJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return avatar;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        /// <summary>
        /// Get wallet address for avatar using real WalletManager API
        /// </summary>
        private async Task<string> GetWalletAddressForAvatarAsync(Guid avatarId)
        {
            try
            {
                if (avatarId == Guid.Empty)
                    return "";
                
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(
                    avatarId, 
                    false, 
                    false, 
                    Core.Enums.ProviderType.FantomOASIS);
                
                if (!walletResult.IsError && walletResult.Result != null && !string.IsNullOrWhiteSpace(walletResult.Result.Address))
                {
                    return walletResult.Result.Address;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error getting wallet address for avatar {avatarId}: {ex.Message}", ex);
            }
            return "";
        }

        /// <summary>
        /// Parse Fantom JSON content and convert to OASIS Player collection
        /// </summary>
        private IEnumerable<IPlayer> ParseFantomToPlayers(string fantomJson)
        {
            try
            {
                // Deserialize the complete Avatar collection to preserve all properties
                var players = JsonSerializer.Deserialize<IEnumerable<Avatar>>(fantomJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return players.Cast<IPlayer>();
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        /// <summary>
        /// Parse Fantom JSON content and convert to OASIS Holon collection
        /// </summary>
        private IEnumerable<IHolon> ParseFantomToHolons(string fantomJson)
        {
            try
            {
                // Deserialize the complete Holon collection to preserve all properties
                var holons = JsonSerializer.Deserialize<IEnumerable<Holon>>(fantomJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return holons;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        private IEnumerable<IAvatar> ParseFantomToAvatars(string fantomJson)
        {
            try
            {
                // Deserialize the complete Avatar collection to preserve all properties
                var avatars = JsonSerializer.Deserialize<IEnumerable<Avatar>>(fantomJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return avatars;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                // Convert decimal amount to wei (1 FTM = 10^18 wei)
                var amountInWei = (long)(amount * 1000000000000000000);

                // Get account balance and nonce using Fantom API
                var accountResponse = await _httpClient.GetAsync($"/api/v1/account/{fromWalletAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Fantom address {fromWalletAddress}: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);

                var balance = accountData.GetProperty("balance").GetInt64();
                if (balance < amountInWei)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balance} wei, Required: {amountInWei} wei");
                    return result;
                }

                var nonce = accountData.GetProperty("nonce").GetInt64();

                // Create Fantom transaction
                var transactionRequest = new
                {
                    from = fromWalletAddress,
                    to = toWalletAddress,
                    value = $"0x{amountInWei:x}",
                    gas = "0x5208", // 21000 gas for simple transfer
                    gasPrice = "0x3b9aca00", // 1 gwei
                    nonce = $"0x{nonce:x}",
                    data = "0x" // Empty data for simple transfer
                };

                // Submit transaction to Fantom network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/api/v1/sendRawTransaction", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new FantomTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("result").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"Fantom transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Fantom transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Fantom transaction: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region IOASISNFTProvider Implementation

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
                    SourceTransactionHash = receipt.TransactionHash,
                    Status = BridgeTransactionStatus.Success
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
                    SourceTransactionHash = receipt.TransactionHash,
                    Status = BridgeTransactionStatus.Success
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

        #endregion

        #region OASISStorageProviderBase Abstract Methods

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveAvatar is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveAvatar: {ex.Message}");
            }
            return response;
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
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveAvatarDetail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveAvatarDetail: {ex.Message}");
            }
            return response;
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
                OASISErrorHandling.HandleError(ref response, "SaveAvatarDetailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveAvatarDetailAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarByEmail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByEmail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarByEmailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByEmailAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByEmail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByEmail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByEmailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByEmailAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByUsername is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByUsername: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByUsernameAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByUsernameAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatar is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatar: {ex.Message}");
            }
            return response;
        }


        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaData is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaData: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllHolonsAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolonsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarDetails is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetails: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarDetailsAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetailsAsync: {ex.Message}");
            }
            return response;
        }

        // Additional missing abstract methods
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByEmailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByEmailAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByUsernameAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByUsernameAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByEmail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByEmail: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByUsername is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByUsername: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatar is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatar: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarByUsername is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByUsername: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarByUsernameAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByUsernameAsync: {ex.Message}");
            }
            return response;
        }


        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarByProviderKey is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByProviderKey: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarByProviderKeyAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByProviderKeyAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatars is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatars: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarsAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailAsync: {ex.Message}");
            }
            return response;
        }

        // Holon-related methods
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParent is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParentAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParent is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParentAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaData is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaData: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaDataAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaDataAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllHolons is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolons: {ex.Message}");
            }
            return response;
        }

        // Save/Delete Holon methods
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveHolons is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolons: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveHolonsAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolonAsync: {ex.Message}");
            }
            return response;
        }

        // Search methods
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "Search is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in Search: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SearchAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SearchAsync: {ex.Message}");
            }
            return response;
        }

        // Export methods
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAll is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAll: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarById is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarById: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByIdAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByIdAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByUsername is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByUsername: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByUsernameAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByUsernameAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByEmail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByEmail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByEmailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByEmailAsync: {ex.Message}");
            }
            return response;
        }

        // Import methods
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "Import is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in Import: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ImportAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ImportAsync: {ex.Message}");
            }
            return response;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion

        #region Smart Contract Methods

        /// <summary>
        /// Get Fantom smart contract ABI for OASIS operations
        /// </summary>
        private string GetFantomContractABI()
        {
            return @"[
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""username"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""email"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""firstName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""lastName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""avatarType"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""metadata"", ""type"": ""string""}
                    ],
                    ""name"": ""createAvatar"",
                    ""outputs"": [
                        {""internalType"": ""bool"", ""name"": """", ""type"": ""bool""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""}
                    ],
                    ""name"": ""getAvatar"",
                    ""outputs"": [
                        {""internalType"": ""string"", ""name"": ""username"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""email"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""firstName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""lastName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""avatarType"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""metadata"", ""type"": ""string""}
                    ],
                    ""stateMutability"": ""view"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""username"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""email"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""firstName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""lastName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""avatarType"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""metadata"", ""type"": ""string""}
                    ],
                    ""name"": ""updateAvatar"",
                    ""outputs"": [
                        {""internalType"": ""bool"", ""name"": """", ""type"": ""bool""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""}
                    ],
                    ""name"": ""deleteAvatar"",
                    ""outputs"": [
                        {""internalType"": ""bool"", ""name"": """", ""type"": ""bool""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                }
            ]";
        }

        #endregion

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1,
                // FromWalletPrivateKey removed - not in SendWeb3NFTRequest interface
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }


        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Fantom account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                result.Result = (publicKey, privateKey, string.Empty);
                result.IsError = false;
                result.Message = "Fantom account created successfully. Seed phrase not applicable for direct key generation.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Fantom account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
                var account = wallet.GetAccount(0);

                result.Result = (account.Address, account.PrivateKey);
                result.IsError = false;
                result.Message = "Fantom account restored successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Fantom account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                var account = new Account(senderPrivateKey, BigInteger.Parse(_chainId));
                var web3 = new Web3(account, _rpcEndpoint);

                var bridgePoolAddress = _account?.Address ?? _contractAddress;
                var transactionReceipt = await web3.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(bridgePoolAddress, amount, 2);

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transactionReceipt.TransactionHash,
                    IsSuccessful = transactionReceipt.Status.Value == 1,
                    Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount, 2);

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transactionReceipt.TransactionHash,
                    IsSuccessful = transactionReceipt.Status.Value == 1,
                    Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                var transactionReceipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

                if (transactionReceipt == null)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found.";
                }
                else if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = BridgeTransactionStatus.Completed;
                    result.IsError = false;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Canceled;
                    result.IsError = true;
                    result.Message = "Transaction failed on chain.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Fantom transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

        #region Token Methods (IOASISBlockchainStorageProvider)

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "ToWalletAddress is required");
                    return result;
                }

                // Fantom token transfer via Nethereum (EVM-compatible)
                if (string.IsNullOrWhiteSpace(request.FromTokenAddress))
                {
                    // Native FTM transfer
                    var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                        .TransferEtherAndWaitForReceiptAsync(request.ToWalletAddress, (decimal)request.Amount, 2);
                    result.Result.TransactionResult = transactionReceipt.TransactionHash;
                    result.IsError = false;
                    result.Message = "FTM sent successfully";
                }
                else
                {
                    // ERC-20 token transfer
                    var contract = _web3Client.Eth.GetContract(GetERC20ABI(), request.FromTokenAddress);
                    var transferFunction = contract.GetFunction("transfer");
                    var amountInWei = new HexBigInteger((BigInteger)(request.Amount * 1000000000000000000)); // Convert to wei
                    var transactionReceipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                        _account.Address,
                        new HexBigInteger(21000),
                        null,
                        null,
                        request.ToWalletAddress,
                        amountInWei);
                    result.Result.TransactionResult = transactionReceipt.TransactionHash;
                    result.IsError = false;
                    result.Message = "Token sent successfully on Fantom";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || request.MetaData == null || 
                    !request.MetaData.ContainsKey("TokenAddress") || string.IsNullOrWhiteSpace(request.MetaData["TokenAddress"]?.ToString()) ||
                    !request.MetaData.ContainsKey("MintToWalletAddress") || string.IsNullOrWhiteSpace(request.MetaData["MintToWalletAddress"]?.ToString()))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and mint to wallet address are required in MetaData");
                    return result;
                }

                var tokenAddress = request.MetaData["TokenAddress"].ToString();
                var mintToWalletAddress = request.MetaData["MintToWalletAddress"].ToString();
                var amount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) ? amt : 0m;

                // Fantom token minting via Nethereum (requires ERC-20 mint function)
                var contract = _web3Client.Eth.GetContract(GetERC20ABI(), tokenAddress);
                var mintFunction = contract.GetFunction("mint");
                var amountInWei = new HexBigInteger((BigInteger)(amount * 1000000000000000000));
                var transactionReceipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    mintToWalletAddress,
                    amountInWei);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token minted successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                    return result;
                }

                var senderAccount = new Account(request.OwnerPrivateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);

                // Fantom token burning via Nethereum (requires ERC-20 burn function)
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
                var contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var burnFunction = contract.GetFunction("burn");
                var decimalsFunction = contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = BigInteger.Pow(10, decimals);
                // IBurnWeb3TokenRequest doesn't have Amount property, so we'll burn the full balance
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
                var amountBigInt = balance;
                var transactionReceipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    amountBigInt);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token burned successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // ILockWeb3TokenRequest doesn't have Amount or LockWalletAddress properties
                // Lock token by transferring to bridge pool (OASIS account)
                var senderAccount = new Account(request.FromWalletPrivateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);
                var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
                var transferFunction = contract.GetFunction("transfer");
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var transactionReceipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    bridgePoolAddress,
                    balance);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token locked successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new FantomTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // IUnlockWeb3TokenRequest doesn't have UnlockWalletAddress or Amount properties
                // Unlock token by transferring from bridge pool (OASIS account) to recipient
                // Get recipient from Web3TokenId using real OASIS API
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var unlockedToWalletAddress = "";
                
                // Get wallet address from Web3TokenId using real OASIS API
                if (request.Web3TokenId != Guid.Empty)
                {
                    try
                    {
                        // Query OASIS storage for the locked token record
                        var tokenResult = await OASISResultHelper.WrapAsync(() => 
                            OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider()
                            .Result.LoadHolonAsync(request.Web3TokenId));
                        
                        if (!tokenResult.IsError && tokenResult.Result != null)
                        {
                            // Extract wallet address from token metadata
                            unlockedToWalletAddress = tokenResult.Result.MetaData?.ContainsKey("UnlockedToWalletAddress") == true
                                ? tokenResult.Result.MetaData["UnlockedToWalletAddress"]?.ToString()
                                : tokenResult.Result.MetaData?.ContainsKey("MintToWalletAddress") == true
                                    ? tokenResult.Result.MetaData["MintToWalletAddress"]?.ToString()
                                    : "";
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error getting wallet address from Web3TokenId: {ex.Message}", ex);
                    }
                }
                
                // Fallback: try to get from UnlockedByAvatarId if available
                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress) && request.UnlockedByAvatarId != Guid.Empty)
                {
                    unlockedToWalletAddress = await GetWalletAddressForAvatarAsync(request.UnlockedByAvatarId);
                }
                
                if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is required. Please provide Web3TokenId or UnlockedByAvatarId.");
                    return result;
                }

                var senderAccount = new Account(_privateKey);
                var web3Client = new Web3(senderAccount, _rpcEndpoint);
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(bridgePoolAddress);
                var transferFunction = contract.GetFunction("transfer");
                var transactionReceipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(21000),
                    null,
                    null,
                    unlockedToWalletAddress,
                    balance);
                result.Result.TransactionResult = transactionReceipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Fantom";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Fantom native FTM balance via Nethereum (real implementation)
                // IGetWeb3WalletBalanceRequest doesn't have TokenAddress property
                var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
                result.Result = (double)(balance.Value / (BigInteger)1000000000000000000); // Convert from wei to FTM
                result.IsError = false;
                result.Message = "FTM balance retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Fantom transactions via Nethereum
                var transactions = new List<IWalletTransaction>();
                var blockNumber = await _web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                var limit = request.Limit > 0 ? request.Limit : 10;
                
                for (var i = 0; i < limit && blockNumber.Value > 0; i++)
                {
                    try
                    {
                        var block = await _web3Client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumber);
                        foreach (var tx in block.Transactions)
                        {
                            if (tx.From == request.WalletAddress || tx.To == request.WalletAddress)
                            {
                                var walletTx = new WalletTransaction
                                {
                                    TransactionId = Guid.NewGuid(),
                                    FromWalletAddress = tx.From,
                                    ToWalletAddress = tx.To ?? string.Empty,
                                    Amount = (double)(tx.Value.Value / (BigInteger)1000000000000000000),
                                    Description = $"Fantom transaction: {tx.TransactionHash}"
                                };
                                transactions.Add(walletTx);
                            }
                        }
                        blockNumber = new HexBigInteger(blockNumber.Value - 1);
                    }
                    catch
                    {
                        break;
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Fantom transactions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                // Generate Fantom key pair using Nethereum Account (EVM-compatible)
                // Fantom uses Ethereum-compatible key generation (secp256k1)
                var account = Nethereum.Web3.Accounts.Account.GenerateAccount();
                
                // Create KeyPairAndWallet using KeyHelper but override with Fantom-specific values from Nethereum
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = account.PrivateKey;
                    keyPair.PublicKey = account.PublicKey;
                    keyPair.WalletAddressLegacy = account.Address; // Fantom address (EVM-compatible)
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Fantom key pair generated successfully using Nethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Gets ERC-20 token ABI for Fantom token operations
        /// </summary>
        private string GetERC20ABI()
        {
            return @"[{""constant"":true,""inputs"":[{""name"":"""",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_value"",""type"":""uint256""}],""name"":""burn"",""outputs"":[],""type"":""function""}]";
        }

        #endregion

    }
}


