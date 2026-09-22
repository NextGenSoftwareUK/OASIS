using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.DiscordOASIS
{
    /// <summary>
    /// OASIS provider for Discord via the Discord REST API v10 (bot token auth).
    /// Guild members → Avatars; channel messages → Holons.
    /// Set DISCORD_BOT_TOKEN and DISCORD_GUILD_ID env vars.
    /// </summary>
    public class DiscordOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private const string ApiBase = "https://discord.com/api/v10";
        private string _guildId;

        public DiscordOASIS(string botToken = null, string guildId = null)
        {
            _http = new HttpClient();
            var token = botToken ?? Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? string.Empty;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
            _guildId = guildId ?? Environment.GetEnvironmentVariable("DISCORD_GUILD_ID") ?? string.Empty;

            ProviderName = "DiscordOASIS";
            ProviderDescription = "Discord REST API v10 provider (guild members → Avatars, messages/channels → Holons)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DiscordOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static Avatar MapMemberToAvatar(JsonElement user, JsonElement? member = null)
        {
            var avatar = new Avatar();
            if (avatar.MetaData == null) avatar.MetaData = new Dictionary<string, object>();
            var userId = user.TryGetProperty("id", out var uid) ? uid.GetString() : Guid.NewGuid().ToString();
            avatar.Username = user.TryGetProperty("username", out var un) ? un.GetString() ?? userId : userId;
            avatar.MetaData["discord_id"] = userId;
            if (user.TryGetProperty("global_name", out var gn) && gn.ValueKind != JsonValueKind.Null)
                avatar.MetaData["global_name"] = gn.GetString();
            if (user.TryGetProperty("email", out var em) && em.ValueKind != JsonValueKind.Null)
                avatar.Email = em.GetString();
            if (user.TryGetProperty("avatar", out var av) && av.ValueKind != JsonValueKind.Null)
                avatar.MetaData["avatar_url"] = $"https://cdn.discordapp.com/avatars/{userId}/{av.GetString()}.png";
            if (member.HasValue && member.Value.TryGetProperty("nick", out var nick) && nick.ValueKind != JsonValueKind.Null)
                avatar.MetaData["guild_nick"] = nick.GetString();
            if (member.HasValue && member.Value.TryGetProperty("joined_at", out var ja))
                avatar.MetaData["joined_at"] = ja.GetString();
            return avatar;
        }

        private static Holon MapMessageToHolon(JsonElement msg, string channelId)
        {
            var holon = new Holon();
            if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
            var messageId = msg.TryGetProperty("id", out var mid) ? mid.GetString() : string.Empty;
            holon.Name = msg.TryGetProperty("content", out var content) ? content.GetString() : string.Empty;
            holon.Description = holon.Name;
            holon.MetaData["discord_channel_id"] = channelId;
            holon.MetaData["discord_message_id"] = messageId;
            holon.MetaData["discord_provider_key"] = $"{channelId}:{messageId}";
            if (msg.TryGetProperty("timestamp", out var ts))
            {
                if (DateTime.TryParse(ts.GetString(), out var dt)) holon.CreatedDate = dt;
                holon.MetaData["timestamp"] = ts.GetString();
            }
            if (msg.TryGetProperty("author", out var author))
            {
                holon.MetaData["author_id"] = author.TryGetProperty("id", out var aid) ? aid.GetString() : string.Empty;
                holon.MetaData["author_username"] = author.TryGetProperty("username", out var aun) ? aun.GetString() : string.Empty;
            }
            return holon;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var resp = await _http.GetAsync($"{ApiBase}/users/@me");
                if (resp.IsSuccessStatusCode) { result.Result = true; result.Message = "DiscordOASIS provider activated."; }
                else OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: Gateway check failed ({resp.StatusCode}). Ensure DISCORD_BOT_TOKEN is set.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, Message = "DiscordOASIS provider deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var keys = KeyManager.Instance.GetProviderPublicKeysForAvatarById(id, Core.Enums.ProviderType.DiscordOASIS);
                if (keys.IsError || keys.Result == null) { OASISErrorHandling.HandleError(ref result, "DiscordOASIS: No Discord user ID for avatar GUID."); return result; }
                return await LoadAvatarByProviderKeyAsync(System.Linq.Enumerable.FirstOrDefault(keys.Result) ?? string.Empty, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"{ApiBase}/users/{providerKey}");
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: GET /users/{providerKey} failed ({resp.StatusCode})."); return result; }
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                result.Result = MapMemberToAvatar(doc.RootElement);
                result.Message = $"DiscordOASIS: Loaded user {providerKey}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var all = await LoadAllAvatarsAsync(version);
                if (all.IsError || all.Result == null) { OASISErrorHandling.HandleError(ref result, all.Message); return result; }
                var match = all.Result.FirstOrDefault(a => a.Username == avatarUsername);
                if (match == null) { OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: No member with username '{avatarUsername}'."); return result; }
                result.Result = match;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Email lookup requires OAuth2 — use LoadAvatarByProviderKeyAsync with the user snowflake ID.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (string.IsNullOrEmpty(_guildId)) { OASISErrorHandling.HandleError(ref result, "DiscordOASIS: DISCORD_GUILD_ID must be set to load all members."); return result; }
                var avatars = new List<IAvatar>();
                string after = "0";
                while (true)
                {
                    var resp = await _http.GetAsync($"{ApiBase}/guilds/{_guildId}/members?limit=1000&after={after}");
                    if (!resp.IsSuccessStatusCode) break;
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    var members = doc.RootElement;
                    if (members.GetArrayLength() == 0) break;
                    string lastId = "0";
                    foreach (var member in members.EnumerateArray())
                    {
                        if (!member.TryGetProperty("user", out var user)) continue;
                        avatars.Add(MapMemberToAvatar(user, member));
                        if (user.TryGetProperty("id", out var uid)) lastId = uid.GetString();
                    }
                    if (members.GetArrayLength() < 1000) break;
                    after = lastId;
                }
                result.Result = avatars;
                result.Message = $"DiscordOASIS: Loaded {avatars.Count} guild members.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (string.IsNullOrEmpty(_guildId) || !avatar.MetaData.ContainsKey("discord_id"))
                { OASISErrorHandling.HandleError(ref result, "DiscordOASIS: DISCORD_GUILD_ID and MetaData[\"discord_id\"] required."); return result; }
                var userId = avatar.MetaData["discord_id"]?.ToString();
                var payload = JsonSerializer.Serialize(new { nick = avatar.Username });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var resp = await _http.PatchAsync($"{ApiBase}/guilds/{_guildId}/members/{userId}", content);
                if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NoContent)
                { result.Result = avatar; result.Message = $"DiscordOASIS: Nick updated for user {userId}."; }
                else OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: PATCH failed ({resp.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Avatar detail save not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: DeleteAvatar(Guid) not supported — use DeleteAvatar(string providerKey) with the Discord user ID.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (string.IsNullOrEmpty(_guildId)) { OASISErrorHandling.HandleError(ref result, "DiscordOASIS: DISCORD_GUILD_ID required."); return result; }
                var resp = await _http.DeleteAsync($"{ApiBase}/guilds/{_guildId}/members/{providerKey}");
                if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NoContent)
                { result.Result = true; result.Message = $"DiscordOASIS: Member {providerKey} removed from guild."; }
                else OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: DELETE failed ({resp.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Delete by email not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Delete by username not directly supported — load all members to find the user ID first.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Holon (channel messages) ──────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Use LoadHolonAsync(string providerKey) with '{channelId}:{messageId}' to load a message.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var parts = providerKey.Split(':');
                if (parts.Length < 2) { OASISErrorHandling.HandleError(ref result, "DiscordOASIS: providerKey must be '{channelId}:{messageId}'."); return result; }
                var resp = await _http.GetAsync($"{ApiBase}/channels/{parts[0]}/messages/{parts[1]}");
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: GET message failed ({resp.StatusCode})."); return result; }
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                result.Result = MapMessageToHolon(doc.RootElement, parts[0]);
                result.Message = $"DiscordOASIS: Loaded message {parts[1]}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: LoadAllHolons not supported — use LoadHolonsForParent(channelId) to load messages from a specific channel.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Use LoadHolonsForParent(string channelId) to load messages.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // providerKey = channelId
                var holons = new List<IHolon>();
                string before = null;
                while (true)
                {
                    var url = $"{ApiBase}/channels/{providerKey}/messages?limit=100";
                    if (before != null) url += $"&before={before}";
                    var resp = await _http.GetAsync(url);
                    if (!resp.IsSuccessStatusCode) break;
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    var msgs = doc.RootElement;
                    if (msgs.GetArrayLength() == 0) break;
                    string lastId = null;
                    foreach (var msg in msgs.EnumerateArray())
                    {
                        holons.Add(MapMessageToHolon(msg, providerKey));
                        if (msg.TryGetProperty("id", out var mid)) lastId = mid.GetString();
                    }
                    if (msgs.GetArrayLength() < 100) break;
                    before = lastId;
                }
                result.Result = holons;
                result.Message = $"DiscordOASIS: Loaded {holons.Count} messages from channel {providerKey}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: LoadHolonsByMetaData not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: LoadHolonsByMetaData not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.MetaData == null || (!holon.MetaData.ContainsKey("discord_channel_id") && !holon.MetaData.ContainsKey("discord_provider_key")))
                {
                    OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Set MetaData[\"discord_channel_id\"] to post a new message, or MetaData[\"discord_provider_key\"] = '{channelId}:{messageId}' to edit.");
                    return result;
                }
                string channelId, messageId = null;
                if (holon.MetaData.ContainsKey("discord_provider_key"))
                {
                    var pk = holon.MetaData["discord_provider_key"]?.ToString() ?? string.Empty;
                    var parts = pk.Split(':');
                    channelId = parts[0];
                    if (parts.Length > 1) messageId = parts[1];
                }
                else channelId = holon.MetaData["discord_channel_id"]?.ToString();

                var payload = JsonSerializer.Serialize(new { content = holon.Name ?? holon.Description ?? string.Empty });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage resp;
                if (messageId == null)
                {
                    resp = await _http.PostAsync($"{ApiBase}/channels/{channelId}/messages", content);
                    if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: POST failed ({resp.StatusCode})."); return result; }
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    var newId = doc.RootElement.TryGetProperty("id", out var nid) ? nid.GetString() : string.Empty;
                    if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
                    holon.MetaData["discord_message_id"] = newId;
                    holon.MetaData["discord_channel_id"] = channelId;
                    holon.MetaData["discord_provider_key"] = $"{channelId}:{newId}";
                    result.Result = holon; result.Message = $"DiscordOASIS: Message posted, ID {newId}.";
                }
                else
                {
                    resp = await _http.PatchAsync($"{ApiBase}/channels/{channelId}/messages/{messageId}", content);
                    if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: PATCH failed ({resp.StatusCode})."); return result; }
                    result.Result = holon; result.Message = $"DiscordOASIS: Message {messageId} updated.";
                }
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons)
            {
                var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (!r.IsError && r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved; result.Message = $"DiscordOASIS: Saved {saved.Count} holons.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Use DeleteHolon(string providerKey) with '{channelId}:{messageId}'.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var parts = providerKey.Split(':');
                if (parts.Length < 2) { OASISErrorHandling.HandleError(ref result, "DiscordOASIS: providerKey must be '{channelId}:{messageId}'."); return result; }
                var resp = await _http.DeleteAsync($"{ApiBase}/channels/{parts[0]}/messages/{parts[1]}");
                if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    var holon = new Holon();
                    if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
                    holon.MetaData["discord_provider_key"] = providerKey;
                    result.Result = holon; result.Message = $"DiscordOASIS: Message {parts[1]} deleted.";
                }
                else OASISErrorHandling.HandleError(ref result, $"DiscordOASIS: DELETE failed ({resp.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Search not yet implemented. Use LoadHolonsForParent(channelId) to load messages from a channel.");
            return await Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Import not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Geolocation not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "DiscordOASIS: Geolocation not supported.");
            return result;
        }
    }
}
