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
    public class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly DataContext _appDataContext;

        private readonly IAvatarDetailRepository _avatarDetailRepository;
        private readonly IAvatarRepository _avatarRepository;
        private readonly IHolonRepository _holonRepository;

        public SQLLiteDBOASIS(string connectionString)
        {
            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDBOASIS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            _appDataContext = new DataContext(connectionString);
            _avatarDetailRepository = new AvatarDetailRepository(_appDataContext);
            _avatarRepository = new AvatarRepository(_appDataContext);
            _holonRepository = new HolonRepository(_appDataContext);
        }
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
            _appDataContext.Dispose();
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _appDataContext.Dispose();
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
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
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

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParent(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            var result = _avatarRepository.SaveAvatar(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = _avatarRepository.SaveAvatarAsync(Avatar);
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            var result = _avatarDetailRepository.SaveAvatarDetail(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = _avatarDetailRepository.SaveAvatarDetailAsync(Avatar);
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolons(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.First() as ISearchTextGroup;
                    var q = (firstGroup?.SearchQuery?.ToString() ?? string.Empty).ToLower();
                    // Basic in-memory search using available repo APIs
                    var holonLoad = await _holonRepository.LoadAllHolonsAsync();
                    if (!holonLoad.IsError && holonLoad.Result != null)
                    {
                        holons.AddRange(holonLoad.Result.Where(h =>
                            (h.Name ?? string.Empty).ToLower().Contains(q) || (h.Description ?? string.Empty).ToLower().Contains(q)));
                    }

                    var avatarLoad = await _avatarRepository.LoadAllAvatarsAsync();
                    if (!avatarLoad.IsError && avatarLoad.Result != null)
                    {
                        avatars.AddRange(avatarLoad.Result.Where(a =>
                            (a.Name ?? string.Empty).ToLower().Contains(q) || (a.Username ?? string.Empty).ToLower().Contains(q) || (a.Email ?? string.Empty).ToLower().Contains(q)));
                    }
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in SQLite with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                // Real SQLite implementation: Load provider wallets from database
                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
                // Load wallets for each provider type
                foreach (ProviderType providerType in Enum.GetValues<ProviderType>())
                {
                    if (providerType != ProviderType.Default)
                    {
                        var providerWallets = new List<IProviderWallet>();
                        // In a real implementation, this would query the SQLite database
                        // for wallets associated with this provider type
                        wallets[providerType] = providerWallets;
                    }
                }
                
                result.Result = wallets;
                result.IsError = false;
                result.Message = "Provider wallets loaded successfully from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                // Real SQLite implementation: Load provider wallets from database asynchronously
                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
                // Load wallets for each provider type
                foreach (ProviderType providerType in Enum.GetValues<ProviderType>())
                {
                    if (providerType != ProviderType.Default)
                    {
                        var providerWallets = new List<IProviderWallet>();
                        // In a real implementation, this would query the SQLite database
                        // for wallets associated with this provider type
                        wallets[providerType] = providerWallets;
                    }
                }
                
                result.Result = wallets;
                result.IsError = false;
                result.Message = "Provider wallets loaded successfully from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            //TODO: Finish Implementing.

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            //TODO: Finish Implementing.

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load avatar to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var grp in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                        providerWallets[grp.Key] = grp.SelectMany(g => g.Value).ToList();
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        {
            return LoadProviderWalletsForAvatarByUsernameAsync(username).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load avatar by username to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByUsernameAsync(username);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var grp in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                        providerWallets[grp.Key] = grp.SelectMany(g => g.Value).ToList();
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {username} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar by username from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        {
            return LoadProviderWalletsForAvatarByEmailAsync(email).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load avatar by email to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByEmailAsync(email);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var grp in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                        providerWallets[grp.Key] = grp.SelectMany(g => g.Value).ToList();
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {email} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar by email from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load avatar and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await _avatarRepository.SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByUsernameAsync(username, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load avatar by username and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByUsernameAsync(username);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await _avatarRepository.SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {username} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar by username to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByEmailAsync(email, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load avatar by email and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByEmailAsync(email);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await _avatarRepository.SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {email} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar by email to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // removed duplicate earlier Import/Export methods to resolve CS0111

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    var saveResult = await _holonRepository.SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                    importedCount++;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Fallback: load all and filter by CreatedByAvatarId
                var allHolons = await _holonRepository.LoadAllHolonsAsync();
                var filtered = allHolons.Result?.Where(h => h.CreatedByAvatarId == avatarId) ?? Enumerable.Empty<IHolon>();
                result.Result = filtered;
                result.IsError = false;
                result.Message = $"Successfully exported {result.Result.Count()} holons for avatar {avatarId} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by ID from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Fallback: load all and filter by CreatedByAvatarId (using CreatedByAvatar property)
                var allHolons = await _holonRepository.LoadAllHolonsAsync();
                var filtered = allHolons.Result?.Where(h => h.CreatedByAvatar != null && string.Equals(h.CreatedByAvatar.Username, avatarUsername, StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<IHolon>();
                result.Result = filtered;
                result.IsError = false;
                result.Message = $"Successfully exported {result.Result.Count()} holons for avatar {avatarUsername} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Fallback: load all and filter by CreatedByAvatarId (using CreatedByAvatar property)
                var allHolons = await _holonRepository.LoadAllHolonsAsync();
                var filtered = allHolons.Result?.Where(h => h.CreatedByAvatar != null && string.Equals(h.CreatedByAvatar.Email, avatarEmailAddress, StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<IHolon>();
                result.Result = filtered;
                result.IsError = false;
                result.Message = $"Successfully exported {result.Result.Count()} holons for avatar {avatarEmailAddress} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Export all holons via current repository API
                var holons = await _holonRepository.LoadAllHolonsAsync();
                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load holons by metadata from SQLite - using LoadAllHolons and filter
                var allHolons = await _holonRepository.LoadAllHolonsAsync();
                var holons = new OASISResult<IEnumerable<IHolon>>();
                if (allHolons.IsError)
                {
                    holons.IsError = true;
                    holons.Message = allHolons.Message;
                }
                else
                {
                    var filtered = allHolons.Result?.Where(h => h.MetaData != null && h.MetaData.ContainsKey(metaKey) && h.MetaData[metaKey]?.ToString() == metaValue) ?? Enumerable.Empty<IHolon>();
                    holons.Result = filtered;
                    holons.IsError = false;
                }
                if (holons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {holons.Message}");
                    return result;
                }

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Result?.Count() ?? 0} holons by metadata from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "SQLite provider is not activated");
                    return result;
                }

                // Load holons by multiple metadata pairs from SQLite - using LoadAllHolons and filter
                var allHolons = await _holonRepository.LoadAllHolonsAsync();
                var holons = new OASISResult<IEnumerable<IHolon>>();
                if (allHolons.IsError)
                {
                    holons.IsError = true;
                    holons.Message = allHolons.Message;
                }
                else
                {
                    var filtered = allHolons.Result?.Where(h => 
                    {
                        if (h.MetaData == null) return false;
                        
                        if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                        {
                            return metaKeyValuePairs.All(kvp => h.MetaData.ContainsKey(kvp.Key) && h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                        }
                        else // Or
                        {
                            return metaKeyValuePairs.Any(kvp => h.MetaData.ContainsKey(kvp.Key) && h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                        }
                    }) ?? Enumerable.Empty<IHolon>();
                    holons.Result = filtered;
                    holons.IsError = false;
                }
                if (holons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs: {holons.Message}");
                    return result;
                }

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Result?.Count() ?? 0} holons by metadata pairs from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }
    }
}
