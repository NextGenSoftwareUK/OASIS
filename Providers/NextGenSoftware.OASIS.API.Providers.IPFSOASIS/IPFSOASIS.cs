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
    public class IPFSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        public IpfsClient IPFSClient;
        //public IpfsEngine IPFSEngine; //= new IpfsEngine();

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

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                IPFSClient = new IpfsClient(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In IPFSOASIS Provider In ActivateProvider Method. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                IPFSClient = new IpfsClient(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In IPFSOASIS Provider In ActivateProviderAsync Method. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                IPFSClient.ShutdownAsync();
                IPFSClient = null;
                result.Result = true;
                IsProviderActivated = false;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In IPFSOASIS Provider In DeActivateProvider Method. Reason: {e}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (IPFSClient != null)
                    IPFSClient.ShutdownAsync();

                IPFSClient = null;
                result.Result = true;
                IsProviderActivated = false;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In IPFSOASIS Provider In DeActivateProviderAsync Method. Reason: {e}");
            }

            return result;
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

        public string GetFileUrl(string ipfsHash, string fileName = "")
        {
            return $"{_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString}/ipfs/{ipfsHash}/{fileName}";
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
                avatar.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == avatar.Id).Key;

            string json = JsonConvert.SerializeObject(avatar);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatar.Id;
            dico.login = avatar.Username;
            dico.password = avatar.Password;
            dico.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;
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
            try
            {
                //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
                _idLookup = await LoadLookupToJson();
                HolonResume dico = _idLookup.Values.FirstOrDefault(a => a.Id == holon.Id);

                // in case there is no element in _idlookup dictionary
                if (dico == null)
                    dico = new HolonResume();

                if (_idLookup.Count(a => a.Value.Id == holon.Id) > 0)
                    holon.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] =
                        _idLookup.FirstOrDefault(a => a.Value.Id == holon.Id).Key;

                string json = JsonConvert.SerializeObject(holon);
                var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

                // we store just values that we will use as a filter of search in other methods.
                dico.Id = holon.Id;
                dico.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
                dico.ParentHolonId = holon.ParentHolonId;
                dico.HolonType = holon.HolonType;

                if (_idLookup.Count == 0)
                    _idLookup.Add(fsn.Id, dico);
                else
                    _idLookup[fsn.Id] = dico;

                string id = await SaveLookupToFile(_idLookup);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError($"Error occured in SaveHolonToFile method in IPFSOASIS Provider. Reason: {e}");
            }

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
                avatarDetail.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] =
                    _idLookup.FirstOrDefault(a => a.Value.Id == avatarDetail.Id).Key;

            string json = JsonConvert.SerializeObject(avatarDetail);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatarDetail.Id;
            dico.login = avatarDetail.Username;           
            dico.ProviderUniqueStorageKey = avatarDetail.ProviderUniqueStorageKey;
            dico.email = avatarDetail.Email;
            dico.HolonType = HolonType.AvatarDetail;

            if (_idLookup.Count == 0)
                _idLookup.Add(fsn.Id, dico);
            else
                _idLookup[fsn.Id] = dico;


            await SaveLookupToFile(_idLookup);

            return avatarDetail;
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    return await LoadAvatarTemplateAsync(a => a.login == username && a.password == password);
        //}
        /************************************************************/

        public async Task<OASISResult<IFileSystemNode>> SaveTextAsync(string text, AddFileOptions options = null)
        {
            OASISResult<IFileSystemNode> result = new OASISResult<IFileSystemNode>();

            try
            {
                result.Result = await IPFSClient.FileSystem.AddTextAsync(text, options);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveText method in IPFSOASIS Provider. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<IFileSystemNode>> SaveFileAsync(string fileName, AddFileOptions options = null)
        {
            OASISResult<IFileSystemNode> result = new OASISResult<IFileSystemNode>();

            try
            {
                result.Result = await IPFSClient.FileSystem.AddFileAsync(fileName, options);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveFile method in IPFSOASIS Provider. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<IFileSystemNode>> SaveDirectoryAsync(string path, bool recursive = true, AddFileOptions options = null)
        {
            OASISResult<IFileSystemNode> result = new OASISResult<IFileSystemNode>();

            try
            {
                result.Result = await IPFSClient.FileSystem.AddDirectoryAsync(path, recursive, options);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveDirectory method in IPFSOASIS Provider. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<IFileSystemNode>> SaveStreamAsync(Stream stream, string name = "", AddFileOptions options = null)
        {
            OASISResult<IFileSystemNode> result = new OASISResult<IFileSystemNode>();

            try
            {
                result.Result = await IPFSClient.FileSystem.AddAsync(stream, name, options);
                
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveStream method in IPFSOASIS Provider. Reason: {e}");
            }

            return result;
        }


        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    return LoadAvatarAsync(username, password).Result;
        //}

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
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

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return new OASISResult<IAvatar>(await SaveAvatarToFile(avatar));
        }

        //TODO: Just like the MongoDBOASIS SearchAync implementation, this is a WIP and only shows a small example of how to use the OASIS Search Architecture.
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
                            if (searchTextGroup.AvatarSerachParams.FirstName)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.FirstName.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            if (searchTextGroup.AvatarSerachParams.LastName)
                            {
                                result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                   a.LastName.Contains(searchTextGroup.SearchQuery)).ToList());
                            }

                            //TODO: Implement remaining properties.
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

                            //TODO: Implement remaining properties.
                        }
                    }
                }
            }

            return result;
        }

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

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
			var result = new OASISResult<IEnumerable<IPlayer>>();
			// IPFS does not provide geolocation; returning Not Supported with empty result set.
			result.Result = Enumerable.Empty<IPlayer>();
			result.Message = "GetPlayersNearMe is not supported by IPFS provider.";
			return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
			var result = new OASISResult<IEnumerable<IHolon>>();
			// IPFS does not provide geolocation; returning Not Supported with empty result set.
			result.Result = Enumerable.Empty<IHolon>();
			result.Message = "GetHolonsNearMe is not supported by IPFS provider.";
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
					return OASISResultHelper.CopyError<IEnumerable<IHolon>>(allHolonsResult);

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
					return OASISResultHelper.CopyError<IEnumerable<IHolon>>(avatarResult);

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
					return OASISResultHelper.CopyError<IEnumerable<IHolon>>(avatarResult);

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
					return OASISResultHelper.CopyError<IEnumerable<IHolon>>(allHolonsResult);

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
					return OASISResultHelper.CopyError<IEnumerable<IHolon>>(allHolonsResult);

				IEnumerable<IHolon> filtered = allHolonsResult.Result;
				if (metaKeyValuePairs != null && metaKeyValuePairs.Count > 0)
				{
					if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.MatchAll)
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
