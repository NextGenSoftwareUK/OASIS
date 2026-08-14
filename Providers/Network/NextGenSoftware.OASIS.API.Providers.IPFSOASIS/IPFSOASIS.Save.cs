using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Ipfs.Http;
using Ipfs;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.Utilities;
using Ipfs.CoreApi;
using System.Xml.Linq;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public partial class IPFSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id).Result;
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        //{
        //    return LoadAvatarAsync(username).Result;
        //}

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string providerKey, int version = 0)
        //{
        //    return await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.Id == Id);
        }


        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            //return await LoadAvatarAsync(providerKey);
            return await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] == providerKey);
        }


        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            string json = "";

            result = await LoadHolonsForParentTemplateAsync(a =>
                a.ProviderUniqueStorageKey.Where(a => a.Value == providerKey).Any() && a.HolonType == type);

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.Id == id);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync().Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            string json = "";
            json = await LoadStringToJson(avatarDetailsFileAddress);
            AvatarsDetailsList = (List<IAvatarDetail>) JsonConvert.DeserializeObject(json);
            return new OASISResult<IEnumerable<IAvatarDetail>>(AvatarsDetailsList);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return new OASISResult<IAvatarDetail>(await SaveAvatarDetailToFile(avatarDetail));
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.email == avatarEmail);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await LoadAvatarTemplateAsync(a => a.login == avatarUsername);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.login == avatarUsername);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.email == avatarEmail);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarEmail).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar = await LoadAvatarTemplateAsync(a => a.email == avatarEmail);

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarByEmailAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar = await LoadAvatarTemplateAsync(a => a.login == avatarUsername);

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarByUsernameAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> res = new OASISResult<IHolon>();

            res.Result = await SaveHolonToFile(holon);
            return res;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            List<IHolon> savedHolons = new List<IHolon>();

            foreach (var holon in holons)
                savedHolons.Add(await SaveHolonToFile(holon));

            return new OASISResult<IEnumerable<IHolon>>(savedHolons);
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate IPFS provider: {activateResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from IPFS: {ex.Message}", ex);
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
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate IPFS provider: {activateResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from IPFS: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
			return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

		public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
		{
			var result = new OASISResult<bool>();
			try
			{
				foreach (var holon in holons)
					await SaveHolonToFile(holon);
				result.Result = true;
				result.Message = "Holons imported into IPFS successfully.";
			}
			catch (Exception ex)
			{
				OASISErrorHandling.HandleError(ref result, $"Error importing holons into IPFS: {ex.Message}");
			}
			return result;
		}

		public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
		{
			var result = new OASISResult<IEnumerable<IHolon>>();
			try
			{
				var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, version: version);
				if (allHolonsResult.IsError)
					result.IsError = true;
					result.Message = allHolonsResult.Message;
					result.Exception = allHolonsResult.Exception;
					return result;

				var holons = allHolonsResult.Result.Where(h => h.CreatedByAvatarId == avatarId || h.ParentHolonId == avatarId).ToList();
				result.Result = holons;
				result.Message = "Exported holons for avatar (by Id) from IPFS.";
			}
			catch (Exception ex)
			{
				OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by Id from IPFS: {ex.Message}");
			}
			return result;
		}

		public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
		{
			var result = new OASISResult<IEnumerable<IHolon>>();
			try
			{
				var avatarResult = await LoadAvatarTemplateAsync(a => a.login == avatarUsername);
				if (avatarResult.IsError || avatarResult.Result == null)
					result.IsError = true;
					result.Message = avatarResult.Message;
					result.Exception = avatarResult.Exception;
					return result;

				return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
			}
			catch (Exception ex)
			{
				OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from IPFS: {ex.Message}");
				return result;
			}
		}

		public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
		{
			var result = new OASISResult<IEnumerable<IHolon>>();
			try
			{
				var avatarResult = await LoadAvatarTemplateAsync(a => a.email == avatarEmailAddress);
				if (avatarResult.IsError || avatarResult.Result == null)
					result.IsError = true;
					result.Message = avatarResult.Message;
					result.Exception = avatarResult.Exception;
					return result;

				return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
			}
			catch (Exception ex)
			{
				OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from IPFS: {ex.Message}");
				return result;
			}
		}

		public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
		{
			return await LoadAllHolonsAsync(HolonType.All, version: version);
		}

		public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
		{
			return ImportAsync(holons).Result;
		}

		public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
		{
			return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
		}

		public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
		{
			return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
		}

		public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
		{
			return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
		}

		public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
		{
			return ExportAllAsync(version).Result;
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
				var allHolonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
				if (allHolonsResult.IsError)
					result.IsError = true;
					result.Message = allHolonsResult.Message;
					result.Exception = allHolonsResult.Exception;
					return result;

				var filtered = allHolonsResult.Result.Where(h => h.MetaData != null && h.MetaData.ContainsKey(metaKey) && Convert.ToString(h.MetaData[metaKey]) == metaValue).ToList();
				result.Result = filtered;
			}
			catch (Exception ex)
			{
				OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from IPFS: {ex.Message}");
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
				var allHolonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
				if (allHolonsResult.IsError)
					result.IsError = true;
					result.Message = allHolonsResult.Message;
					result.Exception = allHolonsResult.Exception;
					return result;

				IEnumerable<IHolon> filtered = allHolonsResult.Result;
				if (metaKeyValuePairs != null && metaKeyValuePairs.Count > 0)
				{
					if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
					{
						filtered = filtered.Where(h => h.MetaData != null && metaKeyValuePairs.All(kvp => h.MetaData.ContainsKey(kvp.Key) && Convert.ToString(h.MetaData[kvp.Key]) == kvp.Value));
					}
					else
					{
						filtered = filtered.Where(h => h.MetaData != null && metaKeyValuePairs.Any(kvp => h.MetaData.ContainsKey(kvp.Key) && Convert.ToString(h.MetaData[kvp.Key]) == kvp.Value));
					}
				}

				result.Result = filtered.ToList();
			}
			catch (Exception ex)
			{
				OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from IPFS: {ex.Message}");
			}
			return result;
		}

		public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
		{
			return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
		}
    }
}
