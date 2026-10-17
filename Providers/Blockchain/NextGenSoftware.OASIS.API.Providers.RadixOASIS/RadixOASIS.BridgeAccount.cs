using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public partial class RadixOASIS
{
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
    {
        // First load the avatar to get its ID
        var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
        if (avatarResult.IsError || avatarResult.Result == null)
        {
            return new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true,
                Message = $"Failed to load avatar by email: {avatarResult.Message}"
            };
        }

        // Then export using the ID
        return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                var fallback = ProviderManager.Instance.GetStorageProvider(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                if (fallback != null && fallback != this)
                    return await fallback.ExportAllAsync(version);
                OASISErrorHandling.HandleError(ref result, "No OASIS blueprint configured and no other storage provider available for ExportAll fallback.");
                return result;
            }

            // Query all export data from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "export_all",
                args = new[] { version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var holons = new List<IHolon>();
                foreach (var holonElement in response.Result.EnumerateArray())
                {
                    var holonJson = holonElement.GetRawText();
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                    if (holon != null) holons.Add(holon);
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to export all data from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                var fallback = ProviderManager.Instance.GetStorageProvider(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                if (fallback != null && fallback != this)
                    return await fallback.LoadAvatarByProviderKeyAsync(providerKey, version);
                OASISErrorHandling.HandleError(ref result, "No OASIS blueprint configured and no other storage provider available for LoadAvatarByProviderKey fallback.");
                return result;
            }

            // Query avatar by provider key from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_by_provider_key",
                args = new[] { providerKey, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarJson = response.Result.GetRawText();
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar by provider key from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by provider key from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAvatarDetailByEmailAsync(email, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query avatar detail by email from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_detail_by_email",
                args = new[] { email, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarDetailJson = response.Result.GetRawText();
                var avatarDetail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(avatarDetailJson);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar detail by email from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail by email from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
    {
        return LoadAvatarDetailByEmailAsync(email, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAvatarDetailByUsernameAsync(username, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query avatar detail by username from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_detail_by_username",
                args = new[] { username, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarDetailJson = response.Result.GetRawText();
                var avatarDetail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(avatarDetailJson);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar detail by username from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail by username from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(username, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAllAvatarDetailsAsync(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query all avatar details from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_all_avatar_details",
                args = new[] { version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var detailElement in response.Result.EnumerateArray())
                {
                    var detailJson = detailElement.GetRawText();
                    var avatarDetail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(detailJson);
                    if (avatarDetail != null) avatarDetails.Add(avatarDetail);
                }
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar details from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

}
