using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    public partial class AzureCosmosDBOASIS
    {
        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with id {id}";

            try
            {
                holonRepository.DeleteAsync(id).Wait();
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with providerKey {providerKey}";

            try
            {
                holonRepository.DeleteAsync(providerKey).Wait();
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with id {id}";

            try
            {
                await holonRepository.DeleteAsync(id);
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with providerKey {providerKey}";

            try
            {
                await holonRepository.DeleteAsync(providerKey);
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Azure Cosmos DB: {ex.Message}", ex);
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
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = "Error occured in LoadAllAvatarDetails method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarDetailsList = avatarDetailRepository.GetList();

                if (avatarDetailsList == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    if (version > 0)
                        avatarDetailsList = avatarDetailsList.Where(a => a.Version == version).ToList();

                    result.Result = avatarDetailsList;
                    result.IsLoaded = true;
                    result.Message = "Avatar details fetched";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = "Error occured in LoadAllAvatarDetailsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarDetailsList = avatarDetailRepository.GetList();

                if (avatarDetailsList == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    if (version > 0)
                        avatarDetailsList = avatarDetailsList.Where(a => a.Version == version).ToList();

                    result.Result = avatarDetailsList;
                    result.IsLoaded = true;
                    result.Message = "Avatar details fetched asynchronously";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            string errorMessage = "Error occured in LoadAllAvatarsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                var avatarList = avatarRepository.GetList();

                if (avatarList == null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    if (version > 0)
                        avatarList = avatarList.Where(a => a.Version == version).ToList();

                    result.Result = avatarList;
                    result.IsLoaded = true;
                    result.Message = "Avatars fetched";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return LoadAllAvatars(version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, int version = 0)
        {
            //return LoadAllHolonsAsync(type, version).Result;
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error occured in LoadAllHolonsAsync method in AzureCosmosDBOASIS Provider. Reason: ";

            try
            {
                List<IHolon> allHolonsToReturn = new List<IHolon>();
                List<IHolon> holonList = holonRepository.GetList();
                IEnumerable<IHolon> holonsFiltered = null;

                if (version > 0)
                    holonsFiltered = holonList.Where(h => h.HolonType == type && h.Version == version).ToList();
                else
                    holonsFiltered = holonList.Where(h => h.HolonType == type).ToList();

                // Child loading is deferred: recursive holon loading would call LoadAllHolons on each child,
                // which risks infinite recursion on deep graphs. Child holons are loaded on demand via LoadChildHolons.
                // The loadChildren/recursive/maxChildDepth params are accepted but not yet applied here.

                if (holonList.Count <= 0)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}No records found.");
                else
                {
                    result.Result = holonsFiltered;
                    result.IsLoaded = true;
                    result.Message = "Holons fetched";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatar avatar = avatarRepository.GetByIdAsync(id.ToString()).Result;

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatar method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatar method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatar method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                var avatar = await avatarRepository.GetByIdAsync(Id.ToString());
                
                if (avatar == null)
                {
                    result.Message = "No avatars found in LoadAvatarAsync method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarAsync method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatar method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                //TODO: Need to test to make sure this works!
                IAvatar avatar = avatarRepository.GetByField("Email", avatarEmail, version);

                //var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.Email == avatarEmail).FirstOrDefault();

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatarByEmail method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByEmail method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatar method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                //TODO: Need to test to make sure this works!
                IAvatar avatar = avatarRepository.GetByField("Email", avatarEmail, version);                

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatarByEmailAsync method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByEmailAsync method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarByEmailAsync method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                //TODO: Need to test to make sure this works!
                IAvatar avatar = avatarRepository.GetByField("UserName", avatarUsername, version);

                if (avatar == null)
                {
                    result.Message = "No Avatar found in LoadAvatarByUsername method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByUsername method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarByUsername method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsername(avatarUsername,version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatarDetail avatarDetail =await avatarDetailRepository.GetByIdAsync(id.ToString());

                if (avatarDetail == null)
                {
                    result.Message = "No AvatarDetails found in LoadAvatarDetailAsync method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatarDetail;
                    result.Message = "AvatarDetails fetched in LoadAvatarDetailAsync method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarDetailAsync method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatarDetail avatarDetail = avatarDetailRepository.GetByField("Email",avatarEmail, version);

                if (avatarDetail == null)
                {
                    result.Message = "No AvatarDetails found in LoadAvatarDetailByEmail method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatarDetail;
                    result.Message = "AvatarDetails fetched in LoadAvatarDetailByEmail method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarDetailByEmail method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmail(avatarEmail, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                //TODO HB: Re-write so follows other methods that use OASISErrorHandling.HandlerError etc.
                IAvatarDetail avatarDetail = avatarDetailRepository.GetByField("UserName", avatarUsername, version);

                if (avatarDetail == null)
                {
                    result.Message = "No AvatarDetails found in LoadAvatarDetailByUsername method in AzureCOSMOSDBOASIS.";
                }
                else
                {
                    result.Result = avatarDetail;
                    result.Message = "AvatarDetails fetched in LoadAvatarDetailByUsername method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarDetailByUsername method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }
            return result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsername(avatarUsername, version);
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                IAvatar avatar = avatarRepository.GetByField("Id", providerKey, version);

                //var avatarList = avatarRepository.GetList();
                //var avatar = avatarList.Where(a => a.Id == new Guid(providerKey)).FirstOrDefault(); //The ID and ProviderUniqueStorageKey are the same for Azure because Azure uses GUID for ID's like OASIS does.
                //var avatar = avatarList.Where(a => a.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] == providerKey).FirstOrDefault();
                
                if (avatar == null)
                    result.Message = "No record found in LoadAvatarByProviderKey method in AzureCosmosDbOASIS Provider.";
                else
                {
                    result.Result = avatar;
                    result.Message = "Avatar fetched in LoadAvatarByProviderKey method in AzureCosmosDbOASIS Provider.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadAvatarByProviderKey method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }

            return result;
        }

        public async override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKey(providerKey, version);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolon(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                var holon = holonRepository.GetByIdAsync(providerKey).Result;

                if (holon == null)
                    result.Message = "No holons found in LoadHolon method in AzureCOSMOSDBOASIS.";
                else
                {
                    result.Result = holon;
                    result.Message = "Holon fetched in LoadHolon method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadHolon method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }

            return result;
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolon(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider,version);
        }

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParent(id.ToString(), type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var holonList = holonRepository.GetList();
                var holonFiltered = holonList.Where(h => h.HolonType == type && h.ParentHolonId.ToString() == providerKey).ToList();

                if (holonList.Count <= 0)
                    result.Message = "No holons found in LoadHolonsForParent method in AzureCOSMOSDBOASIS.";
                else
                {
                    result.Result = holonFiltered;
                    result.Message = "Holons fetched in LoadHolonsForParent method in AzureCOSMOSDBOASIS.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in LoadHolonsForParent method in AzureCosmosDBOASIS Provider. Reason: {ex.Message}.");
            }

            return result;
        }

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParent(id.ToString(), type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

    }
}
