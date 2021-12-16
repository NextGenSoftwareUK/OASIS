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
using Ipfs.Engine;
using Ipfs;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public class IPFSOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public IpfsClient IPFSClient;
        public IpfsEngine IPFSEngine; //= new IpfsEngine();

        private List<IAvatarDetail> AvatarsDetailsList;
        private string avatarDetailsFileAddress;
        private Dictionary<string, HolonResume> _idLookup = new Dictionary<string, HolonResume>();
        private OASISDNA _OASISDNA;
        private string _OASISDNAPath;

        public IPFSOASIS()
        {
            OASISDNAManager.LoadDNA();
            _OASISDNA = OASISDNAManager.OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;

            Init();
        }

        public IPFSOASIS(string OASISDNAPath)
        {
            _OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(_OASISDNAPath);
            _OASISDNA = OASISDNAManager.OASISDNA;
            Init();
        }

        public IPFSOASIS(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAManager.OASISDNAPath;
            Init();
        }

        public IPFSOASIS(OASISDNA OASISDNA, string OASISDNAPath)
        {
            _OASISDNA = OASISDNA;
            _OASISDNAPath = OASISDNAPath;
            Init();
        }

        private void Init()
        {
            this.ProviderName = "IPFSOASIS";
            this.ProviderDescription = "IPFS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.IPFSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public override void ActivateProvider()
        {
            IPFSClient = new IpfsClient(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            IPFSClient.ShutdownAsync();
            IPFSClient = null;
            base.DeActivateProvider();
        }


        public async Task<string> LoadFileToJson(string address)
        {
            await using var stream = await IPFSClient.FileSystem.ReadFileAsync(address);
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.ToArray();
            return Encoding.ASCII.GetString(ms.ToArray());
        }

        public async Task<string> LoadStringToJson(string address)
        {
            string text = await IPFSClient.FileSystem.ReadAllTextAsync((Cid) address);
            return text;
        }

        /******************************/
        public async Task<Dictionary<string, HolonResume>> LoadLookupToJson()
        {
            try
            {
                string json = await LoadStringToJson(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                _idLookup = JsonConvert.DeserializeObject<Dictionary<string, HolonResume>>(json);
            }
            catch
            {
                _idLookup = new Dictionary<string, HolonResume>();
            }

            return _idLookup;
        }

        public async Task<string> SaveJsonToFile<T>(List<T> list)
        {
            string json = JsonConvert.SerializeObject(list);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            return (string) fsn.Id;
        }

        public async Task<string> SaveLookupToFile(Dictionary<string, HolonResume> idLookup)
        {
            string json = JsonConvert.SerializeObject(idLookup);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);

            _OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress = fsn.Id;
            OASISDNAManager.SaveDNA(_OASISDNAPath, _OASISDNA);

            return fsn.Id;
        }

        public async Task<IAvatar> SaveAvatarToFile(IAvatar avatar)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == avatar.Id);

            // in case there is no element in _idlookup dictionary
            if (dico == null)
                dico = new HolonResume();


            if (_idLookup.Count(a => a.Value.Id == avatar.Id) > 0)
                avatar.PreviousVersionProviderKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == avatar.Id).Key;

            string json = JsonConvert.SerializeObject(avatar);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatar.ProviderKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatar.Id;
            dico.login = avatar.Username;
            dico.password = avatar.Password;
            dico.ProviderKey = avatar.ProviderKey;
            dico.email = avatar.Email;
            dico.HolonType = HolonType.Avatar;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;


            await SaveLookupToFile(_idLookup);

            return avatar;
        }

        public async Task<IHolon> SaveHolonToFile(IHolon holon)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == holon.Id);

            // in case there is no element in _idlookup dictionary
            if (dico == null)
                dico = new HolonResume();

            if (_idLookup.Count(a => a.Value.Id == holon.Id) > 0)
                holon.PreviousVersionProviderKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == holon.Id).Key;

            string json = JsonConvert.SerializeObject(holon);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            holon.ProviderKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.
            dico.Id = holon.Id;
            dico.ProviderKey = holon.ProviderKey;
            dico.ParentHolonId = holon.ParentHolonId;
            dico.HolonType = holon.HolonType;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;

            string id = await SaveLookupToFile(_idLookup);
            return holon;
        }

        public async Task<IAvatarDetail> SaveAvatarDetailToFile(IAvatarDetail avatarDetail)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == avatarDetail.Id);

            // in case there is no element in _idlookup dictionary
            if (dico == null)
                dico = new HolonResume();


            if (_idLookup.Count(a => a.Value.Id == avatarDetail.Id) > 0)
                avatarDetail.PreviousVersionProviderKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == avatarDetail.Id).Key;

            string json = JsonConvert.SerializeObject(avatarDetail);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatarDetail.ProviderKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatarDetail.Id;
            dico.login = avatarDetail.Username;           
            dico.ProviderKey = avatarDetail.ProviderKey;
            dico.email = avatarDetail.Email;
            dico.HolonType = HolonType.AvatarDetail;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;


            await SaveLookupToFile(_idLookup);

            return avatarDetail;
        }

        public override async Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            return await LoadAvatarTemplateAsync(a => a.login == username && a.password == password);
        }
        /************************************************************/

        public async Task<string> SaveTextToFile(string text)
        {
            var fsn = await IPFSClient.FileSystem.AddTextAsync(text);
            return fsn.Id;
        }


        public override IAvatar LoadAvatar(string username, string password)
        {
            return LoadAvatarAsync(username, password).Result;
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        //public override async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        //{
        //    if (AvatarsList == null)
        //        AvatarsList = new List<IAvatar>();

        //    AvatarsList.Add(Avatar);


        //    avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);

        //    return Avatar;
        //}

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
        {
            return await SaveAvatarToFile(avatar);
        }

        public override async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            ISearchResults result = new SearchResults();
            IEnumerable<IAvatar> Avatars = await LoadAllAvatarsAsync();
            IEnumerable<IHolon> Holons = await LoadAllHolonsAsync();

            Avatars = Avatars.Where(a =>
                a.Name.Contains(searchTerm.SearchQuery) | a.Description.Contains(searchTerm.SearchQuery)).ToList();
            Holons = Holons.Where(h =>
                h.Name.Contains(searchTerm.SearchQuery) | h.Description.Contains(searchTerm.SearchQuery)).ToList();

            foreach (var h in Holons)
                result.SearchResultHolons.Add((Holon) h);

            foreach (var ava in Avatars)
                result.SearchResultHolons.Add((Holon) ava);

            return result;
        }

        public override IHolon LoadHolon(Guid id)
        {
            return LoadHolonAsync(id).Result;
        }

        public override async Task<IHolon> LoadHolonAsync(Guid id)
        {
            return await LoadHolonTemplateAsync(a => a.Id == id);
        }

        public override IHolon LoadHolon(string providerKey)
        {
            return LoadHolonAsync(providerKey).Result;
        }

        public override async Task<IHolon> LoadHolonAsync(string providerKey)
        {
            return await LoadHolonTemplateAsync(a => a.ProviderKey.Where(b => b.Value == providerKey).Any());
        }

        /*** Templates****/

        public async Task<IAvatar> LoadAvatarTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IAvatar avatar = JsonConvert.DeserializeObject<Avatar>(json);

            return avatar;
        }

        public async Task<IAvatarDetail> LoadAvatarDetailTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IAvatarDetail avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(json);

            return avatarDetail;
        }


        public async Task<IHolon> LoadHolonTemplateAsync(Func<HolonResume, bool> predicate)
        {
            string json = "";
            _idLookup = await LoadLookupToJson();

            HolonResume avatarDico = _idLookup.Values.FirstOrDefault(predicate);
            string avatarAddress = _idLookup.FirstOrDefault(a => a.Value.Id == avatarDico.Id).Key;

            json = await LoadStringToJson(avatarAddress);
            IHolon holon = JsonConvert.DeserializeObject<Holon>(json);

            return holon;
        }

        public async Task<IEnumerable<IHolon>> LoadHolonsForParentTemplateAsync(Func<HolonResume, bool> predicate)
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

            return holons;
        }
        /***********/

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            return LoadHolonsForParentAsync(id, type).Result;
        }

        public override async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All)
        {
            return await LoadHolonsForParentTemplateAsync(a => a.ParentHolonId == id && a.HolonType == type);
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            return LoadHolonsForParentAsync(providerKey, type).Result.Result;
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
                IAvatar avatar = await LoadAvatarTemplateAsync(a => a.Id == id);

                avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                IAvatar avatar =
                    await LoadAvatarTemplateAsync(a => a.ProviderKey.Where(b => b.Value == providerKey).Any());

                avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            return DeleteHolonAsync(id, softDelete).Result;
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            return DeleteHolonAsync(providerKey, softDelete).Result;
        }

        public override async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            try
            {
                IHolon holon = await LoadHolonTemplateAsync(a => a.Id == id);

                holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                holon.DeletedDate = DateTime.Now;

                await SaveHolonToFile(holon);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                IHolon holon =
                    await LoadHolonTemplateAsync(a => a.ProviderKey.Where(b => b.Value == providerKey).Any());

                holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                holon.DeletedDate = DateTime.Now;

                await SaveHolonToFile(holon);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            return LoadAllHolons(Type);
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            return LoadAllAvatarsAsync().Result;
        }

        public override async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
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

            return avatars.AsEnumerable();
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
        {
            return LoadAllHolonsAsync(type).Result;
        }

        public override async Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
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

            return HolonsList.Where(a => a.HolonType == type);
        }


        public override IAvatar LoadAvatar(Guid Id)
        {
            return LoadAvatarAsync(Id).Result;
        }

        public override IAvatar LoadAvatar(string username)
        {
            return LoadAvatarAsync(username).Result;
        }

        public override async Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            return await LoadAvatarTemplateAsync(a => a.ProviderKey.Where(b => b.Value == providerKey).Any());
        }

        public override async Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            return await LoadAvatarTemplateAsync(a => a.Id == Id);
        }


        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            return LoadAvatarForProviderKeyAsync(providerKey).Result;
        }

        public override async Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            return await LoadAvatarAsync(providerKey);
        }


        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All)
        {
            string json = "";
            OASISResult<IEnumerable<IHolon>> res = new OASISResult<IEnumerable<IHolon>>();

            res.Result = await LoadHolonsForParentTemplateAsync(a =>
                a.ProviderKey.Where(a => a.Value == providerKey).Any() && a.HolonType == type);

            return res;
        }

        public override IAvatarDetail LoadAvatarDetail(Guid id)
        {
            return LoadAvatarDetailAsync(id).Result;
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.Id == id);
        }

        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            return LoadAllAvatarDetailsAsync().Result;
        }

        public override async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            string json = "";
            json = await LoadStringToJson(avatarDetailsFileAddress);
            AvatarsDetailsList = (List<IAvatarDetail>) JsonConvert.DeserializeObject(json);
            return AvatarsDetailsList;
        }

        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
           return await SaveAvatarDetailToFile(avatarDetail);
        }

        public override IAvatar LoadAvatarByUsername(string avatarUsername)
        {
            return LoadAvatarByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
        {
            return await LoadAvatarTemplateAsync(a => a.email == avatarEmail);
        }

        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
        {
            return await LoadAvatarTemplateAsync(a => a.login == avatarUsername);
        }

        public override IAvatar LoadAvatarByEmail(string avatarEmail)
        {
            return LoadAvatarByEmailAsync(avatarEmail).Result;
        }

        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail).Result;
        }

        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername).Result;
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            return await LoadAvatarDetailTemplateAsync(a => a.login == avatarUsername);
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
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
                IAvatar avatar = await LoadAvatarTemplateAsync(a => a.email == avatarEmail);

                avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarByEmailAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                IAvatar avatar = await LoadAvatarTemplateAsync(a => a.login == avatarUsername);

                avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                avatar.DeletedDate = DateTime.Now;

                await SaveAvatarToFile(avatar);
                result.Result = true;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An error occured in DeleteAvatarByUsernameAsync in IPFSOASIS Provider. Reason: {ex.ToString()}");
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
        {
            return SaveHolonAsync(holon, saveChildrenRecursive).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
        {
            OASISResult<IHolon> res = new OASISResult<IHolon>();

            res.Result = await SaveHolonToFile(holon);
            return res;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
            bool saveChildrenRecursive = true)
        {
            return SaveHolonsAsync(holons, saveChildrenRecursive).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildrenRecursive = true)
        {
            List<IHolon> savedHolons = new List<IHolon>();

            foreach (var holon in holons)
                savedHolons.Add(await SaveHolonToFile(holon));

            return new OASISResult<IEnumerable<IHolon>>(savedHolons);
        }
    }
}