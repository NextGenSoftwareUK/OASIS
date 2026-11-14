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
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => ExportWalletHolonsForAvatarAsync(LoadAvatarByEmailAsync(avatarEmailAddress), version);



        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin doesn't support location-based avatar discovery
                // Load all avatars and filter by geospatial distance
                var allAvatarsResult = LoadAllAvatarsAsync(0).GetAwaiter().GetResult();
                if (allAvatarsResult.IsError || allAvatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars: {allAvatarsResult.Message}");
                    return response;
                }

                // Filter avatars by geospatial distance (using metadata for location)
                var nearbyAvatars = new List<IAvatar>();
                var centerLat = geoLat / 1000000.0; // Convert from microdegrees to degrees
                var centerLon = geoLong / 1000000.0;

                foreach (var avatar in allAvatarsResult.Result)
                {
                    if (avatar.MetaData != null && 
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lonObj))
                    {
                        if (double.TryParse(latObj?.ToString(), out var avatarLat) &&
                            double.TryParse(lonObj?.ToString(), out var avatarLon))
                        {
                            var distance = GeoHelper.CalculateDistance(centerLat, centerLon, avatarLat, avatarLon);
                            if (distance <= radiusInMeters)
                            {
                                nearbyAvatars.Add(avatar);
                            }
                        }
                    }
                }

                response.Result = nearbyAvatars;
                response.IsError = false;
                response.Message = $"Found {nearbyAvatars.Count} avatars near location";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in GetAvatarsNearMe: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin doesn't support location-based holon discovery
                // Load all holons and filter by geospatial distance
                var allHolonsResult = LoadAllHolonsAsync(holonType, true, true, 0, 0, true, true, 0).GetAwaiter().GetResult();
                if (allHolonsResult.IsError || allHolonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons: {allHolonsResult.Message}");
                    return response;
                }

                // Filter holons by geospatial distance (using metadata for location)
                var nearbyHolons = new List<IHolon>();
                var centerLat = geoLat / 1000000.0; // Convert from microdegrees to degrees
                var centerLon = geoLong / 1000000.0;

                foreach (var holon in allHolonsResult.Result)
                {
                    if (holon.MetaData != null && 
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lonObj))
                    {
                        if (double.TryParse(latObj?.ToString(), out var holonLat) &&
                            double.TryParse(lonObj?.ToString(), out var holonLon))
                        {
                            var distance = GeoHelper.CalculateDistance(centerLat, centerLon, holonLat, holonLon);
                            if (distance <= radiusInMeters)
                            {
                                nearbyHolons.Add(holon);
                            }
                        }
                    }
                }

                response.Result = nearbyHolons;
                response.IsError = false;
                response.Message = $"Found {nearbyHolons.Count} holons near location";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in GetHolonsNearMe: {ex.Message}");
            }

            return response;
        }



        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(string bitcoinJson)
        {
            using var document = JsonDocument.Parse(bitcoinJson);
            var data = document.RootElement;
            string ReadString(string name) => data.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

            if (data.ValueKind != JsonValueKind.Object
                || !Guid.TryParse(ReadString("id"), out var id) || id == Guid.Empty
                || string.IsNullOrWhiteSpace(ReadString("username"))
                || string.IsNullOrWhiteSpace(ReadString("email"))
                || !Enum.TryParse<AvatarType>(ReadString("avatar_type"), out var type)
                || !Enum.IsDefined(typeof(AvatarType), type)
                || !DateTime.TryParse(ReadString("created_date"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var created)
                || !DateTime.TryParse(ReadString("modified_date"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var modified))
                return null;

            return new Avatar
            {
                Id = id,
                Username = ReadString("username"),
                Email = ReadString("email"),
                FirstName = ReadString("first_name"),
                LastName = ReadString("last_name"),
                AvatarType = new EnumValue<AvatarType>(type),
                CreatedDate = created,
                ModifiedDate = modified,
                MetaData = ReadString("metadata") is string metadata
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(metadata)
                    : new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Parse Bitcoin OP_RETURN data to AvatarDetail (separate object, not derived from avatar).
        /// Supports wrapper format { "oasisType": "AvatarDetail", "value": { ... } } or raw AvatarDetail JSON.
        /// </summary>
        private AvatarDetail ParseBitcoinToAvatarDetail(string bitcoinJson)
        {
            if (string.IsNullOrWhiteSpace(bitcoinJson)) return null;
            try
            {
                var el = JsonSerializer.Deserialize<JsonElement>(bitcoinJson);
                JsonElement toParse = el;
                if (el.TryGetProperty("oasisType", out var type) && type.GetString() == "AvatarDetail" && el.TryGetProperty("value", out var value))
                    toParse = value;
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var detail = JsonSerializer.Deserialize<AvatarDetail>(toParse.GetRawText(), options);
                if (detail != null && detail.Id != Guid.Empty)
                    return detail;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Convert Avatar to Bitcoin blockchain format
        /// </summary>
        private string ConvertAvatarToBitcoin(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
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

        public void Dispose()
        {
            _httpClient?.Dispose();
        }



        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(JsonElement bitcoinData, string identifier)
        {
            try
            {
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{identifier}"),
                    Username = identifier,
                    Email = bitcoinData.TryGetProperty("address", out var address) ? address.GetString() : identifier,
                    FirstName = "Bitcoin",
                    LastName = "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Bitcoin-specific metadata
                if (bitcoinData.TryGetProperty("chain_stats", out var chainStats))
                {
                    if (chainStats.TryGetProperty("funded_txo_sum", out var funded))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_balance"] = funded.GetInt64().ToString();
                    }
                    if (chainStats.TryGetProperty("spent_txo_sum", out var spent))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_spent"] = spent.GetInt64().ToString();
                    }
                }
                if (bitcoinData.TryGetProperty("mempool_stats", out var mempoolStats))
                {
                    if (mempoolStats.TryGetProperty("funded_txo_sum", out var mempoolFunded))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_mempool_balance"] = mempoolFunded.GetInt64().ToString();
                    }
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Holon ParseBitcoinToHolon(string bitcoinJson)
        {
            try
            {
                using var document = JsonDocument.Parse(bitcoinJson);
                var payload = document.RootElement;
                if (payload.ValueKind != JsonValueKind.Object ||
                    !payload.TryGetProperty(nameof(IHolon.Id), out var id) ||
                    id.ValueKind != JsonValueKind.String ||
                    !id.TryGetGuid(out var holonId) || holonId == Guid.Empty ||
                    !payload.TryGetProperty(nameof(IHolon.HolonType), out var type) ||
                    type.ValueKind != JsonValueKind.Number ||
                    !type.TryGetInt32(out var holonType) ||
                    !Enum.IsDefined((HolonType)holonType))
                {
                    return null;
                }

                return payload.Deserialize<Holon>();
            }
            catch (JsonException)
            {
                // OP_RETURN can contain arbitrary bytes from unrelated transactions.
                return null;
            }
        }

    }
}
