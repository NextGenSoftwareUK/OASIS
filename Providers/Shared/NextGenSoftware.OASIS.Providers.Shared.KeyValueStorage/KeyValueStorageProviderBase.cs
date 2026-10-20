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
    public abstract class KeyValueStorageProviderBase : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <summary>When true, every save keeps the record it replaces as a numbered version that loads can request by version.</summary>
        public bool IsVersionControlEnabled { get; set; }

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
        // Versions live outside the record prefixes so listing avatars or holons never returns old versions.
        private string VersionPrefix(string recordKey) => $"{KeyPrefix}version/{recordKey[KeyPrefix.Length..]}/";
        private string VersionKey(string recordKey, int version) => $"{VersionPrefix(recordKey)}{version:D10}";

        private async Task StampVersionAsync(string recordKey, IHolonBase entity, IHolonBase previous,List<string> acknowledged=null)
        {
            await StoredRecordVersions.StampAsync(IdFromKey(recordKey), entity, previous,
                IsVersionControlEnabled ? async stored =>
                {
                    var historyKey = VersionKey(recordKey, stored.Version);
                    await Backend.PutAsync(historyKey, OasisJson.Serialize(stored));
                    acknowledged?.Add(historyKey);
                } : null);
        }

        private async Task<T> ReadVersionAsync<T>(string recordKey, T current, int version) where T : class, IHolonBase
        {
            return await StoredRecordVersions.ReadAsync(IdFromKey(recordKey), current, version,
                selected => ReadAsync<T>(VersionKey(recordKey, selected)));
        }

        private async Task DeleteVersionsAsync(string recordKey,List<string> acknowledged=null)
        {
            foreach (var key in await Backend.ListKeysAsync(VersionPrefix(recordKey)))
            {await Backend.DeleteAsync(key);acknowledged?.Add(key);}
        }

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
            if (json == null) return null;
            var record = OasisJson.DeserializeStored<T>(json);
            if (record == null) throw new System.IO.InvalidDataException("Stored record contains a null payload.");
            return record;
        }

        private async Task<Guid?> ReadIdAsync(string key)
        {
            var value = await Backend.GetAsync(key);
            if (value == null) return null;
            if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
                throw new System.IO.InvalidDataException("Stored avatar index contains an invalid record ID.");
            return id;
        }

        private async Task<List<T>> ReadAllAsync<T>(IEnumerable<string> keys, int version = 0, bool continueOnError = false, List<string> failures = null, List<Exception> readErrors = null) where T : class, IHolonBase
        {
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version), "Version must be zero (latest) or a positive historical version.");
            using var gate = new SemaphoreSlim(MaxParallelReads);
            var tasks = keys.Select(async key =>
            {
                await gate.WaitAsync();
                try { return await ReadVersionAsync(key, await ReadAsync<T>(key), version); }
                catch (Exception ex) when (continueOnError && failures != null
                    && ex is not System.IO.InvalidDataException && ex is not System.Text.Json.JsonException
                    && ex is not Newtonsoft.Json.JsonException
                    && ex is not OperationCanceledException && ex is not NotSupportedException
                    && ex is not OutOfMemoryException && ex is not AccessViolationException
                    && ex is not ArgumentException && ex is not FormatException)
                {
                    lock (failures)
                    {
                        failures.Add($"{IdFromKey(key)} ({ex.GetType().Name})");
                        readErrors?.Add(ex);
                    }
                    return null;
                }
                finally { gate.Release(); }
            });
            return (await Task.WhenAll(tasks)).Where(x => x != null).ToList();
        }

        private OASISResult<T> MarkPartialRead<T>(OASISResult<T> result, List<string> failures)
        {
            if (failures.Count == 0) return result;
            result.IsWarning = true;
            result.Message = $"Returned partial holon data; {failures.Count} record reads failed: {string.Join(", ", failures)}";
            result.MetaData ??= new Dictionary<string, string>();
            result.MetaData["partialHolonReads"] = "true";
            result.MetaData["failedHolonReadCount"] = failures.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            result.MetaData["failedHolonReads"] = OasisJson.Serialize(failures.ToArray());
            return result;
        }

        private OASISResult<T> Fail<T>(string message, Exception ex = null, string errorCode = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"{Name}: {message}", ex);
            result.ErrorCode = errorCode;
            return result;
        }

        private static OASISResult<T> Ok<T>(T value, string message = null) => new(value) { IsLoaded = true, Message = message };

        private static bool ParseKey(string providerKey, out Guid id) => Guid.TryParse(providerKey, out id) && id!=Guid.Empty;

        private OASISResult<T> InvalidVersion<T>(int version)=>Fail<T>(
            "Version must be zero (latest) or a positive historical version.",
            new ArgumentOutOfRangeException(nameof(version),version,"Negative versions are invalid."),"HYPERDRIVE_PAYLOAD_INVALID");

        private OASISResult<T> InvalidProviderKey<T>()=>Fail<T>("A nonempty GUID provider key is required.",
            new ArgumentException("A nonempty GUID provider key is required.","providerKey"),"HYPERDRIVE_PAYLOAD_INVALID");

        #endregion

        #region Avatars

        private async Task PutAcknowledgedAsync(string key,string value,List<string> acknowledged)
        {
            await Backend.PutAsync(key,value);acknowledged.Add(key);
        }

        private async Task DeleteAcknowledgedAsync(string key,List<string> acknowledged)
        {await Backend.DeleteAsync(key);acknowledged.Add(key);}

        private void PublishSavedMetadata(IHolonBase source,IHolonBase saved,Guid originalId)
        {
            StoredRecordPublication.EnsureIdentityUnchanged(source,originalId);
            source.Id=saved.Id;source.Version=saved.Version;source.VersionId=saved.VersionId;source.PreviousVersionId=saved.PreviousVersionId;
            source.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
            source.ProviderUniqueStorageKey[StorageType]=saved.ProviderUniqueStorageKey[StorageType];
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var source=avatar;var originalId=source?.Id ?? Guid.Empty;var acknowledged=new List<string>();
            try
            {
                if (avatar == null) return Fail<IAvatar>("Avatar is required.");
                avatar=OasisJson.Deserialize<IAvatar>(OasisJson.Serialize(source));
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[StorageType] = avatar.Id.ToString();

                var previous = await ReadAsync<IAvatar>(AvatarKey(avatar.Id));
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

                await StampVersionAsync(AvatarKey(avatar.Id), avatar, previous,acknowledged);
                await PutAcknowledgedAsync(AvatarKey(avatar.Id), OasisJson.Serialize(avatar),acknowledged);
                if (!string.IsNullOrWhiteSpace(avatar.Username)) await PutAcknowledgedAsync(UsernameKey(avatar.Username), avatar.Id.ToString(),acknowledged);
                if (!string.IsNullOrWhiteSpace(avatar.Email)) await PutAcknowledgedAsync(EmailKey(avatar.Email), avatar.Id.ToString(),acknowledged);
                // Validate replacements before any writes, and retire old indexes only after the new record/indexes exist.
                if (previous != null && !string.IsNullOrWhiteSpace(previous.Username) && !string.Equals(previous.Username, avatar.Username, StringComparison.OrdinalIgnoreCase))
                    await Backend.DeleteAsync(UsernameKey(previous.Username));
                if (previous != null && !string.IsNullOrWhiteSpace(previous.Email) && !string.Equals(previous.Email, avatar.Email, StringComparison.OrdinalIgnoreCase))
                    await Backend.DeleteAsync(EmailKey(previous.Email));

                PublishSavedMetadata(source,avatar,originalId);
                return new OASISResult<IAvatar>(source) { IsSaved = true, Message = $"Avatar {avatar.Id} saved to {Name}." };
            }
            catch (Exception ex) { return Fail<IAvatar>($"Error saving avatar: {ex.Message}",acknowledged.Count>0 ? new KeyValuePartialMutationException(StorageType,acknowledged,ex):ex); }
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            if(version<0)return InvalidVersion<IAvatar>(version);
            try
            {
                var avatar = await ReadVersionAsync(AvatarKey(id), await ReadAsync<IAvatar>(AvatarKey(id)), version);
                return avatar == null ? Fail<IAvatar>($"Avatar {id}{(version > 0 ? $" version {version}" : "")} not found.", errorCode: OASISResultErrorCodes.RecordNotFound) : Ok(avatar);
            }
            catch (Exception ex) { return Fail<IAvatar>($"Error loading avatar {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => ParseKey(providerKey, out var id) ? await LoadAvatarAsync(id, version) : InvalidProviderKey<IAvatar>();

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

        private async Task<OASISResult<IAvatar>> LoadAvatarByIndexAsync(string indexKey, string description, int version,bool byEmail)
        {
            if(version<0)return InvalidVersion<IAvatar>(version);
            try
            {
                var id = await ReadIdAsync(indexKey);
                if(!id.HasValue)return Fail<IAvatar>($"No avatar with {description}.",errorCode:OASISResultErrorCodes.RecordNotFound);
                // Validate the current index owner before selecting an independently retained record version.
                var current=await LoadAvatarAsync(id.Value);
                if(current.IsError)return current;
                var value=byEmail ? current.Result.Email:current.Result.Username;
                if(string.IsNullOrWhiteSpace(value) || !string.Equals(indexKey,byEmail ? EmailKey(value):UsernameKey(value),StringComparison.Ordinal))
                    throw new System.IO.InvalidDataException("Stored avatar index does not match its current record identity.");
                return version==0 ? current:await LoadAvatarAsync(id.Value,version);
            }
            catch (Exception ex) { return Fail<IAvatar>($"Error loading avatar with {description}: {ex.Message}", ex); }
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => string.IsNullOrWhiteSpace(avatarUsername) ? Task.FromResult(Fail<IAvatar>("Username is required.")) : LoadAvatarByIndexAsync(UsernameKey(avatarUsername), $"username '{avatarUsername}'", version,false);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => string.IsNullOrWhiteSpace(avatarEmail) ? Task.FromResult(Fail<IAvatar>("Email is required.")) : LoadAvatarByIndexAsync(EmailKey(avatarEmail), $"email '{avatarEmail}'", version,true);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            if(version<0)return InvalidVersion<IEnumerable<IAvatar>>(version);
            try
            {
                var keys = await Backend.ListKeysAsync($"{KeyPrefix}avatar/");
                return Ok<IEnumerable<IAvatar>>(await ReadAllAsync<IAvatar>(keys, version));
            }
            catch (Exception ex) { return Fail<IEnumerable<IAvatar>>($"Error loading avatars: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        /// <summary>Provider credential admission boundary; record-history reads remain independent.</summary>
        protected virtual Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
            => LoadAllAvatarsAsync(version);

        private Task<OASISResult<IAvatar>> LoadAvatarByTokenAsync(string token, Func<IAvatar, bool> matches, int version)
            => StoredAvatarWalletSelector.SelectMatchingAsync(token,version,LoadAvatarsForCredentialSelectionAsync,matches);

        public virtual Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string publicKey,int version=0)
            => StoredAvatarWalletSelector.SelectAsync(publicKey,false,version,LoadAvatarsForCredentialSelectionAsync);
        public virtual OASISResult<IAvatar> LoadAvatarByPublicKey(string publicKey,int version=0)
            => LoadAvatarByPublicKeyAsync(publicKey,version).GetAwaiter().GetResult();
        public virtual Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string privateKey,int version=0)
            => StoredAvatarWalletSelector.SelectAsync(privateKey,true,version,LoadAvatarsForCredentialSelectionAsync);
        public virtual OASISResult<IAvatar> LoadAvatarByPrivateKey(string privateKey,int version=0)
            => LoadAvatarByPrivateKeyAsync(privateKey,version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string token, int version = 0)
            => LoadAvatarByTokenAsync(token, avatar => string.Equals(avatar.VerificationToken, token, StringComparison.Ordinal), version);
        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string token, int version = 0)
            => LoadAvatarByVerificationTokenAsync(token, version).GetAwaiter().GetResult();
        public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string token, int version = 0)
            => LoadAvatarByTokenAsync(token, avatar => string.Equals(avatar.ResetToken, token, StringComparison.Ordinal), version);
        public override OASISResult<IAvatar> LoadAvatarByResetToken(string token, int version = 0)
            => LoadAvatarByResetTokenAsync(token, version).GetAwaiter().GetResult();
        public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string token, int version = 0)
            => LoadAvatarByTokenAsync(token, avatar => avatar.RefreshTokens?.Any(value =>
                value != null && string.Equals(value.Token, token, StringComparison.Ordinal)) == true, version);
        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string token, int version = 0)
            => LoadAvatarByRefreshTokenAsync(token, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var acknowledged=new List<string>();
            try
            {
                var avatar = await ReadVersionAsync(AvatarKey(id),await ReadAsync<IAvatar>(AvatarKey(id)),0);
                if (avatar == null) return Fail<bool>($"Avatar {id} not found.",errorCode:OASISResultErrorCodes.RecordNotFound);
                var detail=await ReadVersionAsync(AvatarDetailKey(id),await ReadAsync<IAvatarDetail>(AvatarDetailKey(id)),0);

                if (softDelete)
                {
                    avatar.DeletedDate = DateTime.UtcNow;
                    avatar.IsActive = false;
                    await PutAcknowledgedAsync(AvatarKey(id), OasisJson.Serialize(avatar),acknowledged);
                    if(detail!=null)
                    {
                        detail.IsActive=false;detail.DeletedDate=avatar.DeletedDate;
                        await PutAcknowledgedAsync(AvatarDetailKey(id),OasisJson.Serialize(detail),acknowledged);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(avatar.Username)) await DeleteAcknowledgedAsync(UsernameKey(avatar.Username),acknowledged);
                    if (!string.IsNullOrWhiteSpace(avatar.Email)) await DeleteAcknowledgedAsync(EmailKey(avatar.Email),acknowledged);
                    await DeleteVersionsAsync(AvatarDetailKey(id),acknowledged);
                    await DeleteAcknowledgedAsync(AvatarDetailKey(id),acknowledged);
                    await DeleteVersionsAsync(AvatarKey(id),acknowledged);
                    await DeleteAcknowledgedAsync(AvatarKey(id),acknowledged);
                }
                return new OASISResult<bool>(true) { IsDeleted = true, Message = $"Avatar {id} {(softDelete ? "soft" : "permanently")} deleted from {Name}." };
            }
            catch (Exception ex) { return Fail<bool>($"Error deleting avatar {id}: {ex.Message}",acknowledged.Count>0 ? new KeyValuePartialMutationException(StorageType,acknowledged,ex):ex); }
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => ParseKey(providerKey, out var id) ? await DeleteAvatarAsync(id, softDelete) : InvalidProviderKey<bool>();

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmail);
            return avatar.IsError ? Fail<bool>(avatar.Message,avatar.Exception,avatar.ErrorCode) : await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername);
            return avatar.IsError ? Fail<bool>(avatar.Message,avatar.Exception,avatar.ErrorCode) : await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).GetAwaiter().GetResult();

        #endregion

        #region Avatar details

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var source=avatarDetail;var originalId=source?.Id ?? Guid.Empty;var acknowledged=new List<string>();
            try
            {
                if (avatarDetail == null) return Fail<IAvatarDetail>("Avatar detail is required.");
                if (avatarDetail.Id == Guid.Empty) return Fail<IAvatarDetail>("Avatar detail must carry its avatar's id.");
                avatarDetail=OasisJson.Deserialize<IAvatarDetail>(OasisJson.Serialize(source));
                avatarDetail.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatarDetail.ProviderUniqueStorageKey[StorageType] = avatarDetail.Id.ToString();
                await StampVersionAsync(AvatarDetailKey(avatarDetail.Id), avatarDetail, await ReadAsync<IAvatarDetail>(AvatarDetailKey(avatarDetail.Id)),acknowledged);
                await PutAcknowledgedAsync(AvatarDetailKey(avatarDetail.Id), OasisJson.Serialize(avatarDetail),acknowledged);
                PublishSavedMetadata(source,avatarDetail,originalId);
                return new OASISResult<IAvatarDetail>(source) { IsSaved = true, Message = $"Avatar detail {avatarDetail.Id} saved to {Name}." };
            }
            catch (Exception ex) { return Fail<IAvatarDetail>($"Error saving avatar detail: {ex.Message}",acknowledged.Count>0 ? new KeyValuePartialMutationException(StorageType,acknowledged,ex):ex); }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            if(version<0)return InvalidVersion<IAvatarDetail>(version);
            try
            {
                var detail = await ReadVersionAsync(AvatarDetailKey(id), await ReadAsync<IAvatarDetail>(AvatarDetailKey(id)), version);
                return detail == null ? Fail<IAvatarDetail>($"Avatar detail {id} not found.", errorCode: OASISResultErrorCodes.RecordNotFound) : Ok(detail);
            }
            catch (Exception ex) { return Fail<IAvatarDetail>($"Error loading avatar detail {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            return avatar.IsError ? Fail<IAvatarDetail>(avatar.Message, avatar.Exception, avatar.ErrorCode) : await LoadAvatarDetailAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmail, version);
            return avatar.IsError ? Fail<IAvatarDetail>(avatar.Message, avatar.Exception, avatar.ErrorCode) : await LoadAvatarDetailAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            if(version<0)return InvalidVersion<IEnumerable<IAvatarDetail>>(version);
            try
            {
                var keys = await Backend.ListKeysAsync($"{KeyPrefix}avatardetail/");
                return Ok<IEnumerable<IAvatarDetail>>(await ReadAllAsync<IAvatarDetail>(keys, version));
            }
            catch (Exception ex) { return Fail<IEnumerable<IAvatarDetail>>($"Error loading avatar details: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        {
            var acknowledged=new List<string>();
            try
            {
                if (id == Guid.Empty) return Fail<bool>("Avatar detail ID is required.");
                var loaded = await LoadAvatarDetailAsync(id);
                if (loaded.IsError) return Fail<bool>(loaded.Message, loaded.Exception,loaded.ErrorCode);
                if (softDelete)
                {
                    loaded.Result.IsActive = false; loaded.Result.DeletedDate = DateTime.UtcNow;
                    var saved = await SaveAvatarDetailAsync(loaded.Result);
                    if (saved.IsError) return Fail<bool>(saved.Message, saved.Exception,saved.ErrorCode);
                }
                else
                {
                    await DeleteVersionsAsync(AvatarDetailKey(id),acknowledged);
                    await DeleteAcknowledgedAsync(AvatarDetailKey(id),acknowledged);
                }
                return new OASISResult<bool>(true) { IsDeleted = true, Message = "Avatar detail deleted." };
            }
            catch (Exception ex) { return Fail<bool>("Error deleting avatar detail.",acknowledged.Count>0 ? new KeyValuePartialMutationException(StorageType,acknowledged,ex):ex); }
        }

        public OASISResult<bool> DeleteAvatarDetail(Guid id, bool softDelete = true)
            => DeleteAvatarDetailAsync(id, softDelete).GetAwaiter().GetResult();
        public async Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username, bool softDelete = true)
        {
            var avatar = await LoadAvatarByUsernameAsync(username);
            return avatar.IsError ? Fail<bool>(avatar.Message, avatar.Exception,avatar.ErrorCode) : await DeleteAvatarDetailAsync(avatar.Result.Id, softDelete);
        }
        public OASISResult<bool> DeleteAvatarDetailByUsername(string username, bool softDelete = true)
            => DeleteAvatarDetailByUsernameAsync(username, softDelete).GetAwaiter().GetResult();
        public async Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email, bool softDelete = true)
        {
            var avatar = await LoadAvatarByEmailAsync(email);
            return avatar.IsError ? Fail<bool>(avatar.Message, avatar.Exception,avatar.ErrorCode) : await DeleteAvatarDetailAsync(avatar.Result.Id, softDelete);
        }
        public OASISResult<bool> DeleteAvatarDetailByEmail(string email, bool softDelete = true)
            => DeleteAvatarDetailByEmailAsync(email, softDelete).GetAwaiter().GetResult();

        #endregion

        #region Holons

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await SaveHolonInternalAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

        private async Task<OASISResult<IHolon>> SaveHolonInternalAsync(IHolon holon, bool saveChildren, bool recursive, int maxChildDepth, int depth, bool continueOnError,Guid? parent=null)
        {
            var source=holon;var originalId=source?.Id ?? Guid.Empty;var acknowledged=new List<string>();
            try
            {
                if (holon == null) return Fail<IHolon>("Holon is required.");
                ValidateSaveForest(new[] {holon}, saveChildren, recursive, maxChildDepth, depth);
                holon=OasisJson.Deserialize<IHolon>(OasisJson.Serialize(source));
                if(parent.HasValue) holon.ParentHolonId=parent.Value;
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                holon.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                holon.ProviderUniqueStorageKey[StorageType] = holon.Id.ToString();

                var previous = await ReadAsync<IHolon>(HolonKey(holon.Id));
                await StampVersionAsync(HolonKey(holon.Id), holon, previous,acknowledged);
                await PutAcknowledgedAsync(HolonKey(holon.Id), OasisJson.Serialize(holon),acknowledged);
                if (holon.ParentHolonId != Guid.Empty)
                    await PutAcknowledgedAsync(ParentKey(holon.ParentHolonId, holon.Id), holon.Id.ToString(),acknowledged);
                if (previous != null && previous.ParentHolonId != Guid.Empty && previous.ParentHolonId != holon.ParentHolonId)
                    await Backend.DeleteAsync(ParentKey(previous.ParentHolonId, holon.Id));

                PublishSavedMetadata(source,holon,originalId);source.ParentHolonId=holon.ParentHolonId;
                var result = new OASISResult<IHolon>(source) { IsSaved = true, Message = $"Holon {holon.Id} saved to {Name}." };
                var reachedDepth = maxChildDepth > 0 && depth + 1 >= maxChildDepth;
                if (saveChildren && source.Children != null && (depth == 0 || recursive) && !reachedDepth)
                {
                    foreach (var child in source.Children)
                    {
                        var saved = await SaveHolonInternalAsync(child, saveChildren, recursive, maxChildDepth, depth + 1, continueOnError,holon.Id);
                        if (saved.IsError)
                        {
                            var cause=NativeMutationReconciliation.IsRequired(saved.Exception) ? saved.Exception
                                :new KeyValuePartialMutationException(StorageType,acknowledged,saved.Exception ?? new InvalidOperationException(saved.Message));
                            OASISErrorHandling.HandleError(ref result, $"{Name}: error saving child holon {child.Id}: {saved.Message}", cause);
                            if (!continueOnError || NativeMutationReconciliation.IsRequired(saved.Exception)) return result;
                        }
                    }
                }
                return result;
            }
            catch (Exception ex) { return Fail<IHolon>($"Error saving holon: {ex.Message}",acknowledged.Count>0 ? new KeyValuePartialMutationException(StorageType,acknowledged,ex):ex); }
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        private static void ValidateSaveForest(IEnumerable<IHolon> roots,bool saveChildren,bool recursive,int maxChildDepth,int depth)
        {
            if(maxChildDepth<0 || depth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
            var selected=roots.ToList();if(selected.Count==0) return;
            // This provider's existing depth limit counts the root as level one.
            var children=saveChildren && (depth==0 || recursive) && (maxChildDepth==0 || depth+1<maxChildDepth);
            try {HolonGraphWritePlan.Create(selected,children,recursive,maxChildDepth==0 ? 0:maxChildDepth-1,depth);}
            catch(ArgumentException cause)
            {
                // Preserve the existing terminal invalid-graph boundary while sharing one graph validator.
                throw new System.IO.InvalidDataException("Cyclic holon child graph or conflicting/repeated graph selection cannot be saved.",cause);
            }
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (holons == null) return Fail<IEnumerable<IHolon>>("Holons are required.");
            List<IHolon> roots;
            try {roots=holons.ToList();ValidateSaveForest(roots,saveChildren,recursive,maxChildDepth,curentChildDepth);}
            catch(Exception cause) {return Fail<IEnumerable<IHolon>>($"Invalid holon batch: {cause.Message}",cause);}
            var saved = new List<IHolon>();
            var acknowledged=new HashSet<string>(StringComparer.Ordinal);var failures=new List<Exception>();
            Exception BatchFailure()
            {
                var cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
                return acknowledged.Count==0 ? cause:new KeyValuePartialMutationException(StorageType,acknowledged,cause);
            }
            foreach (var holon in roots)
            {
                var r = await SaveHolonInternalAsync(holon, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
                if(r.IsSaved && r.Result!=null) acknowledged.Add(HolonKey(r.Result.Id));
                CollectAcknowledgedKeyEvidence(r.Exception,acknowledged);
                if (r.IsError)
                {
                    failures.Add(r.Exception ?? new InvalidOperationException(r.Message));
                    OASISErrorHandling.HandleError(ref result, $"{Name}: error saving holon {holon?.Id}: {r.Message}", BatchFailure());
                    if (!continueOnError || NativeMutationReconciliation.IsRequired(r.Exception)) return result;
                }
                else saved.Add(r.Result);
            }
            if(failures.Count>0) result.Exception=BatchFailure();
            result.Result = saved;
            result.IsSaved = !result.IsError;
            return result;
        }

        private static void CollectAcknowledgedKeyEvidence(Exception cause,HashSet<string> acknowledged)
        {
            if(cause==null) return;
            if(cause is KeyValuePartialMutationException partial) acknowledged.UnionWith(partial.AcknowledgedKeys);
            if(cause is AggregateException aggregate) foreach(var inner in aggregate.InnerExceptions) CollectAcknowledgedKeyEvidence(inner,acknowledged);
            else CollectAcknowledgedKeyEvidence(cause.InnerException,acknowledged);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if(version<0)return InvalidVersion<IHolon>(version);
            try
            {
                var holon = await ReadVersionAsync(HolonKey(id), await ReadAsync<IHolon>(HolonKey(id)), version);
                if (holon == null) return Fail<IHolon>($"Holon {id} not found.", errorCode: OASISResultErrorCodes.RecordNotFound);
                if (loadChildren)
                {
                    var failures = new List<string>();
                    var children = await LoadChildrenAsync(holon.Id, HolonType.All, recursive, maxChildDepth, 0, version,
                        continueOnError: continueOnError, failures: failures);
                    holon.Children = children.Result?.ToList() ?? new List<IHolon>();
                    return MarkPartialRead(Ok(holon), failures);
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
                : InvalidProviderKey<IHolon>();

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadChildrenAsync(Guid parentId, HolonType type, bool recursive, int maxChildDepth, int depth, int version, HashSet<Guid> ancestors = null, bool continueOnError = false, List<string> failures = null)
        {
            failures ??= new List<string>();
            var path = ancestors == null ? new HashSet<Guid>() : new HashSet<Guid>(ancestors);
            if (!path.Add(parentId)) throw new System.IO.InvalidDataException("Cyclic holon parent relationship encountered during recursive child loading.");
            List<IHolon> children;
            if (version > 0)
            {
                // Current parent indexes cannot represent a retained historical record's former parent.
                var historicalKeys = await Backend.ListKeysAsync($"{KeyPrefix}holon/");
                children = (await ReadAllAsync<IHolon>(historicalKeys, version, continueOnError, failures)).Where(child => child.ParentHolonId == parentId).ToList();
            }
            else
            {
                var keys = await Backend.ListKeysAsync(ParentPrefix(parentId));
                children = (await ReadAllAsync<IHolon>(keys.Select(k => HolonKey(IdFromKey(k))), version, continueOnError, failures)).Where(child => child.ParentHolonId == parentId).ToList();
            }
            var reachedDepth = maxChildDepth > 0 && depth + 1 >= maxChildDepth;
            if (recursive && !reachedDepth)
            {
                foreach (var child in children)
                {
                    var grandChildren = await LoadChildrenAsync(child.Id, HolonType.All, true, maxChildDepth, depth + 1, version, path, continueOnError, failures);
                    child.Children = grandChildren.Result?.ToList() ?? new List<IHolon>();
                }
            }
            return MarkPartialRead(Ok<IEnumerable<IHolon>>(children.Where(h => type == HolonType.All || h.HolonType == type).ToList()), failures);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if(version<0)return InvalidVersion<IEnumerable<IHolon>>(version);
            try { return await LoadChildrenAsync(id, type, loadChildren && recursive, maxChildDepth, curentChildDepth, version, continueOnError: continueOnError); }
            catch (Exception ex) { return Fail<IEnumerable<IHolon>>($"Error loading holons for parent {id}: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ParseKey(providerKey, out var id)
                ? await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version)
                : InvalidProviderKey<IEnumerable<IHolon>>();

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadHolonCollectionAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, version, continueOnError: continueOnError);

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonCollectionAsync(HolonType type, bool loadChildren, bool recursive, int maxChildDepth, int curentChildDepth, int version, Func<IHolon, bool> predicate = null, bool continueOnError = false)
        {
            if(version<0)return InvalidVersion<IEnumerable<IHolon>>(version);
            try
            {
                var keys = await Backend.ListKeysAsync($"{KeyPrefix}holon/");
                var failures = new List<string>();
                var readErrors = new List<Exception>();
                var holons = await ReadAllAsync<IHolon>(keys, version, continueOnError, failures, readErrors);
                if (holons.Count == 0 && readErrors.Count > 0)
                    throw new AggregateException("No partial holon collection is available: record reads failed.", readErrors);
                var selected = holons.Where(h => (type == HolonType.All || h.HolonType == type) && (predicate == null || predicate(h))).ToList();
                if (loadChildren)
                    foreach (var holon in selected)
                    {
                        var children = await LoadChildrenAsync(holon.Id, HolonType.All, recursive, maxChildDepth, curentChildDepth, version,
                            continueOnError: continueOnError, failures: failures);
                        holon.Children = children.Result.ToList();
                    }
                return MarkPartialRead(Ok<IEnumerable<IHolon>>(selected), failures);
            }
            catch (Exception ex) { return Fail<IEnumerable<IHolon>>($"Error loading holons: {ex.Message}", ex); }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        private static bool MetaMatches(IHolon holon, string key, string value)
            => holon.MetaData != null && holon.MetaData.TryGetValue(key, out var v) && string.Equals(StoredRecordSearch.MetadataValueText(v), value, StringComparison.Ordinal);

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (string.IsNullOrWhiteSpace(metaKey)) return Fail<IEnumerable<IHolon>>("A nonblank metadata key is required.");
            return await LoadHolonCollectionAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, version, h => MetaMatches(h, metaKey, metaValue), continueOnError);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0) return Fail<IEnumerable<IHolon>>("At least one metadata key/value pair is required.");
            if (metaKeyValuePairs.Keys.Any(string.IsNullOrWhiteSpace)) return Fail<IEnumerable<IHolon>>("Metadata keys must be nonblank.");
            if (!Enum.IsDefined(typeof(MetaKeyValuePairMatchMode), metaKeyValuePairMatchMode))
                return Fail<IEnumerable<IHolon>>("A defined metadata match mode is required.");
            bool Match(IHolon h) => metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any
                ? metaKeyValuePairs.Any(kv => MetaMatches(h, kv.Key, kv.Value))
                : metaKeyValuePairs.All(kv => MetaMatches(h, kv.Key, kv.Value));
            return await LoadHolonCollectionAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, version, Match, continueOnError);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var acknowledged=new List<string>();
            try
            {
                var holon = await ReadVersionAsync(HolonKey(id),await ReadAsync<IHolon>(HolonKey(id)),0);
                if (holon == null) return Fail<IHolon>($"Holon {id} not found.",errorCode:OASISResultErrorCodes.RecordNotFound);
                if (holon.ParentHolonId != Guid.Empty) await DeleteAcknowledgedAsync(ParentKey(holon.ParentHolonId, id),acknowledged);
                foreach (var childLink in await Backend.ListKeysAsync(ParentPrefix(id)))
                    await DeleteAcknowledgedAsync(childLink,acknowledged);
                await DeleteVersionsAsync(HolonKey(id),acknowledged);
                await DeleteAcknowledgedAsync(HolonKey(id),acknowledged);
                return new OASISResult<IHolon>(holon) { IsDeleted = true, Message = $"Holon {id} deleted from {Name}." };
            }
            catch (Exception ex) { return Fail<IHolon>($"Error deleting holon {id}: {ex.Message}",acknowledged.Count>0 ? new KeyValuePartialMutationException(StorageType,acknowledged,ex):ex); }
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => ParseKey(providerKey, out var id) ? await DeleteHolonAsync(id) : InvalidProviderKey<IHolon>();

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        #endregion

        #region Search, import and export

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try {StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);}
            catch(Exception ex) when(ex is ArgumentException || ex is NotSupportedException)
            {return Fail<ISearchResults>($"Invalid search request: {ex.Message}",ex,"HYPERDRIVE_PAYLOAD_INVALID");}
            try
            {
                var results = new SearchResults();
                OASISResult<IEnumerable<IHolon>> partialCollection=null;
                var groups=searchParams.SearchGroups??new List<ISearchGroupBase>();
                var searchAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
                var searchHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
                var avatars=new List<IAvatar>();var holons=new List<IHolon>();
                if(searchAvatars)
                {
                    var all=await LoadAllAvatarsAsync(version);
                    if(all.IsError)return Fail<ISearchResults>(all.Message,all.Exception,all.ErrorCode);
                    avatars=all.Result.ToList();
                }
                if(searchHolons || searchParams.ParentId!=Guid.Empty)
                {
                    var all=await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,0,continueOnError,false,version);
                    if(all.IsError)return Fail<ISearchResults>(all.Message,all.Exception,all.ErrorCode);
                    if(all.IsWarning)partialCollection=all;
                    holons=all.Result.ToList();
                }
                var ancestry=holons;
                if(version>0 && searchParams.ParentId!=Guid.Empty)
                {
                    var current=await LoadAllHolonsAsync(HolonType.All,false,false,0,0,false,false,0);
                    if(current.IsError)return Fail<ISearchResults>(current.Message,current.Exception,current.ErrorCode);
                    ancestry=current.Result.ToList();
                }
                results.SearchResultAvatars.AddRange(avatars.Where(row=>row.DeletedDate==DateTime.MinValue && StoredRecordSearch.Matches(row,true,searchParams,ancestry)));
                if(searchHolons)results.SearchResultHolons.AddRange(holons.Where(row=>row.DeletedDate==DateTime.MinValue && StoredRecordSearch.Matches(row,false,searchParams,ancestry)));
                results.NumberOfResults = results.SearchResultAvatars.Count + results.SearchResultHolons.Count;
                var result=Ok<ISearchResults>(results);
                if(partialCollection!=null){result.IsWarning=true;result.Message=partialCollection.Message;result.MetaData=partialCollection.MetaData;}
                return result;
            }
            catch (Exception ex) { return Fail<ISearchResults>($"Error searching: {ex.Message}", ex); }
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons, true, true, 0, 0, false);
            return saved.IsError ? Fail<bool>(saved.Message, saved.Exception) : new OASISResult<bool>(true) { Message = $"Imported {saved.Result.Count()} holons into {Name}." };
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).GetAwaiter().GetResult();

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var all = await LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, true, false, version);
            if(all.IsError)return all;
            var filtered=all.Result.Where(h=>h.CreatedByAvatarId==avatarId).ToList();
            all.Result=filtered;all.LoadedCount=filtered.Count;
            return all;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            return avatar.IsError ? Fail<IEnumerable<IHolon>>(avatar.Message,avatar.Exception,avatar.ErrorCode) : await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            return avatar.IsError ? Fail<IEnumerable<IHolon>>(avatar.Message,avatar.Exception,avatar.ErrorCode) : await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        #endregion
    }
}
