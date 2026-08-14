using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

public sealed partial class BaseOASIS
{
    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Export all data for specific avatar from Base blockchain
            var exportRequest = new
            {
                avatarId = avatarId.ToString(),
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(exportRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar", content);
            if (exportResponse.IsSuccessStatusCode)
            {
                var responseContent = await exportResponse.Content.ReadAsStringAsync();
                var exportData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var holons = new List<IHolon>();
                // Parse export data and populate holons list
                if (exportData.TryGetProperty("holons", out var holonsArray))
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = "Avatar data export completed successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Base blockchain: {exportResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Export all data for specific avatar by username from Base blockchain
            var exportRequest = new
            {
                avatarUsername = avatarUsername,
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(exportRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/username", content);
            if (exportResponse.IsSuccessStatusCode)
            {
                var responseContent = await exportResponse.Content.ReadAsStringAsync();
                var exportData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var holons = new List<IHolon>();
                // Parse export data and populate holons list
                if (exportData.TryGetProperty("holons", out var holonsArray))
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = "Avatar data export completed successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Base blockchain: {exportResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Get holons near current location from Base blockchain
            var searchRequest = new
            {
                holonType = Type.ToString(),
                radius = 1000, // 1km radius
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var searchResponse = _httpClient.PostAsync("/api/v1/holons/near", content).Result;
            if (searchResponse.IsSuccessStatusCode)
            {
                var responseContent = searchResponse.Content.ReadAsStringAsync().Result;
                var searchData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var holons = new List<IHolon>();
                // Parse search results and populate holons list
                if (searchData.TryGetProperty("holons", out var holonsArray))
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = "Holons near location retrieved successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get holons near location from Base blockchain: {searchResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near location from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
    {
        var result = new OASISResult<IEnumerable<IPlayer>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Get players near current location from Base blockchain
            var searchRequest = new
            {
                radius = 1000, // 1km radius
                includeOffline = false
            };

            var jsonContent = JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var searchResponse = _httpClient.PostAsync("/api/v1/players/near", content).Result;
            if (searchResponse.IsSuccessStatusCode)
            {
                var responseContent = searchResponse.Content.ReadAsStringAsync().Result;
                var searchData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var players = new List<IPlayer>();
                // Parse search results and populate players list
                if (searchData.TryGetProperty("players", out var playersArray))
                {
                    foreach (var playerElement in playersArray.EnumerateArray())
                    {
                        var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(playerElement.GetRawText());
                        // Avatar implements IHolon, and IPlayer extends IHolon, so we can cast through IHolon
                        if (avatar is IHolon holon)
                        {
                            // Create a Player-like object by wrapping the Avatar
                            // Since there's no Player class, we'll use the Avatar as IHolon and cast to IPlayer
                            // Note: This assumes IPlayer is compatible with IHolon
                            players.Add((IPlayer)(object)holon);
                        }
                    }
                }

                result.Result = players;
                result.IsError = false;
                result.Message = "Players near location retrieved successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to get players near location from Base blockchain: {searchResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting players near location from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).Result;
    }

    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result = new OASISResult<bool>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Import holons to Base blockchain
            var importRequest = new
            {
                holons = holons.Select(h => new
                {
                    id = h.Id.ToString(),
                    name = h.Name,
                    description = h.Description,
                    data = JsonSerializer.Serialize(h),
                    version = h.Version
                }).ToArray()
            };

            var jsonContent = JsonSerializer.Serialize(importRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var importResponse = await _httpClient.PostAsync("/api/v1/import", content);
            if (importResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {holons.Count()} holons to Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Base blockchain: {importResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Load all avatar details from Base blockchain
            var loadRequest = new
            {
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(loadRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var loadResponse = await _httpClient.PostAsync("/api/v1/avatars/details/all", content);
            if (loadResponse.IsSuccessStatusCode)
            {
                var responseContent = await loadResponse.Content.ReadAsStringAsync();
                var loadData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var avatarDetails = new List<IAvatarDetail>();
                // Parse load data and populate avatar details list
                if (loadData.TryGetProperty("avatarDetails", out var avatarDetailsArray))
                {
                    foreach (var avatarDetailElement in avatarDetailsArray.EnumerateArray())
                    {
                        var avatarDetail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(avatarDetailElement.GetRawText());
                        avatarDetails.Add(avatarDetail);
                    }
                }

                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = "All avatar details loaded successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load all avatar details from Base blockchain: {loadResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Load all avatars from Base blockchain
            var loadRequest = new
            {
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(loadRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var loadResponse = await _httpClient.PostAsync("/api/v1/avatars/all", content);
            if (loadResponse.IsSuccessStatusCode)
            {
                var responseContent = await loadResponse.Content.ReadAsStringAsync();
                var loadData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var avatars = new List<IAvatar>();
                // Parse load data and populate avatars list
                if (loadData.TryGetProperty("avatars", out var avatarsArray))
                {
                    foreach (var avatarElement in avatarsArray.EnumerateArray())
                    {
                        var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarElement.GetRawText());
                        avatars.Add(avatar);
                    }
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = "All avatars loaded successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load all avatars from Base blockchain: {loadResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            // Load all holons from Base blockchain
            var loadRequest = new
            {
                holonType = type.ToString(),
                loadChildren = loadChildren,
                recursive = recursive,
                maxChildDepth = maxChildDepth,
                currentChildDepth = curentChildDepth,
                continueOnError = continueOnError,
                loadChildrenFromProvider = loadChildrenFromProvider,
                version = version,
                includeDeleted = false
            };

            var jsonContent = JsonSerializer.Serialize(loadRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            var loadResponse = await _httpClient.PostAsync("/api/v1/holons/all", content);
            if (loadResponse.IsSuccessStatusCode)
            {
                var responseContent = await loadResponse.Content.ReadAsStringAsync();
                var loadData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                var holons = new List<IHolon>();
                // Parse load data and populate holons list
                if (loadData.TryGetProperty("holons", out var holonsArray))
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = "All holons loaded successfully from Base blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Base blockchain: {loadResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Base blockchain: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in LoadAvatarAsync method in BaseOASIS while loading an avatar. Reason: ";

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            AvatarInfo avatarInfo =
                await _contractHandler.QueryAsync<GetAvatarByIdFunction, AvatarInfo>(new()
                {
                    EntityId = avatarEntityId
                });

            if (avatarInfo is null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, $"Avatar (with id {id}) not found!"));
                return result;
            }

            result.Result = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

}
