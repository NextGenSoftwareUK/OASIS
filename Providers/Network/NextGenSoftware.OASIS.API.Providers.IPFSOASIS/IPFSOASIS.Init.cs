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
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public partial class IPFSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                IPFSClient = new IpfsClient(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
                using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                Dictionary<string, string> version = await IPFSClient.VersionAsync(timeout.Token);

                if (version == null || version.Count == 0)
                    throw new InvalidOperationException("The IPFS daemon returned an empty version response.");

                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                IsProviderActivated = false;

                IPFSClient = null;
                OASISErrorHandling.HandleError(ref result, $"Error Occured In IPFSOASIS Provider In ActivateProviderAsync Method. Reason: {e}",e);
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().GetAwaiter().GetResult();
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            // This provider owns a client handle, not the externally managed IPFS daemon.
            IPFSClient = null;
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> {Result=true});
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
            var address=_OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            if(string.IsNullOrWhiteSpace(address))
            {
                if(_idLookup.Count>0) throw new InvalidOperationException("IPFS lookup root is missing while a populated index is retained; refusing to replace it with an empty index.");
                return _idLookup;
            }
            if(IPFSClient == null) throw new InvalidOperationException("An active IPFS client is required to read the configured lookup root.");
            var json=UnprotectIndex(await LoadStringToJson(address));
            var loaded=DeserializeLookup(json);
            _idLookup=loaded;
            return _idLookup;
        }

        private static Dictionary<string,HolonResume> DeserializeLookup(string json)
            => JsonConvert.DeserializeObject<Dictionary<string,HolonResume>>(json)
                ?? throw new InvalidDataException("IPFS lookup root contained no index object.");

        public async Task<string> SaveJsonToFile<T>(List<T> list)
        {
            string json = JsonConvert.SerializeObject(list);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            return (string) fsn.Id;
        }

        public async Task<string> SaveLookupToFile(Dictionary<string, HolonResume> idLookup)
        {
            string json = ProtectIndex(JsonConvert.SerializeObject(idLookup));
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);

            var previousRoot=_OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            _OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress = fsn.Id;
            var saved=OASISDNAManager.SaveDNA(_OASISDNAPath, _OASISDNA);
            EnsureIndexPublication(fsn.Id,previousRoot,saved);

            return fsn.Id;
        }

        private static void EnsureIndexPublication(string cid,string previousRoot,OASISResult<bool> saved)
        {
            if(saved == null || saved.IsError || !saved.Result)
                throw new IPFSIndexPublicationException(cid,previousRoot,saved?.Exception
                    ?? new InvalidOperationException(saved?.Message ?? "DNA pointer persistence returned no result."));
        }

        public async Task<IAvatar> SaveAvatarToFile(IAvatar avatar)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            if(avatar.Id==Guid.Empty) throw new ArgumentException("IPFS entity identity must be nonempty.");
            var previous=FindIndexEntry("avatar",row=>row.Id==avatar.Id,true);
            var dico=new HolonResume {RecordKind="avatar"};
            if(previous.Value != null)
                avatar.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]=previous.Key;

            string json = ProtectRecord("avatar",avatar.Id,JsonConvert.SerializeObject(avatar));
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatar.Id;
            dico.login = avatar.Username;
            dico.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType,string>(avatar.ProviderUniqueStorageKey);
            dico.email = avatar.Email;
            dico.HolonType = HolonType.Avatar;

            ReplaceIndexEntry(fsn.Id,dico);


            await SaveLookupToFile(_idLookup);

            return avatar;
        }

        public async Task<IHolon> SaveHolonToFile(IHolon holon)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            if(holon.Id==Guid.Empty) throw new ArgumentException("IPFS entity identity must be nonempty.");
            var previous=FindIndexEntry("holon",row=>row.Id==holon.Id,true);
            var dico=new HolonResume {RecordKind="holon"};
            if(previous.Value != null)
                holon.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]=previous.Key;

            string json = ProtectRecord("holon",holon.Id,JsonConvert.SerializeObject(holon));
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.
            dico.Id = holon.Id;
            dico.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType,string>(holon.ProviderUniqueStorageKey);
            dico.ParentHolonId = holon.ParentHolonId;
            dico.HolonType = holon.HolonType;

            ReplaceIndexEntry(fsn.Id,dico);

            string id = await SaveLookupToFile(_idLookup);

            return holon;
        }

        public async Task<IAvatarDetail> SaveAvatarDetailToFile(IAvatarDetail avatarDetail)
        {
            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
            _idLookup = await LoadLookupToJson();
            if(avatarDetail.Id==Guid.Empty) throw new ArgumentException("IPFS entity identity must be nonempty.");
            var previous=FindIndexEntry("avatar-detail",row=>row.Id==avatarDetail.Id,true);
            var dico=new HolonResume {RecordKind="avatar-detail"};
            if(previous.Value != null)
                avatarDetail.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]=previous.Key;

            string json = ProtectRecord("avatar-detail",avatarDetail.Id,JsonConvert.SerializeObject(avatarDetail));
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = fsn.Id;

            // we store just values that we will use as a filter of search in other methods.

            dico.Id = avatarDetail.Id;
            dico.login = avatarDetail.Username;           
            dico.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType,string>(avatarDetail.ProviderUniqueStorageKey);
            dico.email = avatarDetail.Email;
            dico.HolonType = HolonType.AvatarDetail;

            ReplaceIndexEntry(fsn.Id,dico);


            await SaveLookupToFile(_idLookup);

            return avatarDetail;
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
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
