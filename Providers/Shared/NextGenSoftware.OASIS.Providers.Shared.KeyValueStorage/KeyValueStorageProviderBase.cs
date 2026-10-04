using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage
{
    /// <summary>
    /// Complete <see cref="IOASISStorageProvider"/> implementation over any <see cref="IKeyValueBackend"/>.
    /// Layout (all keys are prefixed with <see cref="KeyPrefix"/>):
    ///   avatar/{id}, avatar-username/{b64}, avatar-email/{b64}, avatardetail/{id},
    ///   holon/{id}, holon-parent/{parentId}/{id}
    /// The provider unique storage key for an avatar or holon is its id.
    /// </summary>
    public abstract class KeyValueStorageProviderBase : OASISStorageProviderBase, IOASISStorageProvider
    {
        private const int MaxParallelReads = 8;

        protected KeyValueStorageProviderBase(IKeyValueBackend backend, string keyPrefix = "oasis/")
        {
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            KeyPrefix = keyPrefix ?? string.Empty;
        }

        protected IKeyValueBackend Backend { get; }

        protected string KeyPrefix { get; }

        private string Name => ProviderName ?? GetType().Name;

        private ProviderType StorageType => ProviderType.Value;

        #region Keys

        private string AvatarKey(Guid id) => $"{KeyPrefix}avatar/{id:N}";
        private string AvatarDetailKey(Guid id) => $"{KeyPrefix}avatardetail/{id:N}";
        private string UsernameKey(string username) => $"{KeyPrefix}avatar-username/{Encode(username)}";
        private string EmailKey(string email) => $"{KeyPrefix}avatar-email/{Encode(email)}";
        private string HolonKey(Guid id) => $"{KeyPrefix}holon/{id:N}";
        private string ParentPrefix(Guid parentId) => $"{KeyPrefix}holon-parent/{parentId:N}/";
        private string ParentKey(Guid parentId, Guid id) => $"{ParentPrefix(parentId)}{id:N}";

        // Usernames and emails may contain characters some services reject in keys; base64url keeps keys portable.
        private static string Encode(string value)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant())).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static Guid IdFromKey(string key) => Guid.ParseExact(key[(key.LastIndexOf('/') + 1)..], "N");

        #endregion

        #region Activation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await Backend.VerifyAsync();
                IsProviderActivated = true;
                result.Result = true;
                result.Message = $"{Name} activated.";
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                OASISErrorHandling.HandleError(ref result, $"{Name} could not reach its store: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = $"{Name} deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        #endregion

        #region Low-level helpers

        private async Task<T> ReadAsync<T>(string key) where T : class
        {
            var json = await Backend.GetAsync(key);
            return json == null ? null : OasisJson.Deserialize<T>(json);
        }

        private async Task<Guid?> ReadIdAsync(string key)
        {
            var value = await Backend.GetAsync(key);
            return Guid.TryParse(value, out var id) ? id : null;
        }

        private async Task<List<T>> ReadAllAsync<T>(IEnumerable<string> keys) where T : class
        {
            using var gate = new SemaphoreSlim(MaxParallelReads);
            var tasks = keys.Select(async key =>
            {
                await gate.WaitAsync();
                try { return await ReadAsync<T>(key); }
                finally { gate.Release(); }
            });
            return (await Task.WhenAll(tasks)).Where(x => x != null).ToList();
        }

        private OASISResult<T> Fail<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"{Name}: {message}", ex);
            return result;
        }

        private static OASISResult<T> Ok<T>(T value, string message = null) => new(value) { IsLoaded = true, Message = message };

        private static bool ParseKey(string providerKey, out Guid id) => Guid.TryParse(providerKey, out id);

        #endregion

        #region Avatars

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            try
            {
                if (avatar == null) return Fail<IAvatar>("Avatar is required.");
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[StorageType] = avatar.Id.ToString();

                var previous = await ReadAsync<IAvatar>(AvatarKey(avatar.Id));
                if (previous != null && !string.IsNullOrWhiteSpace(previous.Username) && !string.Equals(previous.Username, avatar.Username, StringComparison.OrdinalIgnoreCase))
                    await Backend.DeleteAsync(UsernameKey(previous.Username));
                if (previous != null && !string.IsNullOrWhiteSpace(previous.Email) && !string.Equals(previous.Email, avatar.Email, StringComparison.OrdinalIgnoreCase))
                    await Backend.DeleteAsync(EmailKey(previous.Email));

                if (!string.IsNullOrWhiteSpace(avatar.Username))
                {
                    var owner = await ReadIdAsync(UsernameKey(avatar.Username));
                    if (owner.HasValue && owner.Value != avatar.Id) return Fail<IAvatar>($"Username '{avatar.Username}' is already taken.");
                }
                if (!string.IsNullOrWhiteSpace(avatar.Email))
                {
                    var owner = await ReadIdAsync(EmailKey(avatar.Email));
                    if (owner.HasValue && owner.Value != avatar.Id) return Fail<IAvatar>($"Email '{avatar.Email}' is already registered.");
                }

                await Backend.PutAsync(AvatarKey(avatar.Id), OasisJson.Serialize(avatar));
                if (!string.IsNullOrWhiteSpace(avatar.Username)) await Backend.PutAsync(UsernameKey(avatar.Username), avatar.Id.ToString());
                if (!string.IsNullOrWhiteSpace(avatar.Email)) await Backend.PutAsync(EmailKey(avatar.Email), avatar.Id.ToString());

                return new OASISResult<IAvatar>(avatar) { IsSaved = true, Message = $"Avatar {avatar.Id} saved to {Name}." };
            }
            catch (Exception ex) { return Fail<IAvatar>($"Error saving avatar: {ex.Message}", ex); }
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            try
            {
                var avatar = await ReadAsync<IAvatar>(AvatarKey(id));
                return avatar == null ? Fail<IAvatar>($"Avatar {id} not found.") : Ok(avatar);
            }
            catch (Exception ex) { return Fail<IAvatar>($"Error loading avatar {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => ParseKey(providerKey, out var id) ? await LoadAvatarAsync(id, version) : Fail<IAvatar>($"'{providerKey}' is not a {Name} provider key (expected an avatar id).");

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

        private async Task<OASISResult<IAvatar>> LoadAvatarByIndexAsync(string indexKey, string description, int version)
        {
            try
            {
                var id = await ReadIdAsync(indexKey);
                return id.HasValue ? await LoadAvatarAsync(id.Value, version) : Fail<IAvatar>($"No avatar with {description}.");
            }
            catch (Exception ex) { return Fail<IAvatar>($"Error loading avatar with {description}: {ex.Message}", ex); }
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => string.IsNullOrWhiteSpace(avatarUsername) ? Task.FromResult(Fail<IAvatar>("Username is required.")) : LoadAvatarByIndexAsync(UsernameKey(avatarUsername), $"username '{avatarUsername}'", version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => string.IsNullOrWhiteSpace(avatarEmail) ? Task.FromResult(Fail<IAvatar>("Email is required.")) : LoadAvatarByIndexAsync(EmailKey(avatarEmail), $"email '{avatarEmail}'", version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                var keys = await Backend.ListKeysAsync($"{KeyPrefix}avatar/");
                return Ok<IEnumerable<IAvatar>>(await ReadAllAsync<IAvatar>(keys));
            }
            catch (Exception ex) { return Fail<IEnumerable<IAvatar>>($"Error loading avatars: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var avatar = await ReadAsync<IAvatar>(AvatarKey(id));
                if (avatar == null) return Fail<bool>($"Avatar {id} not found.");

                if (softDelete)
                {
                    avatar.DeletedDate = DateTime.UtcNow;
                    avatar.IsActive = false;
                    await Backend.PutAsync(AvatarKey(id), OasisJson.Serialize(avatar));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(avatar.Username)) await Backend.DeleteAsync(UsernameKey(avatar.Username));
                    if (!string.IsNullOrWhiteSpace(avatar.Email)) await Backend.DeleteAsync(EmailKey(avatar.Email));
                    await Backend.DeleteAsync(AvatarDetailKey(id));
                    await Backend.DeleteAsync(AvatarKey(id));
                }
                return new OASISResult<bool>(true) { IsDeleted = true, Message = $"Avatar {id} {(softDelete ? "soft" : "permanently")} deleted from {Name}." };
            }
            catch (Exception ex) { return Fail<bool>($"Error deleting avatar {id}: {ex.Message}", ex); }
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => ParseKey(providerKey, out var id) ? await DeleteAvatarAsync(id, softDelete) : Fail<bool>($"'{providerKey}' is not a {Name} provider key (expected an avatar id).");

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmail);
            return avatar.IsError ? Fail<bool>(avatar.Message) : await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername);
            return avatar.IsError ? Fail<bool>(avatar.Message) : await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).GetAwaiter().GetResult();

        #endregion

        #region Avatar details

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            try
            {
                if (avatarDetail == null) return Fail<IAvatarDetail>("Avatar detail is required.");
                if (avatarDetail.Id == Guid.Empty) return Fail<IAvatarDetail>("Avatar detail must carry its avatar's id.");
                avatarDetail.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatarDetail.ProviderUniqueStorageKey[StorageType] = avatarDetail.Id.ToString();
                await Backend.PutAsync(AvatarDetailKey(avatarDetail.Id), OasisJson.Serialize(avatarDetail));
                return new OASISResult<IAvatarDetail>(avatarDetail) { IsSaved = true, Message = $"Avatar detail {avatarDetail.Id} saved to {Name}." };
            }
            catch (Exception ex) { return Fail<IAvatarDetail>($"Error saving avatar detail: {ex.Message}", ex); }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                var detail = await ReadAsync<IAvatarDetail>(AvatarDetailKey(id));
                return detail == null ? Fail<IAvatarDetail>($"Avatar detail {id} not found.") : Ok(detail);
            }
            catch (Exception ex) { return Fail<IAvatarDetail>($"Error loading avatar detail {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            return avatar.IsError ? Fail<IAvatarDetail>(avatar.Message) : await LoadAvatarDetailAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmail, version);
            return avatar.IsError ? Fail<IAvatarDetail>(avatar.Message) : await LoadAvatarDetailAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                var keys = await Backend.ListKeysAsync($"{KeyPrefix}avatardetail/");
                return Ok<IEnumerable<IAvatarDetail>>(await ReadAllAsync<IAvatarDetail>(keys));
            }
            catch (Exception ex) { return Fail<IEnumerable<IAvatarDetail>>($"Error loading avatar details: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        #endregion

        #region Holons

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await SaveHolonInternalAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

        private async Task<OASISResult<IHolon>> SaveHolonInternalAsync(IHolon holon, bool saveChildren, bool recursive, int maxChildDepth, int depth, bool continueOnError)
        {
            try
            {
                if (holon == null) return Fail<IHolon>("Holon is required.");
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                holon.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                holon.ProviderUniqueStorageKey[StorageType] = holon.Id.ToString();

                var previous = await ReadAsync<IHolon>(HolonKey(holon.Id));
                if (previous != null && previous.ParentHolonId != Guid.Empty && previous.ParentHolonId != holon.ParentHolonId)
                    await Backend.DeleteAsync(ParentKey(previous.ParentHolonId, holon.Id));

                await Backend.PutAsync(HolonKey(holon.Id), OasisJson.Serialize(holon));
                if (holon.ParentHolonId != Guid.Empty)
                    await Backend.PutAsync(ParentKey(holon.ParentHolonId, holon.Id), holon.Id.ToString());

                var result = new OASISResult<IHolon>(holon) { IsSaved = true, Message = $"Holon {holon.Id} saved to {Name}." };
                var reachedDepth = maxChildDepth > 0 && depth + 1 >= maxChildDepth;
                if (saveChildren && holon.Children != null && (depth == 0 || recursive) && !reachedDepth)
                {
                    foreach (var child in holon.Children)
                    {
                        child.ParentHolonId = holon.Id;
                        var saved = await SaveHolonInternalAsync(child, saveChildren, recursive, maxChildDepth, depth + 1, continueOnError);
                        if (saved.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{Name}: error saving child holon {child.Id}: {saved.Message}");
                            if (!continueOnError) return result;
                        }
                    }
                }
                return result;
            }
            catch (Exception ex) { return Fail<IHolon>($"Error saving holon: {ex.Message}", ex); }
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (holons == null) return Fail<IEnumerable<IHolon>>("Holons are required.");
            var saved = new List<IHolon>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonInternalAsync(holon, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
                if (r.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{Name}: error saving holon {holon?.Id}: {r.Message}");
                    if (!continueOnError) return result;
                }
                else saved.Add(r.Result);
            }
            result.Result = saved;
            result.IsSaved = !result.IsError;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var holon = await ReadAsync<IHolon>(HolonKey(id));
                if (holon == null) return Fail<IHolon>($"Holon {id} not found.");
                if (loadChildren)
                {
                    var children = await LoadChildrenAsync(holon.Id, HolonType.All, recursive, maxChildDepth, 0);
                    if (children.IsError && !continueOnError) return Fail<IHolon>(children.Message);
                    holon.Children = children.Result?.ToList() ?? new List<IHolon>();
                }
                return Ok(holon);
            }
            catch (Exception ex) { return Fail<IHolon>($"Error loading holon {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ParseKey(providerKey, out var id)
                ? await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version)
                : Fail<IHolon>($"'{providerKey}' is not a {Name} provider key (expected a holon id).");

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadChildrenAsync(Guid parentId, HolonType type, bool recursive, int maxChildDepth, int depth)
        {
            var keys = await Backend.ListKeysAsync(ParentPrefix(parentId));
            var children = await ReadAllAsync<IHolon>(keys.Select(k => HolonKey(IdFromKey(k))));
            var reachedDepth = maxChildDepth > 0 && depth + 1 >= maxChildDepth;
            if (recursive && !reachedDepth)
            {
                foreach (var child in children)
                {
                    var grandChildren = await LoadChildrenAsync(child.Id, HolonType.All, true, maxChildDepth, depth + 1);
                    child.Children = grandChildren.Result?.ToList() ?? new List<IHolon>();
                }
            }
            return Ok<IEnumerable<IHolon>>(children.Where(h => type == HolonType.All || h.HolonType == type).ToList());
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try { return await LoadChildrenAsync(id, type, loadChildren && recursive, maxChildDepth, curentChildDepth); }
            catch (Exception ex) { return Fail<IEnumerable<IHolon>>($"Error loading holons for parent {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ParseKey(providerKey, out var id)
                ? await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version)
                : Fail<IEnumerable<IHolon>>($"'{providerKey}' is not a {Name} provider key (expected a holon id).");

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var keys = await Backend.ListKeysAsync($"{KeyPrefix}holon/");
                var holons = await ReadAllAsync<IHolon>(keys);
                return Ok<IEnumerable<IHolon>>(holons.Where(h => type == HolonType.All || h.HolonType == type).ToList());
            }
            catch (Exception ex) { return Fail<IEnumerable<IHolon>>($"Error loading holons: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        private static bool MetaMatches(IHolon holon, string key, string value)
            => holon.MetaData != null && holon.MetaData.TryGetValue(key, out var v) && string.Equals(v?.ToString(), value, StringComparison.Ordinal);

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            return all.IsError ? all : Ok<IEnumerable<IHolon>>(all.Result.Where(h => MetaMatches(h, metaKey, metaValue)).ToList());
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0) return Fail<IEnumerable<IHolon>>("At least one metadata key/value pair is required.");
            var all = await LoadAllHolonsAsync(type);
            if (all.IsError) return all;
            bool Match(IHolon h) => metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any
                ? metaKeyValuePairs.Any(kv => MetaMatches(h, kv.Key, kv.Value))
                : metaKeyValuePairs.All(kv => MetaMatches(h, kv.Key, kv.Value));
            return Ok<IEnumerable<IHolon>>(all.Result.Where(Match).ToList());
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            try
            {
                var holon = await ReadAsync<IHolon>(HolonKey(id));
                if (holon == null) return Fail<IHolon>($"Holon {id} not found.");
                if (holon.ParentHolonId != Guid.Empty) await Backend.DeleteAsync(ParentKey(holon.ParentHolonId, id));
                foreach (var childLink in await Backend.ListKeysAsync(ParentPrefix(id)))
                    await Backend.DeleteAsync(childLink);
                await Backend.DeleteAsync(HolonKey(id));
                return new OASISResult<IHolon>(holon) { IsDeleted = true, Message = $"Holon {id} deleted from {Name}." };
            }
            catch (Exception ex) { return Fail<IHolon>($"Error deleting holon {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => ParseKey(providerKey, out var id) ? await DeleteHolonAsync(id) : Fail<IHolon>($"'{providerKey}' is not a {Name} provider key (expected a holon id).");

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        #endregion

        #region Search, import and export

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var groups = searchParams?.SearchGroups?.OfType<ISearchTextGroup>().Where(g => !string.IsNullOrWhiteSpace(g.SearchQuery)).ToList();
                if (groups == null || groups.Count == 0) return Fail<ISearchResults>("Search requires at least one text search group with a query.");

                var results = new SearchResults();
                List<IAvatar> avatars = null;
                List<IHolon> holons = null;

                foreach (var group in groups)
                {
                    var q = group.SearchQuery.Trim();
                    bool Has(string s) => s != null && s.Contains(q, StringComparison.OrdinalIgnoreCase);
                    var searchAvatars = group.SearchAvatars || !group.SearchHolons;
                    var searchHolons = group.SearchHolons || !group.SearchAvatars;

                    if (searchAvatars)
                    {
                        if (avatars == null)
                        {
                            var all = await LoadAllAvatarsAsync(version);
                            if (all.IsError) return Fail<ISearchResults>(all.Message);
                            avatars = all.Result.ToList();
                        }
                        results.SearchResultAvatars.AddRange(avatars.Where(a => Has(a.Username) || Has(a.Email) || Has(a.FirstName) || Has(a.LastName)
                            || (group.SearchIds && Has(a.Id.ToString()))).Where(a => !results.SearchResultAvatars.Any(x => x.Id == a.Id)));
                    }

                    if (searchHolons)
                    {
                        if (holons == null)
                        {
                            var all = await LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, continueOnError, false, version);
                            if (all.IsError) return Fail<ISearchResults>(all.Message);
                            holons = all.Result.ToList();
                        }
                        results.SearchResultHolons.AddRange(holons.Where(h => Has(h.Name) || Has(h.Description)
                            || (group.SearchIds && Has(h.Id.ToString()))).Where(h => !results.SearchResultHolons.Any(x => x.Id == h.Id)));
                    }
                }

                results.NumberOfResults = results.SearchResultAvatars.Count + results.SearchResultHolons.Count;
                return Ok<ISearchResults>(results);
            }
            catch (Exception ex) { return Fail<ISearchResults>($"Error searching: {ex.Message}", ex); }
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons, true, true, 0, 0, false);
            return saved.IsError ? Fail<bool>(saved.Message) : new OASISResult<bool>(true) { Message = $"Imported {saved.Result.Count()} holons into {Name}." };
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var all = await LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, true, false, version);
            return all.IsError ? all : Ok<IEnumerable<IHolon>>(all.Result.Where(h => h.CreatedByAvatarId == avatarId).ToList());
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            return avatar.IsError ? Fail<IEnumerable<IHolon>>(avatar.Message) : await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            return avatar.IsError ? Fail<IEnumerable<IHolon>>(avatar.Message) : await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        #endregion
    }
}
