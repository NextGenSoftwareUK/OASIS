using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
        private const string AvatarRecordType = "avatar";
        private const string AvatarDetailRecordType = "avatar-detail";

        private async Task<bool> EnsureActivatedAsync<T>(OASISResult<T> result)
        {
            if (_isActivated)
                return true;
            var activated = await ActivateProviderAsync();
            if (!activated.IsError && activated.Result)
                return true;
            OASISErrorHandling.HandleError(ref result, $"Failed to activate Aptos provider: {activated.Message}", activated.Exception);
            return false;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var ledger = await _aptosClient.Block.GetLedgerInfo();
                if (ledger == null || ledger.ChainId <= 0)
                    throw new InvalidOperationException("Aptos fullnode returned no valid ledger information.");
                _isActivated = true;
                IsProviderActivated = true;
                result.Result = true;
                result.Message = $"Aptos provider activated against chain {ledger.ChainId} at ledger version {ledger.LedgerVersion}.";
            }
            catch (Exception ex)
            {
                _isActivated = false;
                IsProviderActivated = false;
                OASISErrorHandling.HandleError(ref result, $"Error activating Aptos provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "Aptos provider deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = avatar.Id.ToString();
                await UpsertRecordAsync(AvatarRecordType, avatar.Id.ToString(), avatar);
                result.Result = avatar;
                result.Message = $"Avatar '{avatar.Username}' saved on Aptos.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving Aptos avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) =>
            await LoadAvatarByProviderKeyAsync(id.ToString(), version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                if (!await HasRecordAsync(AvatarRecordType, providerKey))
                {
                    result.Message = $"Aptos avatar '{providerKey}' was not found.";
                    return result;
                }
                var avatar = await GetRecordAsync<Avatar>(AvatarRecordType, providerKey);
                if (avatar != null)
                    avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = providerKey;
                result.Result = avatar;
                result.Message = avatar == null ? $"Aptos avatar '{providerKey}' was not found." : "Avatar loaded from Aptos.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading Aptos avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                var avatars = new List<IAvatar>();
                foreach (var key in await GetRecordKeysAsync(AvatarRecordType))
                {
                    var avatar = await GetRecordAsync<Avatar>(AvatarRecordType, key);
                    if (avatar != null && !avatar.IsDeleted)
                    {
                        avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = key;
                        avatars.Add(avatar);
                    }
                }
                result.Result = avatars;
                result.Message = $"Loaded {avatars.Count} avatars from Aptos.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading Aptos avatars: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var all = await LoadAllAvatarsAsync(version);
            return SelectAvatar(all, avatar => string.Equals(avatar.Username, username, StringComparison.OrdinalIgnoreCase), $"username '{username}'");
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var all = await LoadAllAvatarsAsync(version);
            return SelectAvatar(all, avatar => string.Equals(avatar.Email, email, StringComparison.OrdinalIgnoreCase), $"email '{email}'");
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;

        private static OASISResult<IAvatar> SelectAvatar(OASISResult<IEnumerable<IAvatar>> all, Func<IAvatar, bool> predicate, string criterion)
        {
            if (all.IsError) return new OASISResult<IAvatar> { IsError = true, Message = all.Message, Exception = all.Exception };
            var avatar = all.Result?.FirstOrDefault(predicate);
            return new OASISResult<IAvatar> { Result = avatar, Message = avatar == null ? $"Aptos avatar with {criterion} was not found." : "Avatar loaded from Aptos." };
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) =>
            await DeleteAvatarAsync(id.ToString(), softDelete);

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                if (!await HasRecordAsync(AvatarRecordType, providerKey))
                {
                    result.Result = false;
                    result.Message = $"Aptos avatar '{providerKey}' was not found.";
                    return result;
                }
                if (softDelete)
                {
                    var avatar = await GetRecordAsync<Avatar>(AvatarRecordType, providerKey);
                    avatar.DeletedDate = DateTime.UtcNow;
                    await UpsertRecordAsync(AvatarRecordType, providerKey, avatar);
                }
                else await DeleteRecordAsync(AvatarRecordType, providerKey);
                result.Result = true;
                result.Message = $"Aptos avatar '{providerKey}' deleted.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting Aptos avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                if (detail.Id == Guid.Empty) detail.Id = Guid.NewGuid();
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = detail.Id.ToString();
                await UpsertRecordAsync(AvatarDetailRecordType, detail.Id.ToString(), detail);
                result.Result = detail;
                result.Message = $"Avatar detail '{detail.Id}' saved on Aptos.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving Aptos avatar detail: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail detail) => SaveAvatarDetailAsync(detail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            await LoadAvatarDetailByProviderKeyAsync(id.ToString(), version);

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByProviderKeyAsync(string key, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                if (!await HasRecordAsync(AvatarDetailRecordType, key))
                {
                    result.Message = $"Aptos avatar detail '{key}' was not found.";
                    return result;
                }
                var detail = await GetRecordAsync<AvatarDetail>(AvatarDetailRecordType, key);
                if (detail != null) detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.AptosOASIS] = key;
                result.Result = detail;
                result.Message = detail == null ? $"Aptos avatar detail '{key}' was not found." : "Avatar detail loaded from Aptos.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading Aptos avatar detail: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!await EnsureActivatedAsync(result)) return result;
                var details = new List<IAvatarDetail>();
                foreach (var key in await GetRecordKeysAsync(AvatarDetailRecordType))
                {
                    var detail = await GetRecordAsync<AvatarDetail>(AvatarDetailRecordType, key);
                    if (detail != null && !detail.IsDeleted) details.Add(detail);
                }
                result.Result = details;
                result.Message = $"Loaded {details.Count} avatar details from Aptos.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading Aptos avatar details: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var all = await LoadAllAvatarDetailsAsync(version);
            return SelectAvatarDetail(all, detail => string.Equals(detail.Username, username, StringComparison.OrdinalIgnoreCase), $"username '{username}'");
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var all = await LoadAllAvatarDetailsAsync(version);
            return SelectAvatarDetail(all, detail => string.Equals(detail.Email, email, StringComparison.OrdinalIgnoreCase), $"email '{email}'");
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        private static OASISResult<IAvatarDetail> SelectAvatarDetail(OASISResult<IEnumerable<IAvatarDetail>> all, Func<IAvatarDetail, bool> predicate, string criterion)
        {
            if (all.IsError) return new OASISResult<IAvatarDetail> { IsError = true, Message = all.Message, Exception = all.Exception };
            var detail = all.Result?.FirstOrDefault(predicate);
            return new OASISResult<IAvatarDetail> { Result = detail, Message = detail == null ? $"Aptos avatar detail with {criterion} was not found." : "Avatar detail loaded from Aptos." };
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var all = LoadAllAvatars();
            if (all.IsError) return all;
            var matches = (all.Result ?? Enumerable.Empty<IAvatar>())
                .Where(avatar => IsWithinRadius(avatar.MetaData, geoLat, geoLong, radiusInMeters))
                .ToList();
            return new OASISResult<IEnumerable<IAvatar>> { Result = matches, Message = $"Found {matches.Count} Aptos avatars within {radiusInMeters} metres." };
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType type)
        {
            var all = LoadAllHolons(type);
            if (all.IsError) return all;
            var matches = (all.Result ?? Enumerable.Empty<IHolon>())
                .Where(holon => IsWithinRadius(holon.MetaData, geoLat, geoLong, radiusInMeters))
                .ToList();
            return new OASISResult<IEnumerable<IHolon>> { Result = matches, Message = $"Found {matches.Count} Aptos holons within {radiusInMeters} metres." };
        }

        private static bool IsWithinRadius(IDictionary<string, object> metadata, double latitude, double longitude, double radiusMetres)
        {
            if (metadata == null || radiusMetres < 0 ||
                !TryCoordinate(metadata, new[] { "latitude", "lat", "geoLat" }, out var itemLatitude) ||
                !TryCoordinate(metadata, new[] { "longitude", "long", "lng", "geoLong" }, out var itemLongitude))
                return false;

            const double earthRadiusMetres = 6_371_000;
            var lat1 = latitude * Math.PI / 180d;
            var lat2 = itemLatitude * Math.PI / 180d;
            var deltaLat = (itemLatitude - latitude) * Math.PI / 180d;
            var deltaLong = (itemLongitude - longitude) * Math.PI / 180d;
            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLong / 2) * Math.Sin(deltaLong / 2);
            return earthRadiusMetres * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)) <= radiusMetres;
        }

        private static bool TryCoordinate(IDictionary<string, object> metadata, IEnumerable<string> names, out double value)
        {
            foreach (var name in names)
            {
                var pair = metadata.FirstOrDefault(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase));
                var text = Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(pair.Key) && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            value = 0;
            return false;
        }
    }
}
