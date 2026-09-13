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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS
{
    public partial class BNBChainOASIS_Legacy
    {
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load avatar detail by username using smart contract
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAvatarDetailByUsername") + EncodeParameter(username)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatarDetail = ParseBNBChainToAvatarDetail(resultData.GetString());
                        if (avatarDetail != null)
                        {
                            result.Result = avatarDetail;
                            result.IsError = false;
                            result.Message = "Avatar detail loaded from BNB Chain by username successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Avatar detail not found with that username");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found with that username");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Save avatar detail using smart contract with ALL fields
                var avatarDetailData = new
                {
                    avatarDetailId = avatarDetail.Id.ToString(),
                    username = avatarDetail.Username ?? "",
                    email = avatarDetail.Email ?? "",
                    karma = avatarDetail.Karma,
                    xp = avatarDetail.XP,
                    model3D = avatarDetail.Model3D ?? "",
                    umaJson = avatarDetail.UmaJson ?? "",
                    portrait = avatarDetail.Portrait ?? "",
                    dob = avatarDetail.DOB.ToString("O"),
                    address = avatarDetail.Address ?? "",
                    town = avatarDetail.Town ?? "",
                    county = avatarDetail.County ?? "",
                    country = avatarDetail.Country ?? "",
                    postcode = avatarDetail.Postcode ?? "",
                    landline = avatarDetail.Landline ?? "",
                    mobile = avatarDetail.Mobile ?? "",
                    achievements = JsonSerializer.Serialize(avatarDetail.Achievements ?? new List<IAchievement>()),
                    attributes = JsonSerializer.Serialize(avatarDetail.Attributes),
                    aura = JsonSerializer.Serialize(avatarDetail.Aura),
                    chakras = JsonSerializer.Serialize(avatarDetail.Chakras),
                    dimensionLevelIds = JsonSerializer.Serialize(avatarDetail.DimensionLevelIds ?? new Dictionary<DimensionLevel, Guid>()),
                    dimensionLevels = JsonSerializer.Serialize(avatarDetail.DimensionLevels ?? new Dictionary<DimensionLevel, IHolon>()),
                    favouriteColour = avatarDetail.FavouriteColour.ToString(),
                    geneKeys = JsonSerializer.Serialize(avatarDetail.GeneKeys ?? new List<IGeneKey>()),
                    gifts = JsonSerializer.Serialize(avatarDetail.Gifts ?? new List<IAvatarGift>()),
                    heartRateData = JsonSerializer.Serialize(avatarDetail.HeartRateData ?? new List<IHeartRateEntry>()),
                    humanDesign = JsonSerializer.Serialize(avatarDetail.HumanDesign),
                    inventory = JsonSerializer.Serialize(avatarDetail.Inventory ?? new List<IInventoryItem>()),
                    karmaAkashicRecords = JsonSerializer.Serialize(avatarDetail.KarmaAkashicRecords ?? new List<IKarmaAkashicRecord>()),
                    omniverse = JsonSerializer.Serialize(avatarDetail.Omniverse),
                    skills = JsonSerializer.Serialize(avatarDetail.Skills),
                    spells = JsonSerializer.Serialize(avatarDetail.Spells ?? new List<ISpell>()),
                    starcliColour = avatarDetail.STARCLIColour.ToString(),
                    stats = JsonSerializer.Serialize(avatarDetail.Stats),
                    superPowers = JsonSerializer.Serialize(avatarDetail.SuperPowers),
                    metadata = JsonSerializer.Serialize(avatarDetail.MetaData ?? new Dictionary<string, object>())
                };

                // Call smart contract function to create/update avatar detail
                var createAvatarDetailFunction = _contract.GetFunction("createAvatarDetail");
                var gasEstimate = createAvatarDetailFunction.EstimateGasAsync(
                    avatarDetailData.avatarDetailId,
                    avatarDetailData.username,
                    avatarDetailData.email,
                    avatarDetailData.karma,
                    avatarDetailData.xp,
                    avatarDetailData.model3D,
                    avatarDetailData.umaJson,
                    avatarDetailData.portrait,
                    avatarDetailData.dob,
                    avatarDetailData.address,
                    avatarDetailData.town,
                    avatarDetailData.county,
                    avatarDetailData.country,
                    avatarDetailData.postcode,
                    avatarDetailData.landline,
                    avatarDetailData.mobile,
                    avatarDetailData.achievements,
                    avatarDetailData.attributes,
                    avatarDetailData.aura,
                    avatarDetailData.chakras,
                    avatarDetailData.dimensionLevelIds,
                    avatarDetailData.dimensionLevels,
                    avatarDetailData.favouriteColour,
                    avatarDetailData.geneKeys,
                    avatarDetailData.gifts,
                    avatarDetailData.heartRateData,
                    avatarDetailData.humanDesign,
                    avatarDetailData.inventory,
                    avatarDetailData.karmaAkashicRecords,
                    avatarDetailData.omniverse,
                    avatarDetailData.skills,
                    avatarDetailData.spells,
                    avatarDetailData.starcliColour,
                    avatarDetailData.stats,
                    avatarDetailData.superPowers,
                    avatarDetailData.metadata
                ).Result;

                var transactionReceipt = createAvatarDetailFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarDetailData.avatarDetailId,
                    avatarDetailData.username,
                    avatarDetailData.email,
                    avatarDetailData.karma,
                    avatarDetailData.xp,
                    avatarDetailData.model3D,
                    avatarDetailData.umaJson,
                    avatarDetailData.portrait,
                    avatarDetailData.dob,
                    avatarDetailData.address,
                    avatarDetailData.town,
                    avatarDetailData.county,
                    avatarDetailData.country,
                    avatarDetailData.postcode,
                    avatarDetailData.landline,
                    avatarDetailData.mobile,
                    avatarDetailData.achievements,
                    avatarDetailData.attributes,
                    avatarDetailData.aura,
                    avatarDetailData.chakras,
                    avatarDetailData.dimensionLevelIds,
                    avatarDetailData.dimensionLevels,
                    avatarDetailData.favouriteColour,
                    avatarDetailData.geneKeys,
                    avatarDetailData.gifts,
                    avatarDetailData.heartRateData,
                    avatarDetailData.humanDesign,
                    avatarDetailData.inventory,
                    avatarDetailData.karmaAkashicRecords,
                    avatarDetailData.omniverse,
                    avatarDetailData.skills,
                    avatarDetailData.spells,
                    avatarDetailData.starcliColour,
                    avatarDetailData.stats,
                    avatarDetailData.superPowers,
                    avatarDetailData.metadata
                ).Result;

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = $"Avatar detail saved to BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";

                    // Store transaction hash in avatar detail metadata
                    avatarDetail.ProviderMetaData[Core.Enums.ProviderType.BNBChainOASIS]["transactionHash"] = transactionReceipt.TransactionHash;
                    avatarDetail.ProviderMetaData[Core.Enums.ProviderType.BNBChainOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to BNB Chain: {ex.Message}");
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load avatar by provider key using smart contract
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAvatarByProviderKey") + EncodeParameter(providerKey)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatar = ParseBNBChainToAvatar(resultData.GetString());
                        if (avatar != null)
                        {
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded from BNB Chain by provider key successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Avatar not found with that provider key");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found with that provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Export all data for avatar by username using smart contract
                var exportRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("exportAllDataForAvatarByUsername") + EncodeParameter(username)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var holons = ParseBNBChainToHolons(resultData.GetString());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Exported {holons.Count()} holons for avatar {username} from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No data found for avatar on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export data for avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Export all data for avatar by ID using smart contract
                var exportRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("exportAllDataForAvatarById") + EncodeParameter(id.ToString())
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var holons = ParseBNBChainToHolons(resultData.GetString());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Exported {holons.Count()} holons for avatar {id} from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No data found for avatar on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export data for avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        // NFT Provider interface methods
    }
}
