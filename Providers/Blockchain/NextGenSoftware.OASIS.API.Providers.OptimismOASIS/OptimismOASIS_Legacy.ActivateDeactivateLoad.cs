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

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Optimism provider is already activated";
                    return response;
                }

                if (!BigInteger.TryParse(_chainId, out var expectedChain) || expectedChain <= 0)
                    throw new InvalidOperationException("Optimism requires a positive configured chain ID.");

                var account = string.IsNullOrEmpty(_privateKey) ? null : new Account(_privateKey, expectedChain);
                var web3 = account == null ? new Web3(_rpcEndpoint) : new Web3(account, _rpcEndpoint);
                var chain = await web3.Eth.ChainId.SendRequestAsync();
                if (chain == null || chain.Value != expectedChain)
                    throw new InvalidOperationException("The Optimism RPC chain ID does not match the configured chain.");

                Contract contract = null;
                if (!string.IsNullOrEmpty(_contractAddress) && _contractAddress != "0x0000000000000000000000000000000000000000")
                {
                    var code = await web3.Eth.GetCode.SendRequestAsync(_contractAddress);
                    if (string.IsNullOrWhiteSpace(code) || code == "0x")
                        throw new InvalidOperationException("The configured Optimism contract has no deployed code.");
                    contract = web3.Eth.GetContract(GetOptimismContractABI(), _contractAddress);
                }

                _account = account;
                _web3Client = web3;
                _contract = contract;
                _isActivated = true;
                response.Result = true;
                response.Message = "Optimism provider activated successfully with Web3 integration.";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Optimism provider: {ex.Message}");
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
                response.Message = "Optimism provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Optimism provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => LoadAvatarByProviderKeyAsync(id.ToString("D"), version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }




        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                // Get players near me from Optimism blockchain
                var queryUrl = "/api/v1/accounts/nearby";

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading avatars: {avatarsResult.Message}");
                    return response;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                response.Result = nearby;
                response.Result = nearby;
                response.IsError = false;
                response.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Optimism: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                // Get holons near me from Optimism blockchain
                var queryUrl = $"/api/v1/accounts/holons?type={holonType}";

                var holonsResult = LoadAllHolons(holonType);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading holons: {holonsResult.Message}");
                    return response;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }
                response.Result = nearby;
                response.IsError = false;
                response.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Optimism: {ex.Message}");
            }

            return response;
        }



        /// <summary>
        /// Convert Avatar to Optimism blockchain format
        /// </summary>
        private string ConvertAvatarToOptimism(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Optimism blockchain structure
                var optimismData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(optimismData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to Optimism blockchain format
        /// </summary>
        private string ConvertHolonToOptimism(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Optimism blockchain structure
                var optimismData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(optimismData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }



        public void Dispose()
        {
            _httpClient?.Dispose();
        }



        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = publicKey;
                }
                result.Result = keyPair;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

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
                    OASISErrorHandling.HandleError(ref result, "Optimism provider is not activated");
                    return result;
                }

                if (_web3Client == null || _account == null)
                    throw new InvalidOperationException("Optimism transfer requires the configured signer.");
                if (!string.Equals(_account.Address, fromWalletAddress, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("The sender address must match the configured Optimism signer.", nameof(fromWalletAddress));
                if (amount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(amount), "The transfer amount must be positive.");

                var receipt = await _web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(toWalletAddress, amount);
                if (receipt?.Status?.Value != BigInteger.One || string.IsNullOrWhiteSpace(receipt.TransactionHash))
                    throw new InvalidOperationException("The Optimism transfer did not receive a successful native receipt.");

                result.Result = new OptimismTransactionResponse
                {
                    TransactionResult = receipt.TransactionHash,
                    MemoText = memoText
                };
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = $"Optimism transaction confirmed: {receipt.TransactionHash}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Optimism transaction: {ex.Message}");
            }

            return result;
        }



    }
}
