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
                        // Fallback simple search for current model
                        if (!string.IsNullOrWhiteSpace(searchTextGroup.SearchQuery))
                        {
                            var q = searchTextGroup.SearchQuery.ToLower();
                            result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                (a.Name ?? string.Empty).ToLower().Contains(q) || (a.Description ?? string.Empty).ToLower().Contains(q)));
                            result.Result.SearchResultHolons.AddRange(holons.Where(h =>
                                (h.Name ?? string.Empty).ToLower().Contains(q) || (h.Description ?? string.Empty).ToLower().Contains(q)));
                        }

                        if (searchTextGroup.SearchAvatars)
                        {
                            // Simplified: only name/username/email/description for current model
                            var qLower = (searchTextGroup.SearchQuery ?? string.Empty).ToLower();
                            result.Result.SearchResultAvatars.AddRange(avatars.Where(a =>
                                (a.Name ?? string.Empty).ToLower().Contains(qLower)
                                || (a.Username ?? string.Empty).ToLower().Contains(qLower)
                                || (a.Email ?? string.Empty).ToLower().Contains(qLower)
                                || (a.Description ?? string.Empty).ToLower().Contains(qLower)));
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
                            // Simplified: only name/description/holontype/date for current model
                            var qLower = (searchTextGroup.SearchQuery ?? string.Empty).ToLower();
                            result.Result.SearchResultHolons.AddRange(holons.Where(h =>
                                (h.Name ?? string.Empty).ToLower().Contains(qLower)
                                || (h.Description ?? string.Empty).ToLower().Contains(qLower)
                                || h.HolonType.ToString().ToLower().Contains(qLower)
                                || h.CreatedDate.ToString().ToLower().Contains(qLower)
                                || h.ModifiedDate.ToString().ToLower().Contains(qLower)));
                        }
                    }
                }
            }

            return result;
        }

    }
}
