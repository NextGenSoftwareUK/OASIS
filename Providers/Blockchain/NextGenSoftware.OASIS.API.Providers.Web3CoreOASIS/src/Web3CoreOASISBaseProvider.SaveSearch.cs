using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public partial class Web3CoreOASISBaseProvider
{
    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        => SavePrivateProfileAsync<IAvatar>(avatar,"avatar",row=>OasisJson.Deserialize<Avatar>(OasisJson.Serialize(row)),
            (id,key,payload)=>SavePrivateProfileSnapshotAsync("avatar",id,key,payload));

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        => SaveAvatarDetailAsync(avatarDetail).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        => SavePrivateProfileAsync<IAvatarDetail>(avatarDetail,"detail",row=>OasisJson.Deserialize<AvatarDetail>(OasisJson.Serialize(row)),
            (id,key,payload)=>SavePrivateProfileSnapshotAsync("detail",id,key,payload));

    private async Task<(TransactionReceipt Receipt,IHolonBase Record)> SavePrivateProfileSnapshotAsync(string kind,uint id,byte[] external,byte[] payload)
    {
        var createHistory=IsVersionControlEnabled;
        var incoming=new EntityOASIS {EntityId=id,ExternalId=external,Info=payload};
        IHolonBase desired=kind=="avatar" ? DecodePrivateEntity<Avatar>(kind,incoming):DecodePrivateEntity<AvatarDetail>(kind,incoming);
        IHolonBase committed=null;
        var receipt=await _web3CoreOASIS.UpsertPreparedAsync(kind,id,external,async previous=>{
            if(!IsPrivateRecordAbsent(previous)){
                IHolonBase stored=kind=="avatar" ? DecodePrivateRecord<Avatar>(kind,desired.Id,previous):DecodePrivateRecord<AvatarDetail>(kind,desired.Id,previous);
                if(!stored.IsActive)throw new StorageLifecycleConflictException("Normal Web3Core saves cannot overwrite a committed inactive profile; use an explicit lifecycle operation.");
            }
            if(kind=="avatar"){
                var staged=await StagePrivateHistoryAsync(kind,(Avatar)desired,previous,createHistory);committed=staged.Record;return staged.Info;
            }
            var detail=await StagePrivateHistoryAsync(kind,(AvatarDetail)desired,previous,createHistory);committed=detail.Record;return detail.Info;
        });
        return (receipt,committed);
    }

    private async Task<OASISResult<T>> SavePrivateProfileAsync<T>(T source,string kind,Func<T,T> clone,
        Func<uint,byte[],byte[],Task<(TransactionReceipt Receipt,IHolonBase Record)>> submit) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        Guid acknowledged=Guid.Empty;string transaction=null;
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            var publication=StoredRecordPublicationSnapshot<T>.Capture(source);
            var originalId=source.Id;
            var snapshot=clone(source);
            snapshot.Id=originalId==Guid.Empty ? Guid.NewGuid():originalId;
            snapshot.IsNewHolon=false;
            snapshot.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
            snapshot.ProviderUniqueStorageKey[ProviderType.Value]=snapshot.Id.ToString();
            var payload=Encoding.UTF8.GetBytes(EncodePrivateRecord(kind,snapshot));
            if(_web3CoreOASIS==null) throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            var committed=await submit((uint)HashUtility.GetNumericHash(snapshot.Id.ToString()),PrivateExternalId(snapshot.Id),payload);
            acknowledged=snapshot.Id;transaction=committed.Receipt.TransactionHash;
            publication.ValidateUnchanged();
            source.Id=snapshot.Id;
            source.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
            source.ProviderUniqueStorageKey[ProviderType.Value]=snapshot.Id.ToString();source.IsNewHolon=false;
            source.Version=committed.Record.Version;source.VersionId=committed.Record.VersionId;source.PreviousVersionId=committed.Record.PreviousVersionId;
            result.Result=source;result.IsSaved=true;
        }
        catch(Exception cause)
        {
            var error=acknowledged==Guid.Empty ? cause:new Web3PartialMutationException(new[] {acknowledged},new[] {transaction},cause);
            OASISErrorHandling.HandleError(ref result,error.Message,error);
            if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        =>SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
    {
        var batch=await SaveHolonsAsync(new[] {holon},saveChildren,recursive,maxChildDepth,0,continueOnError,saveChildrenOnProvider);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(batch);
        if(!batch.IsError) result.Result=holon;
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        =>SaveHolonsAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,saveChildrenOnProvider).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        var acknowledged=new List<IHolon>();var transactions=new List<string>();
        try
        {
            var plan=PlanWeb3Graph(holons,saveChildren,recursive,maxChildDepth,curentChildDepth);
            var createHistory=IsVersionControlEnabled;
            // Freeze and encrypt the entire selected graph before any native write.
            var writes=plan.Nodes.Select(node=>
            {
                node.Snapshot.IsNewHolon=false;
                node.Snapshot.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
                node.Snapshot.ProviderUniqueStorageKey[ProviderType.Value]=node.Snapshot.Id.ToString();
                _=EncodePrivateRecord("holon",node.Snapshot);
                return node;
            }).ToList();
            if(_web3CoreOASIS==null) throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            foreach(var node in writes)
            {
                Holon committed=null;
                var receipt=await _web3CoreOASIS.UpsertPreparedAsync("holon",(uint)HashUtility.GetNumericHash(node.Snapshot.Id.ToString()),PrivateExternalId(node.Snapshot.Id),async previous=>{
                    var staged=await StagePrivateHistoryAsync("holon",node.Snapshot,previous,createHistory,node.Write);committed=staged.Record;return staged.Info;
                });
                acknowledged.Add(committed);transactions.Add(receipt.TransactionHash);
                node.Publication.ValidateUnchanged();
                node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                node.Source.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
                node.Source.ProviderUniqueStorageKey[ProviderType.Value]=node.Snapshot.Id.ToString();node.Source.IsNewHolon=false;
                node.Source.Version=committed.Version;node.Source.VersionId=committed.VersionId;node.Source.PreviousVersionId=committed.PreviousVersionId;
                if(node.Source is Holon source)source.ChildRoleManifestJson=committed.ChildRoleManifestJson;
            }
            result.Result=plan.Roots;result.IsSaved=true;
        }
        catch(Exception cause)
        {
            var error=acknowledged.Count>0 ? new Web3PartialMutationException(acknowledged.Select(row=>row.Id),transactions,cause):cause;
            result.Result=acknowledged;result.IsSaved=false;OASISErrorHandling.HandleError(ref result,error.Message,error);
        }
        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();
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

    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            TransactionReceipt transactionResult = await _web3CoreOASIS.SendTransactionAsync(toWalletAddress, amount);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Web3CoreOASIS transaction performing failed! " +
                                 $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {amount}." +
                                 $"Reason: {transactionResult.Logs}");
                OASISErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        => SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        OASISResult<IProviderWallet> senderAvatarPrivateKeysResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
        OASISResult<IProviderWallet> receiverAvatarAddressesResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByEmailAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        // Load avatars by email to get their wallet addresses
        var fromAvatarResult = await LoadAvatarByEmailAsync(fromAvatarEmail);
        var toAvatarResult = await LoadAvatarByEmailAsync(toAvatarEmail);
        
        if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load sender avatar: {fromAvatarResult.Message}"));
            return result;
        }
        
        if (toAvatarResult.IsError || toAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load receiver avatar: {toAvatarResult.Message}"));
            return result;
        }
        
        // Get wallet addresses from avatars
        var fromWallet = fromAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? fromAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault() 
            : null;
        var toWallet = toAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? toAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault() 
            : null;
        
        if (fromWallet == null || toWallet == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Wallet addresses not found for avatars"));
            return result;
        }
        
        var fromWalletAddress = fromWallet.WalletAddress ?? fromWallet.PublicKey;
        var toWalletAddress = toWallet.WalletAddress ?? toWallet.PublicKey;
        
        if (string.IsNullOrWhiteSpace(fromWalletAddress) || string.IsNullOrWhiteSpace(toWalletAddress))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Wallet addresses not found for avatars"));
            return result;
        }
        
        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarResult.Result.Username, this.ProviderType.Value);
        if (senderAvatarPrivateKeysResult.IsError || senderAvatarPrivateKeysResult.Result == null || senderAvatarPrivateKeysResult.Result.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key"));
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, toWalletAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByEmailAsync (token) in Web3CoreOASIS. Reason: ";
        try
        {
            var fromAvatarResult = await LoadAvatarByEmailAsync(fromAvatarEmail);
            var toAvatarResult = await LoadAvatarByEmailAsync(toAvatarEmail);
            if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Could not load sender avatar: {fromAvatarResult.Message}")); return result; }
            if (toAvatarResult.IsError || toAvatarResult.Result == null)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Could not load receiver avatar: {toAvatarResult.Message}")); return result; }

            var privateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarResult.Result.Username, this.ProviderType.Value);
            var toAddressResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarResult.Result.Username, this.ProviderType.Value);
            if (privateKeysResult.IsError || privateKeysResult.Result == null || privateKeysResult.Result.Count == 0)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key")); return result; }
            if (toAddressResult.IsError || toAddressResult.Result == null || toAddressResult.Result.Count == 0)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get receiver wallet address")); return result; }

            result = await SendTokenAsync(new SendWeb3TokenRequest
            {
                FromWalletPrivateKey = privateKeysResult.Result[0],
                ToWalletAddress = toAddressResult.Result[0],
                FromTokenAddress = token,
                Amount = amount
            });
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByIdAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        // Load avatars by ID to get their wallet addresses
        var fromAvatarResult = await LoadAvatarAsync(fromAvatarId);
        var toAvatarResult = await LoadAvatarAsync(toAvatarId);
        
        if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load sender avatar: {fromAvatarResult.Message}"));
            return result;
        }
        
        if (toAvatarResult.IsError || toAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load receiver avatar: {toAvatarResult.Message}"));
            return result;
        }
        
        // Get wallet addresses from avatars
        var fromWalletAddress = fromAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? fromAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault()?.PublicKey 
            : null;
        var toWalletAddress = toAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? toAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault()?.PublicKey 
            : null;
        
        if (string.IsNullOrWhiteSpace(fromWalletAddress) || string.IsNullOrWhiteSpace(toWalletAddress))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Wallet addresses not found for avatars"));
            return result;
        }
        
        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarResult.Result.Username, this.ProviderType.Value);
        if (senderAvatarPrivateKeysResult.IsError || senderAvatarPrivateKeysResult.Result == null || senderAvatarPrivateKeysResult.Result.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key"));
            return result;
        }
        
        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, toWalletAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByIdAsync (token) in Web3CoreOASIS. Reason: ";
        try
        {
            var privateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarById(fromAvatarId, this.ProviderType.Value);
            var toAddressResult = KeyManager.Instance.GetProviderPublicKeysForAvatarById(toAvatarId, this.ProviderType.Value);
            if (privateKeysResult.IsError || privateKeysResult.Result == null || privateKeysResult.Result.Count == 0)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key")); return result; }
            if (toAddressResult.IsError || toAddressResult.Result == null || toAddressResult.Result.Count == 0)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get receiver wallet address")); return result; }

            result = await SendTokenAsync(new SendWeb3TokenRequest
            {
                FromWalletPrivateKey = privateKeysResult.Result[0],
                ToWalletAddress = toAddressResult.Result[0],
                FromTokenAddress = token,
                Amount = amount
            });
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByUsernameAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, this.ProviderType.Value);
        OASISResult<List<string>> receiverAvatarAddressesResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, this.ProviderType.Value);

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        string receiverWalletAddress = receiverAvatarAddressesResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, receiverWalletAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByUsernameAsync (token) in Web3CoreOASIS. Reason: ";
        try
        {
            var privateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, this.ProviderType.Value);
            var toAddressResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, this.ProviderType.Value);
            if (privateKeysResult.IsError || privateKeysResult.Result == null || privateKeysResult.Result.Count == 0)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key")); return result; }
            if (toAddressResult.IsError || toAddressResult.Result == null || toAddressResult.Result.Count == 0)
            { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get receiver wallet address")); return result; }

            result = await SendTokenAsync(new SendWeb3TokenRequest
            {
                FromWalletPrivateKey = privateKeysResult.Result[0],
                ToWalletAddress = toAddressResult.Result[0],
                FromTokenAddress = token,
                Amount = amount
            });
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex); }
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendTransactionBaseAsync(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionBaseAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            Web3CoreOASIS web3CoreInstance = new(senderAccountPrivateKey, _hostURI, _contractAddress, Web3CoreOASISBaseProviderHelper.Abi);
            TransactionReceipt receipt = await web3CoreInstance.SendTransactionAsync(receiverAccountAddress, amount);

            if (receipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receipt.Logs));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

}
