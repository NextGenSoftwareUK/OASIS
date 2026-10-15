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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

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
            try
            {
                var saved=OASISDNAManager.SaveDNA(_OASISDNAPath, _OASISDNA);
                EnsureIndexPublication(fsn.Id,previousRoot,saved);
            }
            catch(IPFSIndexPublicationException) { throw; }
            catch(Exception cause) { throw new IPFSIndexPublicationException(fsn.Id,previousRoot,cause); }

            return fsn.Id;
        }

        private static void EnsureIndexPublication(string cid,string previousRoot,OASISResult<bool> saved)
        {
            if(saved == null || saved.IsError || !saved.Result)
                throw new IPFSIndexPublicationException(cid,previousRoot,saved?.Exception
                    ?? new InvalidOperationException(saved?.Message ?? "DNA pointer persistence returned no result."));
        }

        private async Task PublishAcknowledgedRecordAsync(string recordCid, HolonResume record)
        {
            try { ReplaceIndexEntry(recordCid, record); await SaveLookupToFile(_idLookup); }
            catch (Exception cause) { throw new IPFSRecordPublicationException(recordCid, cause); }
        }

        private async Task SavePrivateEntityAsync(IHolonBase entity, string kind, HolonResume index)
        {
            if (entity == null || entity.Id == Guid.Empty) throw new ArgumentException("IPFS entity identity must be nonempty.");
            _idLookup = await LoadLookupToJson();
            var previous = FindIndexEntry(kind, row => row.Id == entity.Id, true);
            entity.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            entity.PreviousVersionProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            if (previous.Value != null) entity.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = previous.Key;
            index.Id = entity.Id;
            index.RecordKind = kind;
            var json = ProtectRecord(kind, entity.Id, OasisJson.Serialize(entity));
            var acknowledged = await IPFSClient.FileSystem.AddTextAsync(json);
            var cid = acknowledged.Id.ToString();
            try
            {
                entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = cid;
                index.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType,string>(entity.ProviderUniqueStorageKey);
                await PublishAcknowledgedRecordAsync(cid, index);
            }
            catch (IPFSRecordPublicationException) { throw; }
            catch (Exception cause) { throw new IPFSRecordPublicationException(cid, cause); }
        }

        public async Task<IAvatar> SaveAvatarToFile(IAvatar avatar)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            await SavePrivateEntityAsync(avatar, "avatar", new HolonResume {
                login=avatar.Username, email=avatar.Email, HolonType=HolonType.Avatar });
            return avatar;
        }

        public async Task<IHolon> SaveHolonToFile(IHolon holon)
        {
            if (holon == null) throw new ArgumentNullException(nameof(holon));
            await SavePrivateEntityAsync(holon, "holon", new HolonResume {
                ParentHolonId=holon.ParentHolonId, HolonType=holon.HolonType });
            return holon;
        }

        public async Task<IAvatarDetail> SaveAvatarDetailToFile(IAvatarDetail avatarDetail)
        {
            if (avatarDetail == null) throw new ArgumentNullException(nameof(avatarDetail));
            await SavePrivateEntityAsync(avatarDetail, "avatar-detail", new HolonResume {
                login=avatarDetail.Username, email=avatarDetail.Email, HolonType=HolonType.AvatarDetail });
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
            var result=new OASISResult<ISearchResults>();
            try
            {
                StoredRecordSearch.Validate(searchParams,version,maxChildDepth);
                var groups=searchParams.SearchGroups ?? new List<ISearchGroupBase>();
                var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
                var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
                var snapshot=await LoadLookupToJson();
                var avatars=wantsAvatars ? await ReadIndexSnapshotAsync<Avatar>(snapshot,"avatar",row=>true):new List<Avatar>();
                var holons=wantsHolons || searchParams.ParentId!=Guid.Empty
                    ? await ReadIndexSnapshotAsync<Holon>(snapshot,"holon",row=>true):new List<Holon>();
                var project=HolonGraphProjection.Create(holons,loadChildren,recursive,maxChildDepth,0,true,
                    source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
                var found=new SearchResults();
                found.SearchResultAvatars.AddRange(avatars.Where(row=>StoredRecordSearch.Matches(row,true,searchParams,holons)));
                if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>StoredRecordSearch.Matches(row,false,searchParams,holons)).Select(project));
                found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
                result.Result=found; result.IsLoaded=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,"IPFS private search failed: "+cause.Message,cause);
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

    }
}
