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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

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
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await ReadProjectedHolonAsync(row => row.Id == id, loadChildren, recursive, maxChildDepth, loadChildrenFromProvider, version,id!=Guid.Empty);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await ReadProjectedHolonAsync(row => row.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.IPFSOASIS, out var key) && key == providerKey,
                loadChildren, recursive, maxChildDepth, loadChildrenFromProvider, version,!string.IsNullOrWhiteSpace(providerKey));
        }

        private async Task<OASISResult<IHolon>> ReadProjectedHolonAsync(Func<HolonResume,bool> select,
            bool loadChildren, bool recursive, int maxDepth, bool childrenFromProvider, int version,bool validSelector=true)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if(!validSelector) throw new ArgumentException("A nonempty IPFS holon selector is required.");
                if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
                if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
                var snapshot = await LoadLookupToJson();
                var matches = snapshot.Where(entry => entry.Value?.RecordKind == "holon" && select(entry.Value)).Take(2).ToList();
                if (matches.Count > 1) throw new StorageIdentityAmbiguousException();
                if (matches.Count == 0) return new OASISResult<IHolon> {
                    IsError=true, ErrorCount=1, ErrorCode=OASISResultErrorCodes.RecordNotFound,
                    Message="No IPFS holon matches the requested identity." };
                var selected = matches[0];
                var root = await ReadPrivateVersionAsync<Holon>(selected.Key, "holon", selected.Value.Id,version);
                if(root==null) return new OASISResult<IHolon> {IsError=true,ErrorCount=1,
                    ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="The requested IPFS holon version was not found."};
                var records = loadChildren && childrenFromProvider
                    ? await ReadIndexSnapshotAsync<Holon>(snapshot, "holon", row => true,0)
                    : new List<Holon> { root };
                var project = HolonGraphProjection.Create(records, loadChildren, recursive, maxDepth, 0, childrenFromProvider,
                    source => OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
                result.Result = project(root);
                result.IsLoaded = true;
            }
            catch (Exception cause)
            {
                OASISErrorHandling.HandleError(ref result, "IPFS holon read failed: " + cause.Message, cause);
                if (cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

        /*** Templates****/

        public async Task<OASISResult<IAvatar>> LoadAvatarTemplateAsync(Func<HolonResume,bool> predicate, int version=0, bool validSelector=true)
        {
            var read=await ReadIndexedEntityAsync<Avatar>("avatar",predicate,version,validSelector);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Avatar,IAvatar>(read);
            result.Result=read.Result; return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailTemplateAsync(Func<HolonResume,bool> predicate, int version=0, bool validSelector=true)
        {
            var read=await ReadIndexedEntityAsync<AvatarDetail>("avatar-detail",predicate,version,validSelector);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<AvatarDetail,IAvatarDetail>(read);
            result.Result=read.Result; return result;
        }

        private async Task<OASISResult<T>> ReadIndexedEntityAsync<T>(string kind,Func<HolonResume,bool> predicate,int version,bool validSelector) where T:class,IHolonBase
        {
            var result=new OASISResult<T>();
            try
            {
                if(!validSelector) throw new ArgumentException("A nonempty IPFS entity selector is required.");
                if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
                var snapshot=await LoadLookupToJson();
                var matches=snapshot.Where(entry=>entry.Value?.RecordKind==kind && predicate(entry.Value)).Take(2).ToList();
                if(matches.Count>1) throw new StorageIdentityAmbiguousException();
                if(matches.Count==0) return new OASISResult<T> {IsError=true,ErrorCount=1,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="No IPFS entity matches the requested kind and identity."};
                result.Result=await ReadPrivateVersionAsync<T>(matches[0].Key,kind,matches[0].Value.Id,version);
                if(result.Result==null) return new OASISResult<T> {IsError=true,ErrorCount=1,
                    ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="The requested IPFS entity version was not found."};
                result.IsLoaded=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,"IPFS entity read failed: "+cause.Message,cause);
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }


        public async Task<OASISResult<IHolon>> LoadHolonTemplateAsync(Func<HolonResume,bool> predicate)
        {
            _idLookup=await LoadLookupToJson();
            var selected=FindIndexEntry("holon",predicate);
            var record=await ReadPrivateRecordAsync<Holon>(selected.Key,"holon",selected.Value.Id);
            return new OASISResult<IHolon>(record) {IsLoaded=true};
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentTemplateAsync(Func<HolonResume,bool> predicate,int version=0)
        {
            var records=await ReadIndexedCollectionAsync<Holon>("holon",predicate,version);
            return new OASISResult<IEnumerable<IHolon>>(records) {IsLoaded=true};
        }
        /***********/

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (id == Guid.Empty) throw new ArgumentException("A parent identity is required.", nameof(id));
            if (maxChildDepth < 0 || curentChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
            var (records,graphRecords) = await ReadHolonProjectionSnapshotAsync(version,loadChildren,loadChildrenFromProvider);
            var project = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(
                graphRecords, loadChildren, recursive, maxChildDepth, curentChildDepth, loadChildrenFromProvider,
                source => OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            return new OASISResult<IEnumerable<IHolon>>(records
                .Where(row => row.ParentHolonId == id && (type == HolonType.All || row.HolonType == type))
                .Select(project).ToList()) { IsLoaded = true };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => DeleteIndexedAvatarAsync(row=>row.Id==id,softDelete,id!=Guid.Empty);

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => DeleteIndexedAvatarAsync(row=>row.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.IPFSOASIS,out var key) && key==providerKey,softDelete,!string.IsNullOrWhiteSpace(providerKey));

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
            => DeletePrivateHolonAsync(()=>LoadHolonAsync(id,loadChildren:false));

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => DeletePrivateHolonAsync(()=>LoadHolonAsync(providerKey,loadChildren:false));

        private async Task<OASISResult<IHolon>> DeletePrivateHolonAsync(Func<Task<OASISResult<IHolon>>> load)
        {
            var result=new OASISResult<IHolon>();
            try
            {
                var loaded=await load();
                if(loaded.IsError) return loaded;
                var holon=loaded.Result ?? throw new InvalidDataException("IPFS deletion read returned no entity.");
                if(holon.DeletedDate!=default && !holon.IsActive)
                    return new OASISResult<IHolon>(holon);
                holon.DeletedDate=DateTime.UtcNow;
                holon.DeletedByAvatarId=AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty;
                holon.IsActive=false;
                result.Result=await SaveHolonToFile(holon); result.IsSaved=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,"IPFS holon deletion failed: "+cause.Message,cause);
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)
        {
            var read=await ReadPrivateCollectionResultAsync<Avatar>("avatar",version);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<Avatar>,IEnumerable<IAvatar>>(read);
            result.Result=read.Result; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.Holon, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.Holon,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(maxChildDepth<0 || curentChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                var (records,graphRecords)=await ReadHolonProjectionSnapshotAsync(version,loadChildren,loadChildrenFromProvider);
                var project=HolonGraphProjection.Create(graphRecords,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,
                    source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
                result.Result=records.Where(row=>type==HolonType.All || row.HolonType==type).Select(project).ToList();
                result.IsLoaded=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,"IPFS holon collection failed: "+cause.Message,cause);
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

    }
}
