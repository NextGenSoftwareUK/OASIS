using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;
using Web3NFTTransactionResponse=NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class ProviderWalletDispatchTests
{
    private sealed class Fixture
    {
        internal readonly Guid AvatarId=Guid.NewGuid();
        internal readonly Mock<IOASISStorageProvider> Storage=new();
        internal readonly Mock<IOASISLocalStorageProvider> Local;
        internal readonly Mock<IOASISBlockchainStorageProvider> Chain=new();
        internal readonly Mock<IOASISNFTProvider> Nfts;
        internal readonly ProviderWallet Stored;
        internal readonly ProviderWallet Wallet;
        internal readonly ProviderManager Providers;
        internal readonly WalletManager Manager;
        internal readonly Dictionary<ProviderType,List<IProviderWallet>> Wallets;
        internal int Calls;
        internal ISendWeb3TokenRequest? TokenRequest;
        internal ISendWeb3NFTRequest? NftRequest;
        internal OASISResult<ITransactionResponse>? TokenOutcome=new(new TransactionResponse{TransactionResult="native-token-receipt"}){IsSaved=true};
        internal OASISResult<IWeb3NFTTransactionResponse>? NftOutcome=new(new Web3NFTTransactionResponse{TransactionResult="native-nft-receipt"}){IsSaved=true};
        internal Exception? SubmissionFailure;

        internal Fixture()
        {
            Local=Storage.As<IOASISLocalStorageProvider>();Nfts=Chain.As<IOASISNFTProvider>();
            Storage.SetupAllProperties();Storage.Object.ProviderType=new(ProviderType.SQLLiteDBOASIS);
            Storage.Object.ProviderCategory=new(ProviderCategory.StorageLocal);Storage.Object.IsProviderActivated=true;
            Storage.Object.ProviderName="bound-wallet-store";
            Storage.Setup(x=>x.ActivateProvider()).Returns(new OASISResult<bool>(true));
            Stored=new ProviderWallet{WalletId=Guid.NewGuid(),AvatarId=AvatarId,ProviderType=ProviderType.SuiOASIS,
                WalletAddress="sender",PublicKey="sender-public",PrivateKey=Rijndael.Encrypt("synthetic-bound-key",new string('k',32),KeySize.Aes256)};
            // A non-default wallet must not silently become the avatar's default wallet.
            var other=new ProviderWallet{WalletId=Guid.NewGuid(),AvatarId=AvatarId,ProviderType=ProviderType.SuiOASIS,
                WalletAddress="other-default",PublicKey="other-public",IsDefaultWallet=true,PrivateKey="unrelated-invalid-ciphertext"};
            Wallets=new Dictionary<ProviderType,List<IProviderWallet>>{[ProviderType.SuiOASIS]=new(){other,Stored}};
            Local.Setup(x=>x.LoadProviderWalletsForAvatarById(AvatarId)).Returns(()=>new(Wallets));
            Local.Setup(x=>x.LoadProviderWalletsForAvatarByIdAsync(AvatarId)).ReturnsAsync(()=>new(Wallets));
            Chain.SetupGet(x=>x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
            Chain.Setup(x=>x.SendToken(It.IsAny<ISendWeb3TokenRequest>())).Returns((ISendWeb3TokenRequest r)=>SubmitToken(r));
            Chain.Setup(x=>x.SendTokenAsync(It.IsAny<ISendWeb3TokenRequest>())).Returns((ISendWeb3TokenRequest r)=>Task.FromResult(SubmitToken(r)));
            Nfts.Setup(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>())).Returns((ISendWeb3NFTRequest r)=>SubmitNft(r));
            Nfts.Setup(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>())).Returns((ISendWeb3NFTRequest r)=>Task.FromResult(SubmitNft(r)));
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
            dna.OASIS.Security=new SecuritySettings{OASISProviderPrivateKeys=new EncryptionSettings
                {Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32)}};
            dna.OASIS.StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false};
            Providers=new ProviderManager(null,dna);Providers.RegisterProvider(Storage.Object);Providers.RegisterProvider(Chain.Object);
            Assert.False(Providers.SetAndActivateCurrentStorageProvider(Storage.Object).IsError);
            Manager=new WalletManager(null,dna,Providers);
            Wallet=new ProviderWallet(Manager){WalletId=Stored.WalletId,AvatarId=AvatarId,
                ProviderType=ProviderType.SuiOASIS,WalletAddress=Stored.WalletAddress,PublicKey=Stored.PublicKey};
        }
        private OASISResult<ITransactionResponse> SubmitToken(ISendWeb3TokenRequest r)
        {Calls++;TokenRequest=r;if(SubmissionFailure!=null)throw SubmissionFailure;return TokenOutcome!;}
        private OASISResult<IWeb3NFTTransactionResponse> SubmitNft(ISendWeb3NFTRequest r)
        {Calls++;NftRequest=r;if(SubmissionFailure!=null)throw SubmissionFailure;return NftOutcome!;}
    }

    private sealed record Observed(bool Error,string Code,Exception? Cause,object Container,string? Receipt);

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Web4SummaryCannotInventSuccessWithoutNativeReceipt(bool nft)
    {
        var f=new Fixture();
        if(nft)
        {
            f.NftOutcome!.Result.TransactionResult="";
            var result=await f.Manager.SendNFTAsync(f.AvatarId,new SendWeb4NFTRequest{FromProvider=new(ProviderType.SuiOASIS),ToProvider=new(ProviderType.SuiOASIS),
                FromWalletAddress="sender",ToWalletAddress="recipient",TokenAddress="collection",TokenId="7",Amount=1,OwnerPrivateKey="explicit-key"});
            Assert.True(result.IsError);Assert.True(string.IsNullOrWhiteSpace(result.Result?.SendTransactionResult));
        }
        else
        {
            f.TokenOutcome!.Result.TransactionResult="";
            var result=await f.Manager.SendTokenAsync(f.AvatarId,new SendWeb4TokenRequest{FromProvider=new(ProviderType.SuiOASIS),ToProvider=new(ProviderType.SuiOASIS),
                FromWalletAddress="sender",ToWalletAddress="recipient",Amount=1,OwnerPrivateKey="explicit-key"});
            Assert.True(result.IsError);Assert.True(string.IsNullOrWhiteSpace(result.Result?.SendTransactionResult));
        }
        Assert.Equal(1,f.Calls);
    }

    [Theory]
    [InlineData(false,"success")][InlineData(true,"success")]
    [InlineData(false,"warning")][InlineData(true,"warning")]
    [InlineData(false,"error")][InlineData(true,"error")]
    public async Task Web4SummaryRetainsOriginalNativeReceiptAndEnvelope(bool nft,string outcome)
    {
        var f=new Fixture();var cause=new IOException("original receipt-bearing native uncertainty");
        void Prepare<T>(OASISResult<T> result)
        {
            result.IsSaved=true;result.SavedCount=1;result.ResultsCount=1;
            result.IsError=outcome=="error";result.ErrorCount=result.IsError?1:0;
            result.IsWarning=outcome=="warning";result.WarningCount=result.IsWarning?3:0;
            result.ErrorCode=result.IsError?"ORIGINAL_NATIVE_UNCERTAINTY":"ORIGINAL_NATIVE_STATUS";
            result.Exception=result.IsError?cause:null;result.Message="original-native-status";
            result.DetailedMessage="original native reconciliation detail";
            result.MetaData=new(){["OriginalNativeId"]="original-operation-42"};
        }
        static void Check<TFrom,TTo>(OASISResult<TFrom> original,OASISResult<TTo> summary)
        {
            Assert.Equal(original.IsError,summary.IsError);Assert.Equal(original.ErrorCode,summary.ErrorCode);
            Assert.Equal(original.IsSaved,summary.IsSaved);Assert.Equal(original.SavedCount,summary.SavedCount);
            Assert.Equal(original.ResultsCount,summary.ResultsCount);Assert.Equal(original.ErrorCount,summary.ErrorCount);
            Assert.Equal(original.IsWarning,summary.IsWarning);Assert.Equal(original.WarningCount,summary.WarningCount);
            Assert.Same(original.Exception,summary.Exception);Assert.Equal(original.Message,summary.Message);
            Assert.Equal(original.DetailedMessage,summary.DetailedMessage);Assert.Same(original.MetaData,summary.MetaData);
        }
        if(nft)
        {
            Prepare(f.NftOutcome!);
            var result=await f.Manager.SendNFTAsync(f.AvatarId,new SendWeb4NFTRequest{FromProvider=new(ProviderType.SuiOASIS),ToProvider=new(ProviderType.SuiOASIS),
                FromWalletAddress="sender",ToWalletAddress="recipient",TokenAddress="collection",TokenId="7",Amount=1,OwnerPrivateKey="explicit-key"});
            Assert.Equal(f.NftOutcome!.Result.TransactionResult,result.Result?.SendTransactionResult);
            Check(f.NftOutcome,result);
        }
        else
        {
            Prepare(f.TokenOutcome!);
            var result=await f.Manager.SendTokenAsync(f.AvatarId,new SendWeb4TokenRequest{FromProvider=new(ProviderType.SuiOASIS),ToProvider=new(ProviderType.SuiOASIS),
                FromWalletAddress="sender",ToWalletAddress="recipient",Amount=1,OwnerPrivateKey="explicit-key"});
            Assert.Equal(f.TokenOutcome!.Result.TransactionResult,result.Result?.SendTransactionResult);
            Check(f.TokenOutcome,result);
        }
        Assert.Equal(1,f.Calls);Assert.Same(f.Storage.Object,f.Providers.CurrentStorageProvider);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task LoadedWalletRetainsItsOwningManagerInsteadOfGlobalRouting(bool synchronous)
    {
        var f=new Fixture();var actor=OASISRequestContext.CurrentAvatar;
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=f.AvatarId};
            var wallets=await f.Manager.LoadProviderWalletsForAvatarByIdAsync(f.AvatarId);
            Assert.False(wallets.IsError);
            var loaded=Assert.IsType<ProviderWallet>(wallets.Result[ProviderType.SuiOASIS].Single(x=>x.WalletId==f.Stored.WalletId));
            Assert.Same(f.Manager,typeof(ProviderWallet).GetField("_sendingManager",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(loaded));
            var request=new SendWeb3TokenRequest{ToWalletAddress="recipient",Amount=1};
            var result=synchronous?loaded.SendToken(request):await loaded.SendTokenAsync(request);
            Assert.Same(f.TokenOutcome,result);Assert.Equal(1,f.Calls);
            // Binding is only on the detached response; cached storage value stays untouched.
            Assert.Null(typeof(ProviderWallet).GetField("_sendingManager",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(f.Stored));
        }
        finally{OASISRequestContext.CurrentAvatar=actor;}
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task MutatingCallerWhileStorageIsPendingCannotRedirectTheSend(bool nft)
    {
        var f=new Fixture();var actor=OASISRequestContext.CurrentAvatar;
        var gate=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Local.Setup(x=>x.LoadProviderWalletsForAvatarByIdAsync(f.AvatarId)).Returns(async()=>
        {await gate.Task;return new OASISResult<Dictionary<ProviderType,List<IProviderWallet>>>(f.Wallets);});
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=f.AvatarId};
            if(nft)
            {
                var request=new SendWeb3NFTRequest{ToWalletAddress="original-recipient",Amount=1,TokenAddress="original-asset",TokenId="7"};
                var pending=f.Wallet.SendNFTAsync(request);Assert.Equal(0,f.Calls);
                request.ToWalletAddress="redirected";request.Amount=99;request.TokenId="999";
                f.Wallet.WalletAddress="changed";f.Wallet.ProviderType=ProviderType.EthereumOASIS;
                gate.SetResult(true);Assert.False((await pending).IsError);
                Assert.Equal("original-recipient",f.NftRequest!.ToWalletAddress);Assert.Equal(1,f.NftRequest.Amount);Assert.Equal("7",f.NftRequest.TokenId);
            }
            else
            {
                var request=new SendWeb3TokenRequest{ToWalletAddress="original-recipient",Amount=1,FromTokenAddress="original-asset"};
                var pending=f.Wallet.SendTokenAsync(request);Assert.Equal(0,f.Calls);
                request.ToWalletAddress="redirected";request.Amount=99;request.FromTokenAddress="changed";
                f.Wallet.WalletAddress="changed";f.Wallet.ProviderType=ProviderType.EthereumOASIS;
                gate.SetResult(true);Assert.False((await pending).IsError);
                Assert.Equal("original-recipient",f.TokenRequest!.ToWalletAddress);Assert.Equal(1,f.TokenRequest.Amount);Assert.Equal("original-asset",f.TokenRequest.FromTokenAddress);
            }
            Assert.Equal(1,f.Calls);
        }
        finally{gate.TrySetResult(true);OASISRequestContext.CurrentAvatar=actor;}
    }

    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task LegacyExplicitTokenKeyAliasIsPreservedAndConflictsRejected(bool sync,bool conflict)
    {
        var f=new Fixture();var actor=OASISRequestContext.CurrentAvatar;
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=f.AvatarId};
            var request=new SendWeb3TokenRequest{ToWalletAddress="recipient",Amount=1,FromWalletPrivateKey="explicit-alias",
                OwnerPrivateKey=conflict?"different-explicit-key":null};
            var result=sync?f.Wallet.SendToken(request):await f.Wallet.SendTokenAsync(request);
            Assert.Equal(conflict,result.IsError);Assert.Equal(conflict?0:1,f.Calls);
            if(!conflict)Assert.Equal("explicit-alias",f.TokenRequest!.OwnerPrivateKey);
            Assert.Equal("explicit-alias",request.FromWalletPrivateKey);
        }
        finally{OASISRequestContext.CurrentAvatar=actor;}
    }
    private static async Task<Observed> Send(Fixture f,bool nft,bool sync,decimal amount=1,string recipient="recipient",string? from=null,string? publicKey=null,string? key=null)
    {
        if(nft)
        {
            var request=new SendWeb3NFTRequest{FromWalletAddress=from,ToWalletAddress=recipient,Amount=amount,
                TokenAddress="collection",TokenId="7",OwnerPublicKey=publicKey,OwnerPrivateKey=key};
            var r=sync?f.Wallet.SendNFT(request):await f.Wallet.SendNFTAsync(request);
            return new(r.IsError,r.ErrorCode,r.Exception,r,r.Result?.TransactionResult);
        }
        var token=new SendWeb3TokenRequest{FromWalletAddress=from,ToWalletAddress=recipient,Amount=amount,OwnerPublicKey=publicKey,OwnerPrivateKey=key};
        var result=sync?f.Wallet.SendToken(token):await f.Wallet.SendTokenAsync(token);
        return new(result.IsError,result.ErrorCode,result.Exception,result,result.Result?.TransactionResult);
    }

    public static IEnumerable<object[]> Rejections()=>
        from kind in new[]{"foreign-actor","anonymous","provider","missing","stored-owner","stored-provider","bound-address",
            "request-address","request-public","zero","recipient","cipher","material","read-failure","duplicate"}
        from nft in new[]{false,true} from sync in new[]{false,true} select new object[]{kind,nft,sync};

    [Theory][MemberData(nameof(Rejections))]
    public async Task InvalidBindingCannotSubmitOrDecryptAnotherWallet(string kind,bool nft,bool sync)
    {
        var f=new Fixture();var actor=OASISRequestContext.CurrentAvatar;var actorId=OASISRequestContext.CurrentAvatarId;
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=f.AvatarId};OASISRequestContext.CurrentAvatarId=f.AvatarId;
            string? from=null,pub=null;decimal amount=1;var recipient="recipient";
            switch(kind)
            {
                case "foreign-actor":OASISRequestContext.CurrentAvatar=new Avatar{Id=Guid.NewGuid()};break;
                case "anonymous":OASISRequestContext.CurrentAvatar=new Avatar{Id=Guid.Empty};break;
                case "provider":f.Wallet.ProviderType=ProviderType.Default;break;
                case "missing":f.Stored.WalletId=Guid.NewGuid();break;
                case "stored-owner":f.Stored.AvatarId=Guid.NewGuid();break;
                case "stored-provider":f.Stored.ProviderType=ProviderType.EthereumOASIS;break;
                case "bound-address":f.Wallet.WalletAddress="different";break;
                case "request-address":from="different";break;
                case "request-public":pub="different";break;
                case "zero":amount=0;break;
                case "recipient":recipient="";break;
                case "cipher":f.Stored.PrivateKey="invalid-ciphertext";break;
                case "material":f.Stored.PrivateKey=null;break;
                case "duplicate":f.Wallets[ProviderType.SuiOASIS].Add(f.Stored);break;
                case "read-failure":
                    var failed=new OASISResult<Dictionary<ProviderType,List<IProviderWallet>>>{IsError=true,ErrorCode="ORIGINAL_WALLET_IO",Exception=new IOException("owned storage failure")};
                    f.Local.Setup(x=>x.LoadProviderWalletsForAvatarById(f.AvatarId)).Returns(failed);
                    f.Local.Setup(x=>x.LoadProviderWalletsForAvatarByIdAsync(f.AvatarId)).ReturnsAsync(failed);break;
            }
            var r=await Send(f,nft,sync,amount,recipient,from,pub);
            Assert.True(r.Error);Assert.Equal(0,f.Calls);Assert.Null(r.Receipt);
            if(kind is "foreign-actor" or "anonymous")
            {
                Assert.Equal("OASIS_PRIVATE_KEY_ACCESS_DENIED",r.Code);
                f.Local.Verify(x=>x.LoadProviderWalletsForAvatarById(f.AvatarId),Times.Never);
                f.Local.Verify(x=>x.LoadProviderWalletsForAvatarByIdAsync(f.AvatarId),Times.Never);
            }
            if(kind is "zero" or "recipient")
            {
                f.Local.Verify(x=>x.LoadProviderWalletsForAvatarById(f.AvatarId),Times.Never);
                f.Local.Verify(x=>x.LoadProviderWalletsForAvatarByIdAsync(f.AvatarId),Times.Never);
            }
            if(kind=="read-failure"){Assert.Equal("ORIGINAL_WALLET_IO",r.Code);Assert.IsType<IOException>(r.Cause);}
        }
        finally{OASISRequestContext.CurrentAvatar=actor;OASISRequestContext.CurrentAvatarId=actorId;}
    }

    public static IEnumerable<object[]> Outcomes()=>from kind in new[]{"success","error","null","empty","throw"}
        from nft in new[]{false,true} from sync in new[]{false,true} select new object[]{kind,nft,sync};

    [Theory][MemberData(nameof(Outcomes))]
    public async Task NativeOutcomeIsNeverRetriedOrTurnedIntoInventedReceipt(string kind,bool nft,bool sync)
    {
        var f=new Fixture();var actor=OASISRequestContext.CurrentAvatar;
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=f.AvatarId};var cause=new IOException("original native operation needs reconciliation");
            if(kind=="error")
            {
                f.TokenOutcome!.IsError=true;f.TokenOutcome.ErrorCode="ORIGINAL_NATIVE_ERROR";f.TokenOutcome.Exception=cause;
                f.NftOutcome!.IsError=true;f.NftOutcome.ErrorCode="ORIGINAL_NATIVE_ERROR";f.NftOutcome.Exception=cause;
            }
            if(kind=="null"){f.TokenOutcome=null;f.NftOutcome=null;}
            if(kind=="empty"){f.TokenOutcome!.Result.TransactionResult="";f.NftOutcome!.Result.TransactionResult="";}
            if(kind=="throw")f.SubmissionFailure=cause;
            var r=await Send(f,nft,sync);Assert.Equal(1,f.Calls);Assert.Equal(kind!="success",r.Error);
            if(kind is "success" or "error" or "empty")Assert.Same(nft?(object)f.NftOutcome! : f.TokenOutcome!,r.Container);
            if(kind=="error"){Assert.Equal("ORIGINAL_NATIVE_ERROR",r.Code);Assert.Same(cause,r.Cause);Assert.NotNull(r.Receipt);}
            if(kind=="throw"){Assert.Same(cause,r.Cause);Assert.Null(r.Receipt);}
            Assert.Same(f.Storage.Object,f.Providers.CurrentStorageProvider);
        }
        finally{OASISRequestContext.CurrentAvatar=actor;}
    }

    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task ExplicitSigningMaterialIsNotMixedWithStoredCiphertext(bool nft,bool sync)
    {
        var f=new Fixture();var actor=OASISRequestContext.CurrentAvatar;
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=f.AvatarId};f.Stored.PrivateKey="unreadable-stored-ciphertext";
            var result=await Send(f,nft,sync,key:"synthetic-explicit-key");Assert.False(result.Error);Assert.Equal(1,f.Calls);
            Assert.Equal("synthetic-explicit-key",nft?f.NftRequest!.OwnerPrivateKey:f.TokenRequest!.OwnerPrivateKey);
            Assert.Equal("unreadable-stored-ciphertext",f.Stored.PrivateKey);
        }
        finally{OASISRequestContext.CurrentAvatar=actor;}
    }

    [Theory][InlineData("error")][InlineData("null")][InlineData("empty")][InlineData("throw")]
    public async Task ExistingWeb4NftWrapperCannotReportSuccessAfterNativeFailure(string kind)
    {
        var f=new Fixture();var cause=new IOException("native NFT uncertainty");
        if(kind=="error"){f.NftOutcome!.IsError=true;f.NftOutcome.ErrorCode="ORIGINAL_NFT_ERROR";f.NftOutcome.Exception=cause;}
        if(kind=="null")f.NftOutcome=null;
        if(kind=="empty")f.NftOutcome!.Result.TransactionResult="";
        if(kind=="throw")f.SubmissionFailure=cause;
        var result=await f.Manager.SendNFTAsync(f.AvatarId,new SendWeb4NFTRequest{FromProvider=new(ProviderType.SuiOASIS),ToProvider=new(ProviderType.SuiOASIS),
            FromWalletAddress="sender",ToWalletAddress="recipient",TokenAddress="collection",TokenId="7",Amount=1,OwnerPrivateKey="explicit-key"});
        Assert.True(result.IsError);Assert.Equal(1,f.Calls);
        if(kind is "error" or "throw")Assert.Same(cause,result.Exception);
        if(kind=="error")Assert.Equal("ORIGINAL_NFT_ERROR",result.ErrorCode);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task PublicWalletMethodsSubmitExactBoundWalletOnce(bool nft,bool synchronous)
    {
        var fixture=new Fixture();var actor=OASISRequestContext.CurrentAvatar;
        try
        {
            OASISRequestContext.CurrentAvatar=new Avatar{Id=fixture.AvatarId};
            var ciphertext=fixture.Stored.PrivateKey;
            if(nft)
            {
                var request=new SendWeb3NFTRequest{FromNFTTokenAddress="collection/7",TokenAddress="collection",TokenId="7",
                    ToWalletAddress="recipient",Amount=1, MemoText="owned-memo"};
                var result=synchronous?fixture.Wallet.SendNFT(request):await fixture.Wallet.SendNFTAsync(request);
                Assert.Same(fixture.NftOutcome,result);Assert.NotSame(request,fixture.NftRequest);
                Assert.Equal("synthetic-bound-key",fixture.NftRequest!.OwnerPrivateKey);
                Assert.Equal("sender",fixture.NftRequest.FromWalletAddress);Assert.Equal("7",fixture.NftRequest.TokenId);
                Assert.True(string.IsNullOrEmpty(request.OwnerPrivateKey));Assert.Null(request.FromWalletAddress);
            }
            else
            {
                var request=new SendWeb3TokenRequest{ToWalletAddress="recipient",Amount=1,FromTokenAddress="asset",MemoText="owned-memo"};
                var result=synchronous?fixture.Wallet.SendToken(request):await fixture.Wallet.SendTokenAsync(request);
                Assert.Same(fixture.TokenOutcome,result);Assert.NotSame(request,fixture.TokenRequest);
                Assert.Equal("synthetic-bound-key",fixture.TokenRequest!.OwnerPrivateKey);
                Assert.Equal("sender",fixture.TokenRequest.FromWalletAddress);Assert.Equal("asset",fixture.TokenRequest.FromTokenAddress);
                Assert.Null(request.OwnerPrivateKey);Assert.Null(request.FromWalletAddress);
            }
            Assert.Equal(1,fixture.Calls);Assert.Equal(ciphertext,fixture.Stored.PrivateKey);Assert.Null(fixture.Wallet.PrivateKey);
            Assert.Same(fixture.Storage.Object,fixture.Providers.CurrentStorageProvider);
        }
        finally {OASISRequestContext.CurrentAvatar=actor;}
    }
}
