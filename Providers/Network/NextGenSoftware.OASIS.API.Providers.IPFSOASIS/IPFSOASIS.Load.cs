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
        //TODO: Need to compare the 2 versions of the search function later and keep the best or merge the the best from both! ;-)
        /*
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = await LoadAllAvatarsAsync();
            OASISResult<IEnumerable<IHolon>> holonsResult = await LoadAllHolonsAsync();
            List<IAvatar> avatars = new List<IAvatar>();
            List<IHolon> holons = new List<IHolon>();

            if (avatarsResult.Result != null && !avatarsResult.IsError)
                avatars = avatarsResult.Result.ToList();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in IPFSOASIS in SearchAsync loading the avatars. Reason: {avatarsResult.Message}");

            if (holonsResult.Result != null && !holonsResult.IsError)
                holons = holonsResult.Result.ToList();
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in IPFSOASIS in SearchAsync loading the holons. Reason: {holonsResult.Message}");

            if (!result.IsError)
            {
                foreach (ISearchGroupBase searchGroup in searchParams.SearchGroups)
                {
                    ISearchTextGroup searchTextGroup = searchGroup as ISearchTextGroup;

                    if (searchTextGroup != null)
                    {
                        if (searchTextGroup.SearchAvatars)
                        {
                            if (searchTextGroup.AvatarSearchParams.FirstName)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.FirstName.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            if (searchTextGroup.AvatarSearchParams.LastName)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.LastName.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            // Search additional avatar properties
                            if (searchTextGroup.AvatarSearchParams.Username)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.Username.Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.AvatarSearchParams.Email)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.Email.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            // Search ALL avatar properties comprehensively
                            //if (searchTextGroup.AvatarSearchParams.Address)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       !string.IsNullOrEmpty(a.Address) && a.Address.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Country)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       !string.IsNullOrEmpty(a.Country) && a.Country.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Postcode)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       !string.IsNullOrEmpty(a.Postcode) && a.Postcode.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Mobile)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       !string.IsNullOrEmpty(a.Mobile) && a.Mobile.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Landline)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       !string.IsNullOrEmpty(a.Landline) && a.Landline.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Title)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       !string.IsNullOrEmpty(a.Title) && a.Title.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.DOB)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.DOB.HasValue && a.DOB.Value.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            if (searchTextGroup.AvatarSearchParams.AvatarType)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.AvatarType.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            //if (searchTextGroup.AvatarSearchParams.KarmaAkashicRecords)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.KarmaAkashicRecords.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Level)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.Level.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.XP)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.XP.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.HP)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.HP.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Mana)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.Mana.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.AvatarSearchParams.Stamina)
                            //{
                            //    result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                            //       a.Stamina.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            if (searchTextGroup.AvatarSearchParams.CreatedDate)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.CreatedDate.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.AvatarSearchParams.ModifiedDate)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.ModifiedDate.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                        }

                        if (searchTextGroup.SearchHolons)
                        {
                            if (searchTextGroup.HolonSearchParams.Name)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.Name.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            if (searchTextGroup.HolonSearchParams.Description)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.Description.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            // Search ALL holon properties comprehensively
                            if (searchTextGroup.HolonSearchParams.HolonType)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.HolonType.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.HolonSearchParams.CreatedDate)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.CreatedDate.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.HolonSearchParams.ModifiedDate)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.ModifiedDate.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.HolonSearchParams.Version)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.Version.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            //if (searchTextGroup.HolonSearchParams.ParentId)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       a.ParentId.HasValue && a.ParentId.Value.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.HolonSearchParams.ProviderKey)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       !string.IsNullOrEmpty(a.ProviderKey) && a.ProviderKey.Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.HolonSearchParams.PreviousVersionId)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       a.PreviousVersionId.HasValue && a.PreviousVersionId.Value.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.HolonSearchParams.NextVersionId)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       a.NextVersionId.HasValue && a.NextVersionId.Value.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            if (searchTextGroup.HolonSearchParams.IsActive)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                                   a.IsActive.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            //if (searchTextGroup.HolonSearchParams.IsChanged)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       a.IsChanged.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.HolonSearchParams.IsNew)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       a.IsNew.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            //if (searchTextGroup.HolonSearchParams.IsDeleted)
                            //{
                            //    result.Result.SearchResultHolons.AddRange(holons.Where(a =>
                            //       a.IsDeleted.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            //}
                            if (searchTextGroup.HolonSearchParams.DeletedByAvatarId)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a => a.DeletedByAvatarId.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.HolonSearchParams.DeletedDate)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a => a.DeletedDate.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.HolonSearchParams.CreatedByAvatarId)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a => a.CreatedByAvatarId.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                            if (searchTextGroup.HolonSearchParams.ModifiedByAvatarId)
                            {
                                result.Result.SearchResultHolons.AddRange(holons.Where(a => a.ModifiedByAvatarId.ToString().Contains(searchTextGroup.SearchQuery)).ToList());
                            }
                        }
                    }
                }
            }

            return result;
        }*/

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadHolonTemplateAsync(a => a.Id == id);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadHolonTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());
        }

        /*** Templates****/

        public async Task<OASISResult<IAvatar>> LoadAvatarTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IAvatar avatar = JsonConvert.DeserializeObject<Avatar>(json);

            return new OASISResult<IAvatar>(avatar);
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IAvatarDetail avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(json);

            return new OASISResult<IAvatarDetail>(avatarDetail);
        }


        public async Task<OASISResult<IHolon>> LoadHolonTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IHolon holon = JsonConvert.DeserializeObject<Holon>(json);

            return new OASISResult<IHolon>(holon);
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentTemplateAsync(Func<HolonResume, bool> predicate)
        {
            List<IHolon> holons = new List<IHolon>();
            string json = "";
            _idLookup = await LoadLookupToJson();

            IEnumerable<HolonResume> holonsDico = _idLookup.Values.Where(predicate).AsEnumerable();

            foreach (var h in holonsDico)
            {
                string holonAddress = _idLookup.FirstOrDefault(a => a.Value.Id == h.Id).Key;
                
                json = await LoadStringToJson(holonAddress);
                IHolon holon = JsonConvert.DeserializeObject<Holon>(json);
                holons.Add(holon);
            }

            return new OASISResult<IEnumerable<IHolon>>(holons);
        }
        /***********/

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadHolonsForParentTemplateAsync(a => a.ParentHolonId == id && a.HolonType == type);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type).Result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar = await LoadAvatarTemplateAsync(a => a.Id == id);

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatar =
                    await LoadAvatarTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());

                avatar.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.Result.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                OASISResult<IHolon> holon = await LoadHolonTemplateAsync(a => a.Id == id);

                holon.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                holon.Result.DeletedDate = DateTime.Now;
                result.Result = await SaveHolonToFile(holon.Result);
            }
            catch (Exception ex)
            {
                //result.Result = true;
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeleteHolonAsync method in IPFS Provider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                OASISResult<IHolon> holon =
                    await LoadHolonTemplateAsync(a => a.ProviderUniqueStorageKey.Where(b => b.Value == providerKey).Any());

                holon.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                holon.Result.DeletedDate = DateTime.Now;
                result.Result = await SaveHolonToFile(holon.Result);
            }
            catch (Exception ex)
            {
                //result.Result = true;
                OASISErrorHandling.HandleError(ref result, $"Error occured in DeleteHolonAsync method in IPFS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync().Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            List<IAvatar> avatars = new List<IAvatar>();
            string json = "";
            _idLookup = await LoadLookupToJson();

            IEnumerable<HolonResume> Dico = _idLookup.Values.AsEnumerable();

            foreach (var d in Dico)
            {
                string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == d.Id).Key;

                json = await LoadStringToJson(avatarAddress);

                IAvatar avatar = (IAvatar) JsonConvert.DeserializeObject<Avatar>(json);

                avatars.Add(avatar);
            }

            return new OASISResult<IEnumerable<IAvatar>>(avatars.AsEnumerable());
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.Holon, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.Holon, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            List<IHolon> HolonsList = new List<IHolon>();
            string json = "";
            _idLookup = await LoadLookupToJson();

            IEnumerable<HolonResume> Dico = _idLookup.Values.AsEnumerable();

            foreach (var d in Dico)
            {
                string HolonAddress = _idLookup.FirstOrDefault(a => a.Value.Id == d.Id).Key;
      
                json = await LoadStringToJson(HolonAddress);
                IHolon holon = JsonConvert.DeserializeObject<Holon>(json);
                HolonsList.Add(holon);
            }

            return new OASISResult<IEnumerable<IHolon>>(HolonsList.Where(a => a.HolonType == type));
        }

    }
}
