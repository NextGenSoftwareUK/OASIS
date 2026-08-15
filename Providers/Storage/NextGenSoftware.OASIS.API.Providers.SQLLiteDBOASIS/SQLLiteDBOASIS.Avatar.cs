using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        public bool IsVersionControlEnabled { get; set; } = false;

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                await _appDataContext.Database.EnsureDeletedAsync();
                await _appDataContext.Database.MigrateAsync();

                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In SQLLiteDBOASIS Provider in ActivateProviderAsync. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                _appDataContext.Database.EnsureDeleted();
                _appDataContext.Database.Migrate();

                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In SQLLiteDBOASIS Provider in ActivateProvider. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            // Do not dispose _appDataContext so the provider can be reactivated (e.g. during failover).
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            // Do not dispose _appDataContext so the provider can be reactivated (e.g. during failover).
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatar(id, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatar(providerKey, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatarAsync(id, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatarAsync(providerKey, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatarByEmail(avatarEmail, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatarByEmailAsync(avatarEmail, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatarByUsername(avatarUsername, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = _avatarRepository.DeleteAvatarByUsernameAsync(avatarUsername, softDelete);
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var result = _holonRepository.DeleteHolon(id);
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            var result = _holonRepository.DeleteHolon(providerKey);
            return result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = _holonRepository.DeleteHolonAsync(id);
            return result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = _holonRepository.DeleteHolonAsync(providerKey);
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = _avatarRepository.LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;

                var nearby = new List<IAvatar>();
                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = _holonRepository.LoadAllHolons(Type, true, true, 0, 0, true, 0);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;

                var nearby = new List<IHolon>();
                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var result = _avatarDetailRepository.LoadAllAvatarDetails(version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = _avatarDetailRepository.LoadAllAvatarDetailsAsync(version);
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var result = _avatarRepository.LoadAllAvatars(version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = _avatarRepository.LoadAllAvatarsAsync(version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadAllHolons(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = _avatarRepository.LoadAvatar(Id, version);
            return result;
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        //{
        //    var result = _avatarRepository.LoadAvatar(username, version);
        //    return result;
        //}

        public OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            return LoadAvatarAsync(username, password, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username and verify password
                var avatarResult = await _avatarRepository.LoadAvatarAsync(username, version);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Verify password (in a real implementation, this would hash and compare)
                    if (avatarResult.Result.Password == password)
                    {
                        result.Result = avatarResult.Result;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully from SQLite";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid password");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarAsync(Id, version);
            return result;
        }

        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        //{
        //    var result = _avatarRepository.LoadAvatarAsync(username, version);
        //    return result;
        //}

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarByEmail(avatarEmail, version);
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarByEmailAsync(avatarEmail, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarByUsername(avatarUsername, version);
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarByUsernameAsync(avatarUsername, version);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var result = _avatarDetailRepository.LoadAvatarDetail(id, version);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = _avatarDetailRepository.LoadAvatarDetailAsync(id, version);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            var result = _avatarDetailRepository.LoadAvatarDetailByEmail(avatarEmail, version);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = _avatarDetailRepository.LoadAvatarDetailByEmailAsync(avatarEmail, version);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            var result = _avatarDetailRepository.LoadAvatarDetailByUsername(avatarUsername, version);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = _avatarDetailRepository.LoadAvatarDetailByUsernameAsync(avatarUsername, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarByProviderKey(providerKey, version);
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = _avatarRepository.LoadAvatarByProviderKeyAsync(providerKey, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
            => _avatarRepository.LoadAvatarByVerificationToken(verificationToken, version);

        public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
            => _avatarRepository.LoadAvatarByVerificationTokenAsync(verificationToken, version);

        public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
            => _avatarRepository.LoadAvatarByResetToken(resetToken, version);

        public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
            => _avatarRepository.LoadAvatarByResetTokenAsync(resetToken, version);

        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
            => _avatarRepository.LoadAvatarByRefreshToken(refreshToken, version);

        public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
            => _avatarRepository.LoadAvatarByRefreshTokenAsync(refreshToken, version);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

    }
}
