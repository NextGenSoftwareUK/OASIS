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
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>
    /// Read-access rule for holons: the creator, anyone if the holon is public, and any avatar it has been shared
    /// with (via api/share/share-holon). Sharing grants read access only; write, delete, add-children and manage are granted per holon with SetPermissionsAsync.
    /// </summary>
    public static class HolonAccess
    {
        /// <summary>JSON array of avatar IDs the holon is shared with. Source of truth for read access.</summary>
        public const string SharedAvatarIdsMetaKey = HolonReadGrants.MetadataKey;

        /// <summary>
        /// One flag per recipient so "shared with me" is an exact-match metadata lookup, since
        /// <see cref="SharedAvatarIdsMetaKey"/> holds a list and metadata queries match whole values.
        /// </summary>
        public static string SharedWithIndexKey(Guid avatarId) => HolonReadIndex.KeyFor(avatarId);

        public const string SharedWithIndexValue = "true";

        /// <summary>True when the avatar may be given the whole holon. Field-limited readers need <see cref="Project"/>.</summary>
        public static bool CanRead(IHolon holon, Guid avatarId) =>
            holon != null && HolonPermissions.Allows(holon, avatarId, HolonPermission.Read);

        /// <summary>
        /// The holon as this avatar may see it: the holon itself, a copy holding only the fields they may read, or null.
        /// Resolves the holon's own grants only; the manager's loads additionally apply groups and inherited grants.
        /// </summary>
        public static IHolon Project(IHolon holon, Guid avatarId) =>
            holon == null ? null : new HolonAccessResolver(avatarId, includePublic: true).Project(holon);

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

        private static Task<OASISResult<bool>> ChangeSharesAsync(
            HolonManager holonManager, Guid holonId, IEnumerable<Guid> avatarIds, IAvatar caller,
            bool revoke, CosmicWriteOptions writeOptions)
        {
            var input = avatarIds?.Take(HolonReadGrants.MaximumRecipients + 1).ToArray() ?? Array.Empty<Guid>();
            var recipients = input.Where(id => id != Guid.Empty).Distinct().ToList();
            if (recipients.Count == 0 || holonId == Guid.Empty || input.Length > HolonReadGrants.MaximumRecipients)
                return Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_INPUT_INVALID",
                    Message = "At least one valid avatar id and a nonempty holon id must be supplied." });

            return ChangeAccessAsync(holonManager, holonId, caller, writeOptions, holon =>
            {
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
                return null;
            }, revoke ? $"Read access revoked for {recipients.Count} avatar(s)."
                : $"Holon {holonId} shared with {recipients.Count} avatar(s).");
        }

        /// <summary>The stored permission set of a holon (empty if none, or if the stored value is malformed).</summary>
        public static HolonPermissionSet GetPermissions(IHolon holon)
        {
            var stored = HolonPermissionSet.Read(holon);
            return stored.IsError ? HolonPermissionSet.Empty : stored.Result;
        }

        /// <summary>
        /// Changes who may do what with the holon, as a whole and field by field. Only an avatar with
        /// <see cref="HolonPermission.Manage"/> (the creator always has it) or a Wizard may. An empty change marks the holon
        /// as restricted: creating children under it then needs AddChildren.
        /// </summary>
        public static Task<OASISResult<bool>> SetPermissionsAsync(
            HolonManager holonManager, Guid holonId, HolonPermissionChange change, IAvatar caller,
            CosmicWriteOptions writeOptions = null)
        {
            var invalid = change == null ? "A permission change is required." : change.Validate();
            if (holonId == Guid.Empty || invalid != null)
                return Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_PERMISSIONS_INPUT_INVALID",
                    Message = holonId == Guid.Empty ? "A nonempty holon id is required." : invalid });

            return ChangeAccessAsync(holonManager, holonId, caller, writeOptions, holon =>
            {
                var applied = change.ApplyTo(holon);
                return applied.IsError ? applied : null;
            }, $"Permissions updated on holon {holonId}.");
        }

        /// <summary>
        /// Creates a group (or role): a holon whose members can be granted permissions as one. The creator owns it and
        /// controls its membership.
        /// </summary>
        public static async Task<OASISResult<IHolon>> CreateGroupAsync(
            HolonManager holonManager, string name, IEnumerable<Guid> members, IAvatar caller)
        {
            var callerId = caller?.Id ?? Guid.Empty;
            if (callerId == Guid.Empty || string.IsNullOrWhiteSpace(name))
                return new OASISResult<IHolon> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_INPUT_INVALID",
                    Message = "Sign in and give the group a name." };
            var group = new Holon { Id = Guid.Empty, Name = name.Trim(), HolonType = HolonType.Holon,
                CustomHolonType = HolonGroup.CustomType };
            var set = HolonGroup.SetMembers(group, (members ?? Array.Empty<Guid>()).Where(m => m != Guid.Empty).Distinct().ToArray());
            if (set.IsError)
                return new OASISResult<IHolon> { IsError = true, ErrorCount = 1, ErrorCode = set.ErrorCode, Message = set.Message };
            return await holonManager.SaveHolonAsync(group, callerId, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);
        }

        /// <summary>Adds and removes group members. Needs Manage on the group (its creator has it).</summary>
        public static Task<OASISResult<bool>> SetGroupMembersAsync(
            HolonManager holonManager, Guid groupId, IEnumerable<Guid> add, IEnumerable<Guid> remove, IAvatar caller,
            CosmicWriteOptions writeOptions = null)
        {
            var toAdd = (add ?? Array.Empty<Guid>()).Where(m => m != Guid.Empty).ToList();
            var toRemove = new HashSet<Guid>(remove ?? Array.Empty<Guid>());
            if (groupId == Guid.Empty || (toAdd.Count == 0 && toRemove.Count == 0))
                return Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_INPUT_INVALID",
                    Message = "A group id and at least one member to add or remove are required." });
            return ChangeAccessAsync(holonManager, groupId, caller, writeOptions, holon =>
            {
                if (!HolonGroup.IsGroup(holon))
                    return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_GROUP_INPUT_INVALID",
                        Message = "That holon is not a group." };
                var members = HolonGroup.Members(holon);
                if (members.IsError)
                    return new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = members.ErrorCode, Message = members.Message };
                var next = new HashSet<Guid>(members.Result);
                next.UnionWith(toAdd);
                next.ExceptWith(toRemove);
                var set = HolonGroup.SetMembers(holon, next);
                return set.IsError ? set : null;
            }, $"Group {groupId} membership updated.");
        }

        /// <summary>Rebuilds the "shared with" lookup flags from what the holon grants (see <see cref="HolonReadIndex"/>).</summary>
        public static void RebuildReadIndex(IHolon holon) => HolonReadIndex.Rebuild(holon);

        /// <summary>The index flag for a holon granted to everyone.</summary>
        public const string SharedWithPublicKey = HolonReadIndex.PublicKey;

        /// <summary>
        /// The one load, check, change, conditional-save path for anything that changes who may access a holon.
        /// <paramref name="mutate"/> edits the loaded holon and returns an error result, or null to proceed.
        /// </summary>
        private static async Task<OASISResult<bool>> ChangeAccessAsync(
            HolonManager holonManager, Guid holonId, IAvatar caller, CosmicWriteOptions writeOptions,
            Func<IHolon, OASISResult<bool>> mutate, string successMessage)
        {
            var callerId = caller?.Id ?? Guid.Empty;
            var callerIsWizard = caller?.AvatarType?.Value == AvatarType.Wizard;
            if (callerId == Guid.Empty)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_FORBIDDEN", Message = "Unauthorized. Sign in to change sharing." };

            // Access changes touch only this record. Do not load or write its child graph.
            var authority = ProviderType.Default;
            OASISResult<IHolon> loaded;
            if (holonManager.OASISDNA?.OASIS?.HyperDriveMode == HyperDriveModes.V2)
            {
                var observed = await holonManager.LoadHolonForWriteAsync(holonId, callerId);
                if (observed.IsError || observed.IsWarning || observed.Result == null)
                    return ShareFailure(observed, "COSMIC_SHARE_LOOKUP_INVALID");
                authority = observed.Result.Authority;
                loaded = new OASISResult<IHolon>(observed.Result.Record) { IsLoaded = true };
            }
            else loaded = await holonManager.LoadHolonAsync(holonId, loadChildren: false);
            if (loaded == null || loaded.IsError || loaded.IsWarning || loaded.Result == null
                || loaded.Result.Id != holonId)
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = loaded?.ErrorCode ?? "COSMIC_SHARE_LOOKUP_INVALID",
                    Message = $"Unable to verify sharing record {holonId}. Reason: {loaded?.Message}",
                    Exception = loaded?.Exception };

            var holon = loaded.Result;
            if (!callerIsWizard && !await holonManager.AllowsAsync(holon, callerId, HolonPermission.Manage))
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = "COSMIC_SHARE_FORBIDDEN",
                    Message = "Unauthorized. Only the creator, or an avatar granted Manage, can change access to this holon." };

            var failure = mutate(holon);
            if (failure != null) return failure;

            // Conditions bind the accepted provider revision from this load.
            // Unsupported providers and uncertain/conflicting outcomes fail closed.
            var saved = await holonManager.SaveHolonAsync(holon, callerId,
                writeOptions ?? new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: authority);
            if (saved == null || saved.IsError || saved.IsWarning || saved.Result == null)
            {
                if (holonManager.OASISDNA?.OASIS?.HyperDriveMode == HyperDriveModes.V2)
                    return ShareFailure(saved, "COSMIC_SHARE_SAVE_INVALID");
                return new OASISResult<bool> { IsError = true, ErrorCount = 1,
                    ErrorCode = saved?.ErrorCode ?? "COSMIC_SHARE_SAVE_INVALID",
                    Message = $"Unable to persist access change for holon {holon.Id}. Reason: {saved?.Message}",
                    Exception = saved?.Exception };
            }

            return new OASISResult<bool> { Result = true, IsSaved = saved.IsSaved,
                MetaData = saved.MetaData, Message = successMessage };
        }

        private static OASISResult<bool> ShareFailure<T>(OASISResult<T> source, string invalidCode)
        {
            var result = source == null ? new OASISResult<bool>()
                : OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T, bool>(source);
            result.IsError = true;
            result.ErrorCount = Math.Max(1, result.ErrorCount);
            if (string.IsNullOrEmpty(result.ErrorCode) || result.ErrorCode == "COSMIC_AUTHORITY_READ_INVALID")
                result.ErrorCode = invalidCode;
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
            if (holon == null) throw new ArgumentNullException(nameof(holon));
            if (sharedIds == null) throw new ArgumentNullException(nameof(sharedIds));
            var input = sharedIds.Take(HolonReadGrants.MaximumRecipients + 1).ToArray();
            if (input.Length > HolonReadGrants.MaximumRecipients)
                throw new ArgumentException("Recipient limit exceeded.", nameof(sharedIds));
            var encoded = HolonReadGrants.Encode(input.Where(id => id != Guid.Empty));
            if (encoded.IsError) throw new ArgumentException(encoded.Message, nameof(sharedIds));
            holon.MetaData ??= new Dictionary<string, object>();
            holon.MetaData[SharedAvatarIdsMetaKey] = encoded.Result;
            RebuildReadIndex(holon);
        }
    }
}
