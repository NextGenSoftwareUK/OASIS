using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class HederaTokenIntentTests
{
    // Pause only lookup scheduling; actual storage read and detached decryption remain production code.
    private sealed class GatedWalletStore(string root) : LocalFileOASIS(filePath: root + Path.DirectorySeparatorChar, storageDirectory: root), IOASISLocalStorageProvider
    {
        internal readonly TaskCompletionSource Reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<OASISResult<Dictionary<ProviderType,List<IProviderWallet>>>> IOASISLocalStorageProvider.LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            Reached.TrySetResult(); await Resume.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return await base.LoadProviderWalletsForAvatarByIdAsync(id);
        }
    }

    [Theory]
    [InlineData(false,false,"hedera")] [InlineData(true,false,"hedera")] [InlineData(false,true,"hedera")] [InlineData(true,true,"hedera")]
    [InlineData(false,false,"cosmos")] [InlineData(true,false,"cosmos")] [InlineData(false,true,"cosmos")] [InlineData(true,true,"cosmos")]
    [InlineData(false,false,"cosmos-nft")] [InlineData(true,false,"cosmos-nft")]
    [InlineData(false,false,"sui-nft")] [InlineData(true,false,"sui-nft")]
    [InlineData(false,false,"hedera-nft")] [InlineData(true,false,"hedera-nft")]
    public async Task MintBurnCaptureOriginalIntentBeforeEncryptedWalletAwait(bool synchronous,bool burn,string chain)
    {
        var cosmos=chain.StartsWith("cosmos",StringComparison.Ordinal);
        var sui=chain.StartsWith("sui",StringComparison.Ordinal);
        var nft=chain.EndsWith("-nft",StringComparison.Ordinal);
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
        Assert.NotNull(directory);
        var root=Path.Combine(directory!.FullName,"TestResults/HederaCurrent/wallet-failure-files");Directory.CreateDirectory(root);
        using var lease=new FileStream(Path.Combine(root,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var avatar=Guid.Parse("478e4ef2-b442-459f-93e0-fec53c7de2f1");
        var bridgeVariable=cosmos?"OASIS_COSMOS_SDK_BRIDGE":sui?"OASIS_SUI_SDK_BRIDGE":"OASIS_HEDERA_SDK_BRIDGE";
        var previous=Environment.GetEnvironmentVariable(bridgeVariable);
        var walletProvider=cosmos?ProviderType.CosmosBlockChainOASIS:sui?ProviderType.SuiOASIS:ProviderType.HashgraphOASIS;
        var local=new GatedWalletStore(root);var saved=false;
        Task<OASISResult<ITransactionResponse>> pending=null!;
        var encrypted=Rijndael256.Rijndael.Encrypt("test-only-explicit",new string('k',32),Rijndael256.KeySize.Aes256);
        try{
            Assert.False((await local.ActivateProviderAsync()).IsError);
            var prior=await local.LoadProviderWalletsForAvatarByIdAsync(avatar);
            Assert.False(prior.IsError);Assert.True(prior.Result==null||prior.Result.Count==0,"Preserve existing wallet fixture.");
            Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(avatar,new(){[walletProvider]=new(){
                new ProviderWallet{Id=Guid.NewGuid(),AvatarId=avatar,ProviderType=walletProvider,IsDefaultWallet=true,
                    WalletAddress=cosmos||sui?"fixture-owner":"0.0.1001",PrivateKey=encrypted,PublicKey="fixture-public"}}})).IsError);saved=true;
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
            dna.OASIS.StorageProviders=new(){LogSwitchingProviders=false};
            dna.OASIS.Security=new(){OASISProviderPrivateKeys=new(){Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32)}};
            var providers=new ProviderManager(null,dna);providers.RegisterProvider(local);Assert.False(providers.SetAndActivateCurrentStorageProvider(local).IsError);
            var ctor=typeof(WalletManager).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single(c=>c.GetParameters().Length==4);
            var wallets=(WalletManager)ctor.Invoke(new object?[]{local,dna,providers,null});
            var provider=new HashgraphOASIS(walletManager:wallets);
            Environment.SetEnvironmentVariable(bridgeVariable,Path.Combine(directory.FullName,cosmos?
                "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests/Fixtures/nft-signer.mjs":sui?
                "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests/Fixtures/unlock-readback-failure.mjs":
                "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests/Fixtures/prepared.mjs"));
            using var cosmosProvider=new Providers.CosmosBlockChainOASIS.CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain",contractAddress:"fixture-contract"){WalletManager=wallets};
            using var suiProvider=new Providers.SuiOASIS.SuiOASIS("http://127.0.0.1:1","localnet",contractAddress:"0x1"){WalletManager=wallets};
            var mint=new MintWeb3TokenRequest{MintedByAvatarId=avatar,Amount=1.23m,Symbol="intent-original",Title="original-title",MemoText="Original seed 🌱 memo",
                MetaData=new(){{"TokenAddress","0.0.3001"},{"MintToWalletAddress","0.0.1001"}}};
            if(cosmos)mint.MetaData=new(){{"canary","original"}};
            var burning=new BurnWeb3TokenRequest{BurntByAvatarId=avatar,TokenAddress=cosmos?"intent-original":"0.0.3001",Amount=1.23m,
                OwnerPrivateKey=cosmos?"":"test-only-explicit",OwnerPublicKey="fixture-public",OwnerSeedPhrase=""};
            var nftRequest=new Core.Objects.NFT.Requests.MintWeb3NFTRequest {MintedByAvatarId=avatar,Title=sui?"identity/wallet":"NFT original",
                Price=12.5m,Discount=2m,RoyaltyPercentage=7,MemoText="mint memo",NumberToMint=1,SendToAddressAfterMinting="fixture-owner",
                MetaData=new(){{"canary","original"}},Tags=new(){"original"},Image=new byte[]{1,2},Thumbnail=new byte[]{3,4}};
            if(nft&&!cosmos&&!sui){nftRequest.CollectionPublicKey="0.0.3001";nftRequest.RoyaltyPercentage=0;
                nftRequest.JSONMetaDataURL="https://example.invalid/original.json";nftRequest.SendToAddressAfterMinting="0.0.1001";}
            OASISResult<ITransactionResponse> MapNft(OASISResult<Core.Interfaces.NFT.Responses.IWeb3NFTTransactionResponse> value){
                var mapped=Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.NFT.Responses.IWeb3NFTTransactionResponse,ITransactionResponse>(value);
                mapped.Result=value.Result;return mapped;
            }
            OASISResult<ITransactionResponse> Run()=>nft?MapNft(sui?suiProvider.MintNFT(nftRequest):cosmos?cosmosProvider.MintNFT(nftRequest):provider.MintNFT(nftRequest)):
                cosmos?burn?cosmosProvider.BurnToken(burning):cosmosProvider.MintToken(mint):burn?provider.BurnToken(burning):provider.MintToken(mint);
            async Task<OASISResult<ITransactionResponse>> RunAsync()=>nft?MapNft(await(sui?suiProvider.MintNFTAsync(nftRequest):cosmos?cosmosProvider.MintNFTAsync(nftRequest):provider.MintNFTAsync(nftRequest))):
                await(cosmos?burn?cosmosProvider.BurnTokenAsync(burning):cosmosProvider.MintTokenAsync(mint):burn?provider.BurnTokenAsync(burning):provider.MintTokenAsync(mint));
            pending=synchronous?Task.Run(Run):RunAsync();
            await local.Reached.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.False(pending.IsCompleted);
            mint.Amount=99;mint.MintedByAvatarId=Guid.NewGuid();mint.MemoText="changed";
            mint.Symbol="intent-mutated";mint.Title="changed";
            if(cosmos)mint.MetaData["canary"]="changed";
            mint.MetaData["TokenAddress"]="0.0.9999";mint.MetaData["MintToWalletAddress"]="0.0.9999";
            burning.Amount=99;burning.TokenAddress="0.0.9999";burning.BurntByAvatarId=Guid.NewGuid();
            nftRequest.Title="changed";nftRequest.Price=99;nftRequest.RoyaltyPercentage=99;nftRequest.MintedByAvatarId=Guid.NewGuid();
            nftRequest.MetaData["canary"]="changed";nftRequest.Tags[0]="changed";nftRequest.Image[0]=99;nftRequest.Thumbnail[0]=99;
            nftRequest.CollectionPublicKey="changed-contract";nftRequest.SendToAddressAfterMinting="changed-recipient";nftRequest.MemoText="changed";
            local.Resume.TrySetResult();var result=await pending;
            Assert.False(result.IsError,result.Message);
            if(cosmos)Assert.Equal(new string('a',64),result.Result.TransactionResult);else if(sui)Assert.Equal(new string('1',32),result.Result.TransactionResult);else Assert.StartsWith("0.0.1001@",result.Result.TransactionResult);
            if(nft){
                var minted=((Core.Interfaces.NFT.Responses.IWeb3NFTTransactionResponse)result.Result).Web3NFT;
                Assert.Equal(sui?"identity/wallet":"NFT original",minted.Title);Assert.Equal(12.5m,minted.Price);Assert.Equal(cosmos||sui?7:0,minted.RoyaltyPercentage);
                Assert.Equal("original",minted.MetaData["canary"]);Assert.Equal("original",minted.Tags[0]);
                Assert.Equal((byte)1,minted.Image[0]);Assert.Equal((byte)3,minted.Thumbnail[0]);
                Assert.Equal(cosmos||sui?"fixture-owner":"0.0.1001",minted.SendToAddressAfterMinting);
            }
            Assert.Equal(encrypted,(await local.LoadProviderWalletsForAvatarByIdAsync(avatar)).Result[walletProvider][0].PrivateKey);
            Assert.False(provider.IsProviderActivated);
            Assert.False(cosmosProvider.IsProviderActivated);
            Assert.False(suiProvider.IsProviderActivated);
        }finally{
            local.Resume.TrySetResult();
            try { if(pending!=null)await pending; }
            finally {
                Environment.SetEnvironmentVariable(bridgeVariable,previous);
                if(saved){
                    Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(avatar,new())).IsError);
                    var owned=Path.GetFullPath(Path.Combine(root,"wallets_"+avatar+".json"));
                    Assert.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,owned,StringComparison.OrdinalIgnoreCase);
                    Assert.Equal("{}",(await File.ReadAllTextAsync(owned)).Trim());File.Delete(owned);
                }
                await local.DeActivateProviderAsync();
            }
        }
    }
}
