using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;
using Rijndael256;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class HederaEscrowProcessTests
{
    [Theory]
    [InlineData(false,false,false)] [InlineData(true,false,false)] [InlineData(false,true,false)] [InlineData(true,true,false)]
    [InlineData(false,false,true)] [InlineData(true,false,true)] [InlineData(false,true,true)] [InlineData(true,true,true)]
    public async Task EncryptedWalletPublicEscrowUsesRealSdkProcess(bool synchronous,bool release,bool viaManager)
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
        Assert.NotNull(directory);var repository=directory!.FullName;
        var bridge=Path.Combine(repository,"Providers","Blockchain","NextGenSoftware.OASIS.API.Providers.HashgraphOASIS","SdkBridge");
        var walletRoot=Path.Combine(repository,"TestResults","HederaCurrent","wallet-failure-files");Directory.CreateDirectory(walletRoot);
        using var lease=new FileStream(Path.Combine(walletRoot,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var avatar=Guid.Parse("478e4ef2-b442-459f-93e0-fec53c7de2f1");
        var walletFile=Path.Combine(walletRoot,"wallets_"+avatar+".json");Assert.False(File.Exists(walletFile),"Preserve existing fixture before retry.");
        var start=new ProcessStartInfo(Environment.GetEnvironmentVariable("OASIS_NODE_PATH")??"node"){
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=bridge};
        start.ArgumentList.Add(Path.Combine(bridge,"id-bound-execution.test.mjs"));start.ArgumentList.Add(release?"--serve-csharp-release":"--serve-csharp-lock");
        using var fixture=new Process{StartInfo=start};Assert.True(fixture.Start());
        var errors=fixture.StandardError.ReadToEndAsync();var priorBridge=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        var local=new LocalFileOASIS(filePath:walletRoot+Path.DirectorySeparatorChar,storageDirectory:walletRoot);
        var completed=false;
        try{
            using var ready=JsonDocument.Parse((await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)))!);
            var config=ready.RootElement;
            Assert.False((await local.ActivateProviderAsync()).IsError);
            var prior=await local.LoadProviderWalletsForAvatarByIdAsync(avatar);Assert.False(prior.IsError);Assert.True(prior.Result==null||prior.Result.Count==0);
            var encrypted=Rijndael256.Rijndael.Encrypt(config.GetProperty("privateKey").GetString(),new string('k',32),KeySize.Aes256);
            var wallet=new ProviderWallet{Id=Guid.NewGuid(),AvatarId=avatar,ProviderType=ProviderType.HashgraphOASIS,IsDefaultWallet=true,
                WalletAddress="0.0.1001",PrivateKey=encrypted,PublicKey=config.GetProperty("publicKey").GetString()};
            Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(avatar,new(){[ProviderType.HashgraphOASIS]=new List<IProviderWallet>{wallet}})).IsError);
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
            dna.OASIS.StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false};
            dna.OASIS.Security=new SecuritySettings{OASISProviderPrivateKeys=new EncryptionSettings{Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32)}};
            var providers=new ProviderManager(null,dna);Assert.True(providers.RegisterProvider(local));
            Assert.False(providers.SetAndActivateCurrentStorageProvider(local).IsError);
            var constructor=typeof(WalletManager).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single(c=>c.GetParameters().Length==4);
            var wallets=(WalletManager)constructor.Invoke(new object?[]{local,dna,providers,null});
            var settings=new HashgraphOASISProviderSettings{TokenEscrowProtocol="id-bound-v1",TokenEscrowContractId="0.0.2001",
                TokenEscrowRuntimeCodeHash=config.GetProperty("runtimeCodeHash").GetString(),TokenEscrowGasLimit=400000,TokenEscrowMaxFeeTinybars=100000000};
            var hedera=new HashgraphOASIS(rpcEndpoint:$"http://127.0.0.1:{config.GetProperty("mirrorPort").GetInt32()}",network:"local",walletManager:wallets,
                consensusNodes:new Dictionary<string,string>{[$"127.0.0.1:{config.GetProperty("port").GetInt32()}"]="0.0.3"},tokenEscrowConfiguration:HederaEscrowConfiguration.Create(settings));
            Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(bridge,"bridge.mjs"));
            if(viaManager)Assert.True(providers.RegisterProvider(hedera));
            var id=Guid.Parse("5f31afde-1229-4e99-8088-a0034d33a557");
            var locked=new Core.Objects.Wallet.Requests.LockWeb4TokenRequest{LockedByAvatarId=avatar,LockId=id,TokenAddress="0.0.3001",EscrowContractAddress="0.0.2001",Amount=1.23m,
                ProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.HashgraphOASIS)};
            var unlocked=new Core.Objects.Wallet.Requests.UnlockWeb4TokenRequest{UnlockedByAvatarId=avatar,LockId=id,TokenAddress="0.0.3001",EscrowContractAddress="0.0.2001",
                ProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.HashgraphOASIS)};
            var result=viaManager?release?synchronous?wallets.UnlockToken(unlocked):await wallets.UnlockTokenAsync(unlocked)
                :synchronous?wallets.LockToken(locked):await wallets.LockTokenAsync(locked)
                :release?synchronous?hedera.UnlockToken(unlocked):await hedera.UnlockTokenAsync(unlocked)
                :synchronous?hedera.LockToken(locked):await hedera.LockTokenAsync(locked);
            Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);Assert.Equal(1,result.SavedCount);
            var response=Assert.IsType<HederaIdBoundEscrowResponse>(result.Result);Assert.Equal(id,response.LockId);
            Assert.Equal(release?1:2,response.NativeTransactions.Count);Assert.Equal(response.NativeTransactions.Last(),response.TransactionResult);
            Assert.False(hedera.IsProviderActivated);
            var retained=await local.LoadProviderWalletsForAvatarByIdAsync(avatar);Assert.False(retained.IsError);
            Assert.Equal(encrypted,retained.Result[ProviderType.HashgraphOASIS].Single().PrivateKey);
            await fixture.StandardInput.WriteLineAsync("stop");fixture.StandardInput.Close();
            using var finish=JsonDocument.Parse((await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)))!);
            Assert.True(finish.RootElement.GetProperty("completed").GetBoolean());Assert.False(finish.RootElement.GetProperty("prepared").GetBoolean());
            Assert.Equal(release?1:2,finish.RootElement.GetProperty("submissions").GetInt32());
            Assert.Equal(response.NativeTransactions,finish.RootElement.GetProperty("nativeTransactions").EnumerateArray().Select(row=>row.GetString()!));
            await fixture.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));Assert.True(fixture.ExitCode==0,await errors);
            completed=true;
        }finally{
            Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",priorBridge);
            try{
                if(!fixture.HasExited)fixture.Kill(entireProcessTree:true);
                await fixture.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
                if(completed){Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(avatar,new())).IsError);
                    Assert.Equal("{}",await File.ReadAllTextAsync(walletFile));File.Delete(walletFile);}
            }finally{await local.DeActivateProviderAsync();}
        }
    }
}
