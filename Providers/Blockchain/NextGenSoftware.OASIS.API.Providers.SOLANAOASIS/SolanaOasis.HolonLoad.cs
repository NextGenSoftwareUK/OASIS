using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using System.Linq;
using System.IO;
using System.Text;
using static Solnet.Programs.TokenProgram;
using static Solnet.Programs.AssociatedTokenAccountProgram;
using static Solnet.Programs.SystemProgram;
using static Solnet.Programs.MemoProgram;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
        => ReadHolonCollectionAsync(row=>row.ParentHolonId==id && (type==HolonType.All || row.HolonType==type),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load holons for parent by provider key from Solana blockchain
            // First, load the parent holon by provider key
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading parent holon by provider key: {parentResult.Message}");
                return result;
            }

            // Then load children for the parent
            var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (childrenResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading child holons: {childrenResult.Message}");
                return result;
            }

            result.Result = childrenResult.Result;
            result.IsError = false;
            result.Message = $"Successfully loaded {childrenResult.Result?.Count() ?? 0} holons for parent by provider key from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        => ReadHolonCollectionAsync(row=>type==HolonType.All || row.HolonType==type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,
        int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
    {
        var saved=await SavePlannedSolanaGraphAsync(new[] {holon},saveChildren,recursive,maxChildDepth,0,continueOnError);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(saved);
        result.Result=saved.Result?.Single();
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0,
        bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError)
            .Result;
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
        bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int currentChildDepth=0,
        bool continueOnError=true,bool saveChildrenOnProvider=false)
        => SavePlannedSolanaGraphAsync(holons,saveChildren,recursive,maxChildDepth,currentChildDepth,continueOnError);

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
    }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result=new OASISResult<ISearchResults>();
            try
            {
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth);
                var groups=searchParams.SearchGroups ?? new List<ISearchGroupBase>();
                var wantsAvatars=groups.Count == 0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
                var wantsHolons=groups.Count == 0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
                var avatars=new List<IAvatar>();
                var holons=new List<IHolon>();
                if(wantsAvatars)
                {
                    var loaded=await LoadAllAvatarsAsync(version);
                    if(loaded.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result,loaded.Message,loaded.Exception);
                        result.ErrorCode=loaded.ErrorCode; return result;
                    }
                    avatars=loaded.Result.ToList();
                }
                if(wantsHolons || searchParams.ParentId != Guid.Empty)
                {
                    var loaded=await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,0,continueOnError,false,version);
                    if(loaded.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result,loaded.Message,loaded.Exception);
                        result.ErrorCode=loaded.ErrorCode; return result;
                    }
                    holons=loaded.Result.ToList();
                }
                var found=new SearchResults();
                found.SearchResultAvatars.AddRange(avatars.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,true,searchParams,holons)));
                if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,false,searchParams,holons)));
                found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
                result.Result=found; result.IsLoaded=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,$"Solana native search failed: {cause.Message}",cause);
            }
            return result;
        }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

}
