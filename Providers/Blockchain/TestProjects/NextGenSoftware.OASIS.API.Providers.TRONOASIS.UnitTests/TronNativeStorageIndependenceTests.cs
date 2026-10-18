using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNativeStorageIndependenceTests
{
    private static string Address(string key)=>new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress();
    private sealed class NativeOnly(TronNativeTransferClient client,bool throwActivation,WalletManager? manager=null)
        :TRONOASIS(nativeTransfers:client,walletManager:manager,
            bridgePoolWalletAddress:Address(new string('2',64)),escrowContractAddress:Address(new string('3',64)),
            nativeSigningKeyResolver:_=>Task.FromResult(new string('2',64)))
    {
        public int StorageActivations;
        public override Task<OASISResult<bool>> ActivateProviderAsync()
        {
            StorageActivations++;
            return throwActivation?Task.FromException<OASISResult<bool>>(new IOException("Unrelated storage activation must not run."))
                :Task.FromResult(new OASISResult<bool>{IsError=true,Message="Unrelated storage activation must not run."});
        }
    }
    private sealed class WalletRecords(IAvatar avatar):TRONOASIS
    {
        public override Task<OASISResult<bool>> ActivateProviderAsync()
        {IsProviderActivated=true;return Task.FromResult(new OASISResult<bool>(true));}
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id,int version=0)
            =>Task.FromResult(new OASISResult<IAvatar>{Result=id==avatar.Id?avatar:null!,IsLoaded=id==avatar.Id});
    }
    private static (TronNativeTransferClient Client,TronNftBurnSafetyTests.StopAtWallet Wallet,TronNftBurnSafetyTests.NoTransactions Transactions) Boundary()
    {
        var wallet=DispatchProxy.Create<TronNet.IWalletClient,TronNftBurnSafetyTests.StopAtWallet>();
        var transactions=DispatchProxy.Create<TronNet.ITransactionClient,TronNftBurnSafetyTests.NoTransactions>();
        return(new(wallet,transactions,new string('a',64)),(TronNftBurnSafetyTests.StopAtWallet)(object)wallet,
            (TronNftBurnSafetyTests.NoTransactions)(object)transactions);
    }

    [Theory]
    [InlineData("withdraw",false)] [InlineData("withdraw",true)]
    [InlineData("deposit",false)] [InlineData("deposit",true)]
    [InlineData("nft-deposit",false)] [InlineData("nft-deposit",true)]
    public async Task NativeBridgeDoesNotActivateUnrelatedEncryptedStorage(string operation,bool throwActivation)
    {
        var boundary=Boundary();var provider=new NativeOnly(boundary.Client,throwActivation);
        var other=Address(new string('1',64));
        var result=operation switch{
            "withdraw"=>await provider.WithdrawAsync(1m,other,new string('1',64)),
            "deposit"=>await provider.DepositAsync(1m,other),
            _=>await provider.DepositNFTAsync(Address(new string('4',64)),"1",other)};
        Assert.Equal(0,provider.StorageActivations);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Same(boundary.Wallet.Cause,result.Exception);
        Assert.Equal(0,boundary.Transactions.Calls);Assert.False(provider.IsProviderActivated);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(true,true)]
    public async Task AvatarOwnedNftEscrowUsesWalletManagerWithoutNativeStorageActivation(bool unlock,bool synchronous)
    {
        var avatarId=Guid.NewGuid();var privateKey=new string('1',64);var encryptionKey=new string('k',32);
        var encrypted=Rijndael256.Rijndael.Encrypt(privateKey,encryptionKey,Rijndael256.KeySize.Aes256);
        var avatar=new Avatar{Id=avatarId,IsActive=true,ProviderWallets=new(){[ProviderType.TRONOASIS]=new(){
            new Core.Objects.ProviderWallet{Id=Guid.NewGuid(),AvatarId=avatarId,ProviderType=ProviderType.TRONOASIS,
                IsDefaultWallet=true,WalletAddress=Address(privateKey),PrivateKey=encrypted}}}};
        var storage=new WalletRecords(avatar);
        var dna=new API.DNA.OASISDNA();dna.OASIS.HyperDriveMode=API.DNA.HyperDriveModes.V2;
        dna.OASIS.StorageProviders=new(){LogSwitchingProviders=false};
        dna.OASIS.Security=new(){OASISProviderPrivateKeys=new(){Rijndael256EncryptionEnabled=true,Rijndael256Key=encryptionKey}};
        var providers=new ProviderManager(null,dna);providers.RegisterProvider(storage);
        Assert.False(providers.SetAndActivateCurrentStorageProvider(storage).IsError);
        var ctor=typeof(WalletManager).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single(c=>c.GetParameters().Length==4);
        var manager=(WalletManager)ctor.Invoke(new object?[]{storage,dna,providers,null});
        var resolved=await manager.GetAvatarDefaultWalletByIdAsync(avatarId,ProviderType.TRONOASIS,showPrivateKeys:true);
        Assert.False(resolved.IsError,resolved.Message);Assert.NotNull(resolved.Result);Assert.Equal(privateKey,resolved.Result.PrivateKey);
        var boundary=Boundary();var provider=new NativeOnly(boundary.Client,false,manager);
        var nftAddress=Address(new string('4',64))+"/1";
        var result=unlock?synchronous?provider.UnlockNFT(new Core.Objects.NFT.Requests.UnlockWeb3NFTRequest{UnlockedByAvatarId=avatarId,NFTTokenAddress=nftAddress})
            :await provider.UnlockNFTAsync(new Core.Objects.NFT.Requests.UnlockWeb3NFTRequest{UnlockedByAvatarId=avatarId,NFTTokenAddress=nftAddress})
            :synchronous?provider.LockNFT(new Core.Objects.NFT.Requests.LockWeb3NFTRequest{LockedByAvatarId=avatarId,NFTTokenAddress=nftAddress})
            :await provider.LockNFTAsync(new Core.Objects.NFT.Requests.LockWeb3NFTRequest{LockedByAvatarId=avatarId,NFTTokenAddress=nftAddress});
        Assert.Equal(0,provider.StorageActivations);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Same(boundary.Wallet.Cause,result.Exception);
        Assert.Equal(encrypted,avatar.ProviderWallets[ProviderType.TRONOASIS][0].PrivateKey);
        Assert.Equal(0,boundary.Transactions.Calls);Assert.False(provider.IsProviderActivated);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);
        Assert.False((await storage.DeActivateProviderAsync()).IsError);
    }
}
