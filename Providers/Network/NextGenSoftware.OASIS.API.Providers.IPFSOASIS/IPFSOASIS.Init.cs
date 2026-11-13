using System;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CatalogueGates =
            new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        private readonly SemaphoreSlim _unboundCatalogueGate = new(1, 1);
        private SemaphoreSlim CatalogueGate => string.IsNullOrWhiteSpace(_OASISDNAPath)
            ? _unboundCatalogueGate
            : CatalogueGates.GetOrAdd(OASISDNAManager.ResolveEffectiveOASISDNAPath(_OASISDNAPath), _ => new SemaphoreSlim(1, 1));

        private async Task<FileStream> AcquireCatalogueFileLeaseAsync()
        {
            var path = OASISDNAManager.ResolveEffectiveOASISDNAPath(_OASISDNAPath);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("A persisted DNA path is required to publish an IPFS catalogue.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var leasePath = path + ".ipfs.lock";
            var elapsed = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return new FileStream(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException cause) when (IsCatalogueLeaseContention(cause))
                {
                    if (elapsed.Elapsed >= TimeSpan.FromSeconds(30))
                        throw new TimeoutException("The IPFS catalogue writer did not release its file lease.", cause);
                    await Task.Delay(50).ConfigureAwait(false);
                }
            }
        }

        private static bool IsCatalogueLeaseContention(IOException cause)
        {
            if (OperatingSystem.IsWindows())
                return cause.HResult == unchecked((int)0x80070020);
            // FileStream exposes the raw EWOULDBLOCK errno from flock on Unix.
            return (OperatingSystem.IsLinux() && cause.HResult == 11)
                || (OperatingSystem.IsMacOS() && cause.HResult == 35);
        }

        private OASISDNA ReadCatalogueMutationDna()
        {
            var path = OASISDNAManager.ResolveEffectiveOASISDNAPath(_OASISDNAPath);
            if (!File.Exists(path))
                return _OASISDNA;

            var persisted = JsonConvert.DeserializeObject<OASISDNA>(File.ReadAllText(path));
            var settings = persisted?.OASIS?.StorageProviders?.IPFSOASIS;
            var current = _OASISDNA.OASIS.StorageProviders.IPFSOASIS;
            if (settings == null || !StringComparer.Ordinal.Equals(settings.ConnectionString, current.ConnectionString))
                throw new InvalidDataException("The persisted IPFS catalogue targets a different connection.");
            return persisted;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().GetAwaiter().GetResult();
        }

        public override Task<OASISResult<bool>> ActivateProviderAsync()
            => ActivateQuerySourceAsync(CancellationToken.None);

        /// <summary>Checks the configured native daemon without writing a catalogue,
        /// changing the default provider, or starting a replacement daemon.</summary>
        public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var client = new IpfsClient(_OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                Dictionary<string, string> version = await client.VersionAsync(timeout.Token).ConfigureAwait(false);
                timeout.Token.ThrowIfCancellationRequested();

                if (version == null || !version.TryGetValue("Version", out var nativeVersion)
                    || string.IsNullOrWhiteSpace(nativeVersion))
                    throw new InvalidOperationException("The IPFS daemon returned no native version.");

                _ipfsClient = client;
                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception e)
            {
                IsProviderActivated = false;

                _ipfsClient = null;
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
            _ipfsClient = null;
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
            var gate = CatalogueGate;
            await gate.WaitAsync().ConfigureAwait(false);
            try { return await LoadCatalogueAsync().ConfigureAwait(false); }
            finally { gate.Release(); }
        }

        private async Task<Dictionary<string, HolonResume>> LoadCatalogueAsync(OASISDNA dna = null)
        {
            var settings = (dna ?? _OASISDNA)?.OASIS?.StorageProviders?.IPFSOASIS;
            if(settings == null)
                throw new InvalidOperationException("IPFS storage configuration is required before reading the private index.");
            var address=settings.LookUpIPFSAddress;
            if(string.IsNullOrWhiteSpace(address))
            {
                if(_idLookup.Count>0) throw new InvalidOperationException("IPFS lookup root is missing while a populated index is retained; refusing to replace it with an empty index.");
                return _idLookup;
            }
            if(IPFSClient == null) throw new InvalidOperationException("An active IPFS client is required to read the configured lookup root.");
            var json=UnprotectIndex(await LoadStringToJson(address));
            Dictionary<string,HolonResume> loaded;
            try { loaded=DeserializeLookup(json); }
            catch(JsonException cause) { throw new InvalidDataException("IPFS stored private index JSON is invalid.",cause); }
            _idLookup=loaded;
            return _idLookup;
        }

        private static Dictionary<string,HolonResume> DeserializeLookup(string json)
        {
            var token=Newtonsoft.Json.Linq.JToken.Parse(json,new Newtonsoft.Json.Linq.JsonLoadSettings {
                DuplicatePropertyNameHandling=Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error });
            if(token.Type==Newtonsoft.Json.Linq.JTokenType.Null) throw new InvalidDataException("IPFS lookup root contained no index object.");
            if(token is not Newtonsoft.Json.Linq.JObject) throw new JsonSerializationException("IPFS lookup root must be an object.");
            var records=token.ToObject<Dictionary<string,HolonResume>>();
            foreach(var entry in records)
            {
                var row=entry.Value;
                if(string.IsNullOrWhiteSpace(entry.Key) || row==null || row.Id==Guid.Empty
                    || row.RecordKind is not ("avatar" or "avatar-detail" or "holon")
                    || row.ProviderUniqueStorageKey==null
                    || !row.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.IPFSOASIS,out var cid) || cid!=entry.Key)
                    throw new InvalidDataException("IPFS index entry has invalid kind, identity or storage-key binding.");
            }
            if(records.Values.GroupBy(row=>(row.RecordKind,row.Id)).Any(group=>group.Count()>1))
                throw new StorageIdentityAmbiguousException();
            foreach(var cid in records.Keys)
            {
                try { _=(Cid)cid; }
                catch(Exception cause) { throw new InvalidDataException("IPFS index entry has an invalid CID.",cause); }
            }
            return records;
        }

        public async Task<string> SaveJsonToFile<T>(List<T> list)
        {
            string json = JsonConvert.SerializeObject(list);
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
            return (string) fsn.Id;
        }

        public async Task<string> SaveLookupToFile(Dictionary<string, HolonResume> idLookup)
        {
            var gate = CatalogueGate;
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                using var lease = await AcquireCatalogueFileLeaseAsync().ConfigureAwait(false);
                return await SaveCatalogueAsync(idLookup, ReadCatalogueMutationDna()).ConfigureAwait(false);
            }
            finally { gate.Release(); }
        }

        private async Task<string> SaveCatalogueAsync(Dictionary<string, HolonResume> idLookup, OASISDNA dna)
        {
            string json = ProtectIndex(JsonConvert.SerializeObject(idLookup));
            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);

            var settings = dna.OASIS.StorageProviders.IPFSOASIS;
            var previousRoot = settings.LookUpIPFSAddress;
            settings.LookUpIPFSAddress = fsn.Id;
            try
            {
                var saved=OASISDNAManager.SaveDNA(_OASISDNAPath, dna);
                EnsureIndexPublication(fsn.Id,previousRoot,saved);
            }
            catch (Exception cause)
            {
                settings.LookUpIPFSAddress = previousRoot;
                if (cause is IPFSIndexPublicationException) throw;
                throw new IPFSIndexPublicationException(fsn.Id, previousRoot, cause);
            }

            _OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress = fsn.Id;
            _idLookup = idLookup;
            return fsn.Id;
        }

        private static void EnsureIndexPublication(string cid,string previousRoot,OASISResult<bool> saved)
        {
            if(saved == null || saved.IsError || !saved.Result)
                throw new IPFSIndexPublicationException(cid,previousRoot,saved?.Exception
                    ?? new InvalidOperationException(saved?.Message ?? "DNA pointer persistence returned no result."));
        }

        private async Task PublishAcknowledgedRecordAsync(string recordCid, HolonResume record, OASISDNA dna)
        {
            var previousCatalogue = _idLookup;
            try
            {
                _idLookup = new Dictionary<string, HolonResume>(previousCatalogue);
                ReplaceIndexEntry(recordCid, record);
                await SaveCatalogueAsync(_idLookup, dna).ConfigureAwait(false);
            }
            catch (Exception cause)
            {
                _idLookup = previousCatalogue;
                throw new IPFSRecordPublicationException(recordCid, cause);
            }
        }

        private async Task SavePrivateEntityAsync(IHolonBase entity, string kind, HolonResume index,
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite plannedHolon=null,Action validateCaller=null)
        {
            // Keep catalogue read, native record acknowledgement and durable root publication together.
            var gate = CatalogueGate;
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                using var lease = await AcquireCatalogueFileLeaseAsync().ConfigureAwait(false);
                await PublishPrivateEntityAsync(entity, kind, index, plannedHolon, validateCaller).ConfigureAwait(false);
            }
            finally { gate.Release(); }
        }

        private async Task PublishPrivateEntityAsync(IHolonBase entity, string kind, HolonResume index,
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite plannedHolon, Action validateCaller)
        {
            if (entity == null || entity.Id == Guid.Empty) throw new ArgumentException("IPFS entity identity must be nonempty.");
            var dna = ReadCatalogueMutationDna();
            await LoadCatalogueAsync(dna).ConfigureAwait(false);
            var previous = FindIndexEntry(kind, row => row.Id == entity.Id, true);
            if(kind=="holon" && entity is Holon holon){
                var observed=previous.Value==null?null:await ReadPrivateRecordAsync<Holon>(previous.Key,kind,entity.Id);
                var write=plannedHolon??new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite(holon,holon);
                holon.ChildRoleManifestJson=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonChildRolePersistence.Resolve(write,
                    NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonChildRoleWriteKind.SourceSave,observed!=null,observed?.ChildRoleManifestJson);
            }
            entity.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            entity.PreviousVersionProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            if (previous.Value != null) entity.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = previous.Key;
            index.Id = entity.Id;
            index.RecordKind = kind;
            var json = ProtectRecord(kind, entity.Id, OasisJson.Serialize(entity));
            validateCaller?.Invoke();
            var acknowledged = await IPFSClient.FileSystem.AddTextAsync(json);
            var cid = acknowledged.Id.ToString();
            try
            {
                validateCaller?.Invoke();
                entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS] = cid;
                index.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType,string>(entity.ProviderUniqueStorageKey);
                await PublishAcknowledgedRecordAsync(cid, index, dna);
                validateCaller?.Invoke();
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

        public Task<IHolon> SaveHolonToFile(IHolon holon)=>SaveHolonRecordAsync(holon,null);

        private async Task<IHolon> SaveHolonRecordAsync(IHolon holon,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite plan,Action validateSource=null)
        {
            if (holon == null) throw new ArgumentNullException(nameof(holon));
            var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<Holon>.Capture(holon);
            var proposal=publication.Proposal;
            void Validate(){publication.ValidateUnchanged();validateSource?.Invoke();}
            await SavePrivateEntityAsync(proposal, "holon", new HolonResume {
                ParentHolonId=proposal.ParentHolonId, HolonType=proposal.HolonType },plan,Validate);
            holon.ProviderUniqueStorageKey=new Dictionary<Core.Enums.ProviderType,string>(proposal.ProviderUniqueStorageKey);
            holon.PreviousVersionProviderUniqueStorageKey=new Dictionary<Core.Enums.ProviderType,string>(proposal.PreviousVersionProviderUniqueStorageKey);
            if(holon is Holon caller)caller.ChildRoleManifestJson=proposal.ChildRoleManifestJson;
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
            return await SavePrivateResultAsync(()=>SaveAvatarToFile(avatar));
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result=new OASISResult<ISearchResults>();
            try
            {
                StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
                searchParams=Core.Serialization.StoredRecordJson.Project<SearchParams>(searchParams);
                StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
                var groups=searchParams.SearchGroups ?? new List<ISearchGroupBase>();
                var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
                var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
                var snapshot=await LoadLookupToJson();
                var avatars=wantsAvatars ? await ReadIndexSnapshotAsync<Avatar>(snapshot,"avatar",row=>true,version):new List<Avatar>();
                var holons=wantsHolons || searchParams.ParentId!=Guid.Empty
                    ? await ReadIndexSnapshotAsync<Holon>(snapshot,"holon",row=>true,version):new List<Holon>();
                var graphHolons=version!=0 && loadChildren && wantsHolons
                    ? await ReadIndexSnapshotAsync<Holon>(snapshot,"holon",row=>true,0):holons;
                var project=HolonGraphProjection.Create(graphHolons,loadChildren,recursive,maxChildDepth,0,true,
                    source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
                var found=new SearchResults();
                found.SearchResultAvatars.AddRange(avatars.Where(row=>StoredRecordSearch.Matches(row,true,searchParams,holons)));
                if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>StoredRecordSearch.Matches(row,false,searchParams,holons)).Select(project));
                found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
                result.Result=found; MarkIpfsLoaded(result,found.NumberOfResults);
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
