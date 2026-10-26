using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private const string NativeNftIdKey="polkadot:token-id";
    private const string NativeNftChainKey="polkadot:genesis-hash";
    private sealed record NftIntent(string Action,string Contract,string TokenId,string Sender,string To,
        string ExpectedFrom,string PublicKey,string PrivateKey,string Mnemonic,string[][] Attributes,
        string Title="",string Symbol="",string Description="",string ImageUrl="",bool DeliveredAtMint=false,Guid MinterAvatarId=default,string ContractAlias=null,string SourceCustodyTransaction=null)
    {
        public override string ToString()=>$"{Action} NFT intent";
    }

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> ExecuteOwnedNftAsync(Func<NativeTransferExecution,Task<NftIntent>> freeze)
    {
        NftIntent intent=null;
        var operation=await ExecuteOwnedMutationAsync(async owner=>{
            if(_nftConfiguration==null)throw new InvalidOperationException("Configure the reviewed PSP34 code hash before NFT mutations.");
            intent=await freeze(owner).ConfigureAwait(false);
            intent=await NormalizeNftIntentAccountsAsync(intent,owner.Token).ConfigureAwait(false);
            if(intent.SourceCustodyTransaction!=null)
                await VerifyNftCustodySourceAsync(intent,owner.Token).ConfigureAwait(false);
            return await PrepareNftIntentAsync(intent,owner).ConfigureAwait(false);
        },NativeMutationKind.Nft).ConfigureAwait(false);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.Wallet.Responses.ITransactionResponse,IWeb3NFTTransactionResponse>(operation);
        if(operation.IsError)return result;
        var nft=new Web3NFT { NFTTokenAddress=intent.Contract,CollectionPublicKey=intent.Contract,TokenId=intent.TokenId,
            Title=intent.Title,Symbol=intent.Symbol,Description=intent.Description,ImageUrl=intent.ImageUrl,
            OnChainProvider=new EnumValue<ProviderType>(Core.Enums.ProviderType.PolkadotOASIS),
            NFTStandardType=new EnumValue<NFTStandardType>(NFTStandardType.PSP34) };
        nft.MetaData[NativeNftChainKey]=ReportedGenesisHash;
        if(intent.Action=="mint") {
            nft.Id=Guid.NewGuid();nft.MintedOn=DateTime.UtcNow;
            nft.MintedByAvatarId=intent.MinterAvatarId;
            nft.MintTransactionHash=operation.Result.TransactionResult;
            nft.NFTMintedUsingWalletAddress=intent.Sender;
            if(intent.DeliveredAtMint) {
                // One actual mint-to-recipient event delivers atomically. Tell
                // NFTManager not to schedule a second transfer of the same NFT.
                nft.SendNFTTransactionHash=operation.Result.TransactionResult;
                nft.MetaData["polkadot:delivery-kind"]="mint-to-recipient";
            }
        }
        if(intent.Action=="transfer")nft.SendNFTTransactionHash=operation.Result.TransactionResult;
        result.Result=new Web3NFTTransactionResponse { TransactionResult=operation.Result.TransactionResult,Web3NFT=nft,
            Web3NFTs=new List<IWeb3NFT>{nft},SendNFTTransactionResult=intent.Action=="transfer"||intent.DeliveredAtMint?operation.Result.TransactionResult:null };
        result.IsSaved=true;
        return result;
    }

    private Task<OASISResult<IWeb3NFTTransactionResponse>> SendOwnedNftAsync(ISendWeb3NFTRequest request)
        =>ExecuteOwnedNftAsync(owner=>{
            ArgumentNullException.ThrowIfNull(request);
            var contract=NftContractAlias(request.FromNFTTokenAddress,request.TokenAddress);
            var id=CanonicalNftTokenId(request.TokenId);
            var from=NftAccount(request.FromWalletAddress);
            var to=NftAccount(request.ToWalletAddress);
            var signer=NftAccount(string.IsNullOrWhiteSpace(request.OwnerPublicKey)?from:request.OwnerPublicKey);
            if(request.Amount!=1)throw new ArgumentException("PSP34 transfer amount must be exactly one NFT.");
            if(!string.IsNullOrEmpty(request.MemoText))throw new ArgumentException("PSP34 transfer memo is not supported; it cannot be discarded.");
            return Task.FromResult(new NftIntent("transfer",contract,id,signer,to,from,
                request.OwnerPublicKey,request.OwnerPrivateKey,request.OwnerSeedPhrase,null,ContractAlias:request.TokenAddress));
        });

    private Task<OASISResult<IWeb3NFTTransactionResponse>> BurnOwnedNftAsync(IBurnWeb3NFTRequest request)
        =>ExecuteOwnedNftAsync(owner=>{
            ArgumentNullException.ThrowIfNull(request);
            if(request.BurntByAvatarId==Guid.Empty)throw new ArgumentException("An authenticated burner avatar is required.");
            var contract=NftAccount(request.NFTTokenAddress);
            var id=CanonicalNftTokenId(request.TokenId);
            var signer=NftAccount(request.OwnerPublicKey);
            return Task.FromResult(new NftIntent("burn",contract,id,signer,null,signer,
                request.OwnerPublicKey,request.OwnerPrivateKey,request.OwnerSeedPhrase,null));
        });

    private Task<OASISResult<IWeb3NFTTransactionResponse>> MintOwnedNftAsync(IMintWeb3NFTRequest request)
        =>ExecuteOwnedNftAsync(async owner=>{
            ArgumentNullException.ThrowIfNull(request);
            // Snapshot all caller-owned data before the wallet await.
            var avatar=request.MintedByAvatarId;
            var contract=NftAccount(request.CollectionPublicKey);
            var metadata=request.MetaData?.ToDictionary(pair=>pair.Key,pair=>pair.Value);
            var id=metadata?.TryGetValue(NativeNftIdKey,out var explicitId)==true?CanonicalNftTokenId(explicitId):NewNativeNftId();
            var to=string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)?null:request.SendToAddressAfterMinting;
            var title=request.Title??"";var symbol=request.Symbol??"";var description=request.Description??"";var image=request.ImageUrl??"";
            if(avatar==Guid.Empty)throw new ArgumentException("An authenticated minter avatar is required.");
            if((request.NumberToMint??1)!=1||(request.Amount!=0&&request.Amount!=1))
                throw new ArgumentException("This original mints one PSP34 token; use the existing NFT manager batch orchestration for multiple tokens.");
            if(request.OnChainProvider.HasValue&&request.OnChainProvider.Value!=Core.Enums.ProviderType.PolkadotOASIS)
                throw new ArgumentException("NFT mint provider conflicts with PolkadotOASIS.");
            if(request.NFTStandardType.HasValue&&request.NFTStandardType.Value!=NFTStandardType.PSP34)
                throw new ArgumentException("Polkadot NFT mint requires PSP34, not an EVM or SPL standard.");
            if(request.RoyaltyPercentage.GetValueOrDefault()!=0)
                throw new NotSupportedException("The reviewed PSP34 contract does not enforce royalty payments.");
            if(request.FreezeMetadata==false)throw new NotSupportedException("The reviewed PSP34 token metadata is immutable.");
            if(!string.IsNullOrEmpty(request.MemoText))throw new ArgumentException("PSP34 mint memo is not supported.");
            if((request.SendToAvatarAfterMintingId!=Guid.Empty||!string.IsNullOrEmpty(request.SendToAvatarAfterMintingUsername)
                ||!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))&&string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Resolve the recipient avatar through NFTManager before invoking the chain provider.");
            if(!string.IsNullOrWhiteSpace(to))to=NftAccount(to);
            if(request.Image?.Length>0&&image.Length==0)throw new ArgumentException("Upload NFT image bytes through the off-chain provider and supply ImageUrl.");
            var attributes=new List<string[]>();
            void Attribute(string key,string value) {
                if(Encoding.UTF8.GetByteCount(value)>8192)throw new ArgumentException("PSP34 metadata field exceeds8192UTF8bytes.");
                attributes.Add(new[]{Convert.ToBase64String(Encoding.UTF8.GetBytes(key)),Convert.ToBase64String(Encoding.UTF8.GetBytes(value))});
            }
            Attribute("name",title);Attribute("symbol",symbol);Attribute("description",description);Attribute("image",image);
            if(!string.IsNullOrEmpty(request.JSONMetaDataURL))Attribute("metadata_uri",request.JSONMetaDataURL);
            if(!string.IsNullOrEmpty(request.JSONMetaData)) {
                using var json=JsonDocument.Parse(request.JSONMetaData);
                if(request.StoreNFTMetaDataOnChain==false&&string.IsNullOrEmpty(request.JSONMetaDataURL))
                    throw new ArgumentException("Off-chain JSON metadata requires its uploaded metadata URL.");
                if(request.StoreNFTMetaDataOnChain!=false)Attribute("metadata",request.JSONMetaData);
            }
            if(request.Tags?.Count>0)Attribute("tags",JsonSerializer.Serialize(request.Tags.ToArray()));
            if(metadata!=null) {
                metadata.Remove(NativeNftIdKey);
                if(metadata.Count>0) {
                    if(request.StoreNFTMetaDataOnChain==false&&string.IsNullOrEmpty(request.JSONMetaDataURL))
                        throw new ArgumentException("Off-chain custom metadata requires its uploaded metadata URL.");
                    if(request.StoreNFTMetaDataOnChain!=false)Attribute("oasis_metadata",JsonSerializer.Serialize(metadata));
                }
            }
            var wallet=await ResolveAssetMintWalletAsync(avatar).ConfigureAwait(false);
            owner.Token.ThrowIfCancellationRequested();
            if(wallet.IsError||wallet.Result==null)throw wallet.Exception??new InvalidOperationException(wallet.Message);
            if(wallet.Result.AvatarId!=avatar||wallet.Result.ProviderType!=Core.Enums.ProviderType.PolkadotOASIS)
                throw new InvalidOperationException("Mint wallet does not belong to the requested avatar/provider.");
            var sender=NftAccount(wallet.Result.WalletAddress);
            return new NftIntent("mint",contract,id,sender,to??sender,null,wallet.Result.PublicKey,wallet.Result.PrivateKey,null,
                attributes.ToArray(),title,symbol,description,image,to!=null,avatar);
        });

    private async Task<OASISResult<JsonElement>> PrepareNftIntentAsync(NftIntent intent,NativeTransferExecution owner)
    {
        var result=new OASISResult<JsonElement>();
        try {
            if(_nftConfiguration==null)throw new InvalidOperationException("Configure the reviewed PSP34 code hash before NFT mutations.");
            using var token=JsonDocument.Parse(intent.TokenId);
            var input=new Dictionary<string,object> { ["operation"]="prepareNftMutation",["endpoint"]=_rpcEndpoint,
                ["genesisHash"]=ReportedGenesisHash,["action"]=intent.Action,["contractAddress"]=intent.Contract,
                ["contractCodeHash"]=_nftConfiguration.CodeHash,["tokenId"]=token.RootElement.Clone(),
                ["sender"]=intent.Sender,["recipient"]=intent.Contract,["publicKey"]=intent.PublicKey,
                ["privateKey"]=intent.PrivateKey,["mnemonic"]=intent.Mnemonic };
            if(intent.To!=null)input["to"]=intent.To;
            if(intent.ExpectedFrom!=null)input["expectedFrom"]=intent.ExpectedFrom;
            if(intent.Attributes!=null)input["attributes"]=intent.Attributes;
            var original=await InvokePolkadotSdkAsync(input,owner.Token).ConfigureAwait(false);
            // Bind the SDK response to the frozen API intent BEFORE any durable ACK.
            if(original.GetProperty("action").GetString()!=intent.Action||original.GetProperty("contractAddress").GetString()!=intent.Contract
                ||original.GetProperty("sender").GetString()!=intent.Sender||original.GetProperty("contractCodeHash").GetString()!=_nftConfiguration.CodeHash
                ||!JsonElement.DeepEquals(original.GetProperty("tokenId"),token.RootElement)
                ||(intent.To!=null&&original.GetProperty("to").GetString()!=intent.To)
                ||(intent.Attributes!=null&&!JsonElement.DeepEquals(original.GetProperty("attributes"),JsonSerializer.SerializeToElement(intent.Attributes)))
                ||(intent.ExpectedFrom!=null&&original.GetProperty("expectedFrom").GetString()!=intent.ExpectedFrom))
                throw new InvalidDataException("Prepared NFT original differs from the frozen API intent.");
            result.Result=original;
        } catch(Exception failure){OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }

    private static void ValidateFinalizedNftEffect(JsonElement original,JsonElement receipt)
    {
        var action=original.GetProperty("action").GetString();
        if(!receipt.GetProperty("nftEffectMatchesRequest").GetBoolean()||receipt.GetProperty("action").GetString()!=action
            ||receipt.GetProperty("contractAddress").GetString()!=original.GetProperty("contractAddress").GetString()
            ||!JsonElement.DeepEquals(receipt.GetProperty("tokenId"),original.GetProperty("tokenId"))
            ||(action=="burn"?receipt.GetProperty("to").ValueKind!=JsonValueKind.Null:
                receipt.GetProperty("to").GetString()!=original.GetProperty("to").GetString())
            ||(action=="mint"?receipt.GetProperty("from").ValueKind!=JsonValueKind.Null:
                receipt.GetProperty("from").GetString()!=original.GetProperty("expectedFrom").GetString()))
            throw new InvalidDataException("Finalized NFT effect differs from the original action/token/from/to; reconcile, never replay.");
    }

    private static string NftContractAlias(string first,string second)
    {
        return NftAccount(string.IsNullOrWhiteSpace(first)?second:first);
    }
    private static string NftAccount(string value)=>IsGenesisHash(value)?value.ToLowerInvariant():
        value!=null&&Regex.IsMatch(value,"^[1-9A-HJ-NP-Za-km-z]{46,50}$",RegexOptions.CultureInvariant)?value:
        throw new ArgumentException("Explicit AccountId32 hex or SS58 NFT identity required.");

    private async Task<NftIntent> NormalizeNftIntentAccountsAsync(NftIntent intent,CancellationToken token)
    {
        var values=new[]{intent.Contract,intent.Sender,intent.To,intent.ExpectedFrom,intent.ContractAlias};
        var present=values.Where(value=>!string.IsNullOrWhiteSpace(value)).Select(NftAccount).ToArray();
        string[] canonical;
        if(present.All(IsGenesisHash))canonical=present;
        else {
            var decoded=await InvokePolkadotSdkAsync(new { operation="canonicalAccountIdentities",addresses=present },token).ConfigureAwait(false);
            if(decoded.ValueKind!=JsonValueKind.Array||decoded.GetArrayLength()!=present.Length)
                throw new InvalidDataException("SDK account identity count differs from the frozen intent.");
            canonical=decoded.EnumerateArray().Select(value=>value.GetString()).ToArray();
            if(canonical.Any(value=>!IsGenesisHash(value)))throw new InvalidDataException("SDK returned a non-AccountId32 identity.");
        }
        var index=0;
        string Next(string original)=>string.IsNullOrWhiteSpace(original)?null:canonical[index++].ToLowerInvariant();
        var contract=Next(intent.Contract);var sender=Next(intent.Sender);var to=Next(intent.To);
        var from=Next(intent.ExpectedFrom);var alias=Next(intent.ContractAlias);
        if(alias!=null&&alias!=contract)throw new ArgumentException("Conflicting NFT collection contract addresses.");
        return intent with { Contract=contract,Sender=sender,To=to,ExpectedFrom=from,ContractAlias=alias };
    }
    private async Task VerifyNftCustodySourceAsync(NftIntent intent,CancellationToken token)
    {
        if(!IsGenesisHash(intent.SourceCustodyTransaction))throw new ArgumentException("Exact local finalized custody transaction hash required.");
        using var id=JsonDocument.Parse(intent.TokenId);
        var proof=await InvokePolkadotSdkAsync(new { operation="nftOutcome",custodyReceipt=true,endpoint=_rpcEndpoint,
            genesisHash=ReportedGenesisHash,transactionHash=intent.SourceCustodyTransaction.ToLowerInvariant(),
            contractAddress=intent.Contract,contractCodeHash=_nftConfiguration.CodeHash,
            tokenId=id.RootElement.Clone(),custody=intent.Sender },token).ConfigureAwait(false);
        if(!proof.GetProperty("custodyReceiptVerified").GetBoolean()||!proof.GetProperty("finalized").GetBoolean()
            ||!proof.GetProperty("dispatchSucceeded").GetBoolean()||!proof.GetProperty("nftEffectMatchesRequest").GetBoolean()
            ||proof.GetProperty("transactionHash").GetString()!=intent.SourceCustodyTransaction.ToLowerInvariant()
            ||proof.GetProperty("contractAddress").GetString()!=intent.Contract
            ||proof.GetProperty("contractCodeHash").GetString()!=_nftConfiguration.CodeHash
            ||proof.GetProperty("custody").GetString()!=intent.Sender||proof.GetProperty("to").GetString()!=intent.Sender
            ||!JsonElement.DeepEquals(proof.GetProperty("tokenId"),id.RootElement))
            throw new InvalidDataException("Source NFT custody proof differs from the frozen release intent.");
    }

    private static string NewNativeNftId()=>JsonSerializer.Serialize(new Dictionary<string,string>{["U128"]=
        new BigInteger(RandomNumberGenerator.GetBytes(16),isUnsigned:true,isBigEndian:true).ToString(CultureInfo.InvariantCulture)});
    private static string CanonicalNftTokenId(string value)
    {
        using var document=JsonDocument.Parse(value??throw new ArgumentNullException(nameof(value)));
        var root=document.RootElement;
        if(root.ValueKind!=JsonValueKind.Object)throw new ArgumentException("NFT token ID must declare its native PSP34 variant.");
        var fields=root.EnumerateObject().ToArray();
        if(fields.Length!=1||fields[0].Value.ValueKind!=JsonValueKind.String)throw new ArgumentException("Exactly one typed native token ID required.");
        var variant=fields[0].Name;var id=fields[0].Value.GetString();
        if(variant=="Bytes") {
            if(id.Length>2050||!Regex.IsMatch(id,"^0x(?:[0-9a-f]{2})*$",RegexOptions.CultureInvariant))throw new ArgumentException("Canonical byte token ID required.");
        } else {
            var bits=variant switch{"U8"=>8,"U16"=>16,"U32"=>32,"U64"=>64,"U128"=>128,_=>0};
            if(bits==0||id.Length>39||!Regex.IsMatch(id,"^(0|[1-9][0-9]*)$",RegexOptions.CultureInvariant)
                ||!BigInteger.TryParse(id,NumberStyles.None,CultureInfo.InvariantCulture,out var number)||number>=(BigInteger.One<<bits))
                throw new ArgumentException("Canonical unsigned native token ID required.");
        }
        return JsonSerializer.Serialize(new Dictionary<string,string>{[variant]=id});
    }

    /// <summary>Read-only reconstruction from the durable owner's exact signed bytes.
    /// Does not sign/submit or invent the original API's unsigned owner precondition.</summary>
    public async Task<OASISResult<JsonElement>> RecoverNftPreparationAsync(UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new OASISResult<JsonElement>();
        try {
            if(!_isActivated||_nftConfiguration==null)throw new InvalidOperationException("Activate the pinned network and configure reviewed NFT code before recovery.");
            var snapshot=NativePreparedAttemptSnapshot.Capture(attempt,"PolkadotOASIS",ReportedGenesisHash);
            var original=new { chainId=snapshot.ChainId,sender=snapshot.Sender,transactionHash=snapshot.ProviderRequestId,
                signedTransactionBase64=snapshot.SignedTransactionBase64,signDocBase64=snapshot.SignDocBase64,signature=snapshot.Signature };
            var recovered=await InvokePolkadotSdkAsync(new { operation="recoverNftPreparation",endpoint=_rpcEndpoint,
                genesisHash=ReportedGenesisHash,contractCodeHash=_nftConfiguration.CodeHash,preparation=original },token).ConfigureAwait(false);
            if(recovered.GetProperty("transactionHash").GetString()!=snapshot.ProviderRequestId
                ||recovered.GetProperty("sender").GetString()!=snapshot.Sender||recovered.GetProperty("chainId").GetString()!=snapshot.ChainId
                ||!recovered.GetProperty("recoveredFromSignedOriginal").GetBoolean()
                ||(recovered.GetProperty("sourceOwnerPreconditionRetained").GetBoolean()
                    &&!IsGenesisHash(recovered.GetProperty("expectedFrom").GetString())))
                throw new InvalidDataException("Recovered NFT intent does not bind the retained original identity and recovery scope.");
            result.Result=recovered;
            result.Message="NFT native intent reconstructed from retained signed bytes; read actual finalized effects, never replay. Only a verified atomic source-owner witness preserves that API precondition.";
        } catch(Exception failure){OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
}
