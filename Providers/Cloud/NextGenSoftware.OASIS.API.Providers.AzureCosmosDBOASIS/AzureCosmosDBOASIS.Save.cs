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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    public partial class AzureCosmosDBOASIS
    {
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
            => await SaveCurrentRecordAsync<IAvatar>(avatar,()=>avatarRepository);

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
            => SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

        public async override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
            => await SaveCurrentRecordAsync<IAvatarDetail>(avatarDetail,()=>avatarDetailRepository);

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(holon);
                if(maxChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if(saveChildren)
                {
                    var graph=await SaveHolonsAsync(new[] {holon},true,recursive,maxChildDepth,0,continueOnError,saveChildrenOnProvider);
                    var converted=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(graph);
                    if(!graph.IsError) converted.Result=holon;
                    return converted;
                }
                return await SaveCurrentRecordAsync<IHolon>(holon,()=>holonRepository);
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon> { IsSaved = false, IsError = true, Message = ex.Message, Exception = ex };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var failures=new List<Exception>();
            try {
                var sourceGuards=new Dictionary<IHolon,StoredRecordPublicationSnapshot<IHolon>>(ReferenceEqualityComparer.Instance);
                var plan=PlanCosmosGraph(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,
                    source=>sourceGuards.Add(source,StoredRecordPublicationSnapshot<IHolon>.Capture(source)));
                var failedParents=new HashSet<Guid>();
                foreach(var node in plan.Nodes) {
                    var publication=sourceGuards[node.Source];publication.ValidateUnchanged();
                    if(failedParents.Contains(node.Snapshot.ParentHolonId)) {
                        failedParents.Add(node.Snapshot.Id);
                        failures.Add(new InvalidOperationException("Cosmos DB descendant skipped because its parent failed."));
                        continue;
                    }
                    var written=await SaveHolonAsync(node.Snapshot,false,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider);
                    if(written.IsError || written.Result==null) {
                        failedParents.Add(node.Snapshot.Id);
                        if(!result.IsError) result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(written);
                        result.IsError=true;
                        failures.Add(written.Exception ?? new InvalidOperationException(written.Message ?? "Cosmos DB item returned no acknowledgement."));
                        if(!continueOnError || NativeMutationReconciliation.IsRequired(written.Exception)) break;
                    }
                    else {
                        saved.Add(written.Result);
                        publication.ValidateUnchanged();
                        node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                        node.Source.Version=written.Result.Version;node.Source.VersionId=written.Result.VersionId;
                        node.Source.PreviousVersionId=written.Result.PreviousVersionId;
                        node.Source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                        node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=node.Snapshot.Id.ToString();
                    }
                }
                result.Result=failures.Count==0 ? plan.Roots:saved;result.IsSaved=failures.Count==0;
                if(failures.Count>0) {
                    result.IsError=true;
                    var cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
                    result.Exception=saved.Count>0 ? new CosmosPartialMutationException(saved.Select(row=>row.Id),cause):cause;
                }
            }
            catch(Exception cause) {
                result.Exception=saved.Count>0 ? new CosmosPartialMutationException(saved.Select(row=>row.Id),cause):cause;
                result.IsSaved=false;OASISErrorHandling.HandleError(ref result,cause.Message);
            }
            return result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var search=searchParams;
        var children=loadChildren;
        var maxDepth=maxChildDepth;
        var result=new OASISResult<ISearchResults>();
        try {
            StoredRecordSearch.Validate(search,version,maxDepth,supportsHistory:true);
            search=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<SearchParams>(search);
            StoredRecordSearch.Validate(search,version,maxDepth,supportsHistory:true);
            var groups=search.SearchGroups ?? new List<ISearchGroupBase>();
            var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
            var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
            var avatars=new List<IAvatar>();var holons=new List<IHolon>();
            if(wantsAvatars) {
                var loaded=await LoadAllAvatarsAsync(version).ConfigureAwait(false);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);
                avatars=loaded.Result.ToList();
            }
            if(wantsHolons || search.ParentId!=Guid.Empty) {
                var loaded=await LoadAllHolonsAsync(HolonType.All,children,recursive,maxDepth,0,continueOnError,true,version).ConfigureAwait(false);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(loaded);
                holons=loaded.Result.ToList();
            }
            var found=new SearchResults();
            found.SearchResultAvatars.AddRange(avatars.Where(row=>StoredRecordSearch.Matches(row,true,search,holons)));
            if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>StoredRecordSearch.Matches(row,false,search,holons)));
            found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
            result.Result=found;result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved=await SaveHolonsAsync(holons,continueOnError:false);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved);
            result.Result=!saved.IsError && saved.IsSaved;
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => await ExportCurrentOwnedAsync(avatarId,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportCurrentOwnedAsync(avatarId,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await ExportCurrentSelectorAsync(avatarUsername,false,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportCurrentSelectorAsync(avatarUsername,false,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => await ExportCurrentSelectorAsync(avatarEmailAddress,true,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportCurrentSelectorAsync(avatarEmailAddress,true,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await ReadCurrentHolonsAsync(HolonType.All,true,true,0,0,true,version).ConfigureAwait(false);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ReadCurrentHolonsAsync(HolonType.All,true,true,0,0,true,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentMetadataAsync(metaKey==null ? null : new Dictionary<string,string> {{metaKey,metaValue}},MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).ConfigureAwait(false);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentMetadataAsync(metaKey==null ? null : new Dictionary<string,string> {{metaKey,metaValue}},MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).ConfigureAwait(false);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();
    }
}
