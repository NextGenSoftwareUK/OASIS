using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
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
        public const string SharedAvatarIdsMetaKey = HolonReadGrants.MetadataKey;

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
            // Existing WEB4 explicitly admits the bounded legacy text grammar.
            // New policy consumers use Core's default strict mode until migrated.
            var parsed = HolonReadGrants.Read(holon, allowLegacyText: true);
            return parsed.IsError ? new HashSet<Guid>() : new HashSet<Guid>(parsed.Result);
        }

        /// <summary>
        /// Shares a holon with avatars on behalf of <paramref name="caller"/>. Every transport (REST, GraphQL, gRPC)
        /// must go through this, because sharing grants read access: only the creator or a Wizard may share, and
        /// new recipients are added to the existing list rather than replacing it.
        /// </summary>
        public static Task<OASISResult<bool>> ShareAsync(
            HolonManager holonManager, Guid holonId, IEnumerable<Guid> avatarIds, IAvatar caller) =>
            ShareAsync(holonManager, holonId, avatarIds, caller, writeOptions: null);

        public static Task<OASISResult<bool>> ShareAsync(
            HolonManager holonManager, Guid holonId, IEnumerable<Guid> avatarIds, IAvatar caller,
            CosmicWriteOptions writeOptions) =>
            ChangeSharesAsync(holonManager, holonId, avatarIds, caller, revoke: false, writeOptions);

        /// <summary>
        /// Revokes read recipients using the same owner check and atomic revision
        /// precondition as a grant. A conflict is returned; it is never retried as
        /// an unconditional write. Supply stable write options for a known intent.
        /// </summary>
        public static Task<OASISResult<bool>> RevokeAsync(
            HolonManager holonManager, Guid holonId, IEnumerable<Guid> avatarIds, IAvatar caller,
            CosmicWriteOptions writeOptions = null) =>
            ChangeSharesAsync(holonManager, holonId, avatarIds, caller, revoke: true, writeOptions);

        private static async Task<OASISResult<bool>> ChangeSharesAsync(
            HolonManager holonManager, Guid holonId, IEnumerable<Guid> avatarIds, IAvatar caller,
            bool revoke, CosmicWriteOptions writeOptions)
        {
            if (caller == null || caller.Id == Guid.Empty)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_FORBIDDEN", Message = "Unauthorized. Sign in to change sharing." };

            var input = avatarIds?.Take(HolonReadGrants.MaximumRecipients + 1).ToArray() ?? Array.Empty<Guid>();
            var recipients = input.Where(id => id != Guid.Empty).Distinct().ToList();
            if (recipients.Count == 0 || holonId == Guid.Empty || input.Length > HolonReadGrants.MaximumRecipients)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_INPUT_INVALID",
                    Message = "At least one valid avatar id and a nonempty holon id must be supplied." };

            // Sharing changes only this record. Do not load or write its child graph.
            var loaded = await holonManager.LoadHolonAsync(holonId, loadChildren: false);
            if (loaded == null || loaded.IsError || loaded.IsWarning || loaded.Result == null
                || loaded.Result.Id != holonId)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = loaded?.ErrorCode ?? "COSMIC_SHARE_LOOKUP_INVALID",
                    Message = $"Unable to verify sharing record {holonId}. Reason: {loaded?.Message}",
                    Exception = loaded?.Exception };

            var holon = loaded.Result;
            if (holon.CreatedByAvatarId != caller.Id && caller.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_FORBIDDEN",
                    Message = "Unauthorized. You can only change sharing on holons that you created." };

            var parsed = HolonReadGrants.Read(holon, allowLegacyText: true);
            if (parsed.IsError)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = parsed.ErrorCode, Message = parsed.Message, Exception = parsed.Exception };
            var sharedIds = new HashSet<Guid>(parsed.Result);
            if (revoke) sharedIds.ExceptWith(recipients);
            else sharedIds.UnionWith(recipients);
            if (sharedIds.Count > HolonReadGrants.MaximumRecipients)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = HolonReadGrants.InvalidCode, Message = "Recipient limit exceeded." };
            RecordShares(holon, sharedIds);

            // Conditions bind the accepted provider revision from this load.
            // Unsupported providers and uncertain/conflicting outcomes fail closed.
            var saved = await holonManager.SaveHolonAsync(holon, caller.Id,
                writeOptions ?? new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);
            if (saved == null || saved.IsError || saved.IsWarning || saved.Result == null)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = saved?.ErrorCode ?? "COSMIC_SHARE_SAVE_INVALID",
                    Message = $"Unable to persist sharing for holon {holonId}. Reason: {saved?.Message}",
                    Exception = saved?.Exception };

            return new OASISResult<bool> { Result = true, IsSaved = saved.IsSaved,
                MetaData = saved.MetaData,
                Message = revoke ? $"Read access revoked for {recipients.Count} avatar(s)."
                    : $"Holon {holonId} shared with {recipients.Count} avatar(s)." };
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
            if (holon == null) throw new ArgumentNullException(nameof(holon));
            if (sharedIds == null) throw new ArgumentNullException(nameof(sharedIds));
            var input = sharedIds.Take(HolonReadGrants.MaximumRecipients + 1).ToArray();
            if (input.Length > HolonReadGrants.MaximumRecipients)
                throw new ArgumentException("Recipient limit exceeded.", nameof(sharedIds));
            var encoded = HolonReadGrants.Encode(input.Where(id => id != Guid.Empty));
            if (encoded.IsError) throw new ArgumentException(encoded.Message, nameof(sharedIds));
            var all = input.Where(id => id != Guid.Empty).Distinct().OrderBy(id => id).ToArray();
            holon.MetaData ??= new Dictionary<string, object>();
            // Indexes are derived lookup hints, never independent grants. Remove
            // revoked entries before rebuilding the current canonical set.
            foreach (var key in holon.MetaData.Keys.Where(key =>
                key.StartsWith("SHARED_WITH_", StringComparison.Ordinal)
                && Guid.TryParseExact(key.Substring("SHARED_WITH_".Length), "N", out _)).ToArray())
                holon.MetaData.Remove(key);
            holon.MetaData[SharedAvatarIdsMetaKey] = encoded.Result;
            foreach (var id in all)
                holon.MetaData[SharedWithIndexKey(id)] = SharedWithIndexValue;
        }
    }
}
