using System;
using System.IO;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Data;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using Google.Cloud.BigQuery.V2;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public partial class GoogleCloudOASIS
    {

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var search=searchParams;
        var children=loadChildren;
        var maxDepth=maxChildDepth;
        var result=new OASISResult<ISearchResults>();
        try {
            ValidateFirestoreVersion(version);
            StoredRecordSearch.Validate(search,version,maxDepth,supportsHistory:true);
            var groups=search.SearchGroups ?? new List<ISearchGroupBase>();
            var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
            var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
            var avatars=new List<IAvatar>();var holons=new List<IHolon>();
            if(wantsAvatars) {
                var loaded=await LoadAllAvatarsAsync(version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);
                avatars=loaded.Result.ToList();
            }
            if(wantsHolons || search.ParentId!=Guid.Empty) {
                var loaded=await LoadAllHolonsAsync(HolonType.All,children,recursive,maxDepth,0,continueOnError,true,version);
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
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
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

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => ExportCanonicalOwnedHolonsAsync(avatarId,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => ExportCanonicalSelectedAvatarAsync(avatarUsername,false,version);

    }
}
