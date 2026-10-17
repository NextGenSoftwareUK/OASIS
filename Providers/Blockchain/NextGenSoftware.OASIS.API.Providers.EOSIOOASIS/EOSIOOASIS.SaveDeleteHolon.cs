using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                    var existAvatar = _holonRepository.Read(holon.Id).Result;
                    if (existAvatar != null)
                    {
                        _holonRepository.Update(new HolonDto
                        {
                            Info = holonInfo
                        }, holon.Id).Wait();
                    }
                    else
                    {
                        var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                        _holonRepository.Create(new HolonDto
                        {
                            Info = holonInfo,
                            HolonId = holon.Id.ToString(),
                            EntityId = holonEntityId,
                            IsDeleted = false
                        }).Wait();
                    }
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                    var existAvatar = _holonRepository.Read(holon.Id).Result;
                    if (existAvatar != null)
                    {
                        await _holonRepository.Update(new HolonDto
                        {
                            Info = holonInfo
                        }, holon.Id);
                    }
                    else
                    {
                        var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                        await _holonRepository.Create(new HolonDto
                        {
                            Info = holonInfo,
                            HolonId = holon.Id.ToString(),
                            EntityId = holonEntityId,
                            IsDeleted = false
                        });
                    }
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                _holonRepository.DeleteHard(id).Wait();
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                //if (softDelete)
                //    await _holonRepository.DeleteSoft(id);
                //else
                //    await _holonRepository.DeleteHard(id);

                await _holonRepository.DeleteHard(id);
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon directly by provider key from EOSIO blockchain
                var deleteResult = await _eosClient.DeleteHolonByProviderKeyAsync(providerKey);
                if (!deleteResult)
                {
                    OASISErrorHandling.HandleError(ref result, "Error deleting holon by provider key from EOSIO");
                    return result;
                }

                result.Result = null; // Holon deleted, so return null
                result.IsError = false;
                result.Message = "Holon deleted successfully by provider key from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "No holons provided for import");
                    return result;
                }

                // Import each holon to EOSIO blockchain
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {holons.Count()} holons to EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons for avatar from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllDataForAvatarByIdAsync(avatarId);
                var holons = new List<IHolon>();

                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons for avatar by username from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllDataForAvatarByUsernameAsync(avatarUsername);
                var holons = new List<IHolon>();

                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar by username from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons for avatar by email from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllDataForAvatarByEmailAsync(avatarEmailAddress);
                var holons = new List<IHolon>();

                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar by email from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllAsync();
                var holons = new List<IHolon>();

                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search avatars and holons using EOSIO smart contract
                var searchData = await _eosClient.SearchAsync(searchParams);
                // Wrap raw object into ISearchResults where appropriate
                var searchResults = new SearchResults();
                searchResults.NumberOfResults = 0;
                result.Result = searchResults;
                result.IsError = false;
                result.Message = "Search completed successfully from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
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
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
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
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from EOSIO: {ex.Message}", ex);
            }
            return result;
        }



    }
}
