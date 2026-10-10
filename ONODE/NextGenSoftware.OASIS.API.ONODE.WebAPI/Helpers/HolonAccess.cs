using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>
    /// Read-access rule for holons: the creator, anyone if the holon is public, and any avatar it has been shared
    /// with (via api/share/share-holon). Sharing grants read access only; writes and deletes stay creator-only.
    /// </summary>
    public static class HolonAccess
    {
        /// <summary>JSON array of avatar IDs the holon is shared with. Source of truth for read access.</summary>
        public const string SharedAvatarIdsMetaKey = "SHARED_AVATAR_IDS";

        /// <summary>
        /// One flag per recipient so "shared with me" is an exact-match metadata lookup, since
        /// <see cref="SharedAvatarIdsMetaKey"/> holds a list and metadata queries match whole values.
        /// </summary>
        public static string SharedWithIndexKey(Guid avatarId) => $"SHARED_WITH_{avatarId:N}";

        public const string SharedWithIndexValue = "true";

        public static bool CanRead(IHolon holon, Guid avatarId) =>
            holon != null && (holon.IsPublic || (avatarId != Guid.Empty &&
                (holon.CreatedByAvatarId == avatarId || GetSharedAvatarIds(holon).Contains(avatarId))));

        public static HashSet<Guid> GetSharedAvatarIds(IHolon holon)
        {
            var ids = new HashSet<Guid>();
            if (holon?.MetaData == null || !holon.MetaData.TryGetValue(SharedAvatarIdsMetaKey, out var raw) || raw == null)
                return ids;

            switch (raw)
            {
                case IEnumerable<Guid> guids:
                    ids.UnionWith(guids);
                    break;
                case JsonElement { ValueKind: JsonValueKind.Array } array:
                    foreach (var item in array.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var g))
                            ids.Add(g);
                    break;
                case JsonElement { ValueKind: JsonValueKind.String } str:
                    AddFromText(str.GetString(), ids);
                    break;
                default:
                    AddFromText(raw.ToString(), ids);
                    break;
            }

            ids.Remove(Guid.Empty);
            return ids;
        }

        // Stored as a JSON array by ShareController, but older writers used a comma-separated list.
        private static void AddFromText(string text, HashSet<Guid> ids)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            if (text.StartsWith("[", StringComparison.Ordinal))
            {
                try
                {
                    foreach (var g in JsonSerializer.Deserialize<Guid[]>(text) ?? Array.Empty<Guid>())
                        ids.Add(g);
                    return;
                }
                catch (JsonException)
                {
                    // Fall through to the comma-separated parser.
                }
            }
            foreach (var token in text.Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (Guid.TryParse(token.Trim('"'), out var g))
                    ids.Add(g);
        }

        /// <summary>
        /// Shares a holon with avatars on behalf of <paramref name="caller"/>. Every transport (REST, GraphQL, gRPC)
        /// must go through this, because sharing grants read access: only the creator or a Wizard may share, and
        /// new recipients are added to the existing list rather than replacing it.
        /// </summary>
        public static async Task<OASISResult<bool>> ShareAsync(
            HolonManager holonManager, Guid holonId, IEnumerable<Guid> avatarIds, IAvatar caller)
        {
            var result = new OASISResult<bool>();
            if (caller == null || caller.Id == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Unauthorized. Sign in to share holons.");
                return result;
            }

            var recipients = avatarIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (recipients.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "At least one valid avatar id must be supplied.");
                return result;
            }

            var holonResult = await holonManager.LoadHolonAsync(holonId);
            if (holonResult == null || holonResult.IsError || holonResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Unable to load holon {holonId}. Reason: {holonResult?.Message}");
                return result;
            }

            var holon = holonResult.Result;
            if (holon.CreatedByAvatarId != caller.Id && caller.AvatarType?.Value != AvatarType.Wizard)
            {
                OASISErrorHandling.HandleError(ref result, "Unauthorized. You can only share holons that you created.");
                return result;
            }

            var sharedIds = GetSharedAvatarIds(holon);
            sharedIds.UnionWith(recipients);
            RecordShares(holon, sharedIds);

            var saveResult = await holonManager.SaveHolonAsync(holon, caller.Id);
            if (saveResult == null || saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Unable to persist shared metadata for holon {holonId}. Reason: {saveResult?.Message}");
                return result;
            }

            result.Result = true;
            result.Message = $"Holon {holonId} shared with {recipients.Count} avatar(s).";
            return result;
        }

        /// <summary>True when a holon has recipients whose "shared with me" index flag is missing (shares made before the index existed).</summary>
        public static bool NeedsShareIndex(IHolon holon)
        {
            var ids = GetSharedAvatarIds(holon);
            return ids.Count > 0 && ids.Any(id =>
                !holon.MetaData.TryGetValue(SharedWithIndexKey(id), out var flag) ||
                !string.Equals(flag?.ToString(), SharedWithIndexValue, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Records the share list and the per-recipient index flags on the holon's metadata.</summary>
        public static void RecordShares(IHolon holon, IEnumerable<Guid> sharedIds)
        {
            holon.MetaData ??= new Dictionary<string, object>();
            var all = sharedIds.Where(id => id != Guid.Empty).Distinct().ToList();
            holon.MetaData[SharedAvatarIdsMetaKey] = JsonSerializer.Serialize(all);
            foreach (var id in all)
                holon.MetaData[SharedWithIndexKey(id)] = SharedWithIndexValue;
        }
    }
}
