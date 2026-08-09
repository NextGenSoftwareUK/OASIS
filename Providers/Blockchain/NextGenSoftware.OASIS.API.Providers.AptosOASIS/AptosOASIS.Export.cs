using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                var saveResult = await SaveHolonsAsync(holons);
                if (saveResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"ImportAsync failed: {saveResult.Message}");
                else
                {
                    result.Result = true;
                    result.Message = saveResult.Message;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in ImportAsync: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            // Export all holons - delegate to LoadAllHolonsAsync
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            // Query Aptos for holons created by this avatar
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                var request = new
                {
                    function = $"{APTOS_CONTRACT_ADDRESS}::oasis::get_holons_by_avatar",
                    arguments = new object[] { id.ToString() },
                    type_arguments = new object[0]
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var aptosResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (aptosResponse.TryGetProperty("0", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonData in holonsArray.EnumerateArray())
                        {
                            var holon = ParseAptosToHolon(holonData);
                            if (holon != null)
                                holons.Add(holon);
                        }
                        
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Exported {holons.Count} holons for avatar {id} from Aptos blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found for avatar on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export data from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting data from Aptos: {ex.Message}");
            }
            return response;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0) => ExportAllDataForAvatarByIdAsync(id, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {username} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0) => ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {email} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0) => ExportAllDataForAvatarByEmailAsync(email, version).Result;
    }
}
