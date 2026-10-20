using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.Common;
using Rijndael256;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.Subscription;

public partial class SubscriptionEndpointsTests
{
    [Fact]
    [Trait("Category", "RetainedNativeRuntime")] // Needs the retained local Sui/TRON chains; CI filters this category out.
    public async Task WalletHttpSendCommitsRealTrxWithJwtAndEncryptedAvatarWallet()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!System.IO.File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Use-ReusableTronRuntime.ps1")))directory=directory.Parent;
        directory.Should().NotBeNull();
        var root=Path.Combine(directory!.FullName,"TestResults/Web4Current/JwtAvatar");Directory.CreateDirectory(root);
        using var lease=new FileStream(Path.Combine(root,"store.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var key=new string('0',63)+"1"; // Public local fixture only.
        var sender=new TronECKey(key,TronNetwork.MainNet).GetPublicAddress();
        var recipient=new TronECKey(new string('0',63)+"2",TronNetwork.MainNet).GetPublicAddress();
        var contract=(await System.IO.File.ReadAllTextAsync(Path.Combine(directory.FullName,"TestResults/TronRuntime/local-store-address.txt"))).Trim();
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),
            "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f",contractFeeLimitSun:1000000000);
        using var backend=new TronPrivateStorageBackend(runtime.NativeTransfers,sender,contract,()=>key,
            ()=>SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
        var tron=new TRONOASIS(rpcEndpoint:"http://127.0.0.1:9090",nativeTransfers:runtime.NativeTransfers,storageBackend:backend);
        var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatarId=Guid.Parse("11111111-1111-1111-1111-111111111111");
        var encrypted=Rijndael256.Rijndael.Encrypt(key,new string('k',32),KeySize.Aes256);
        var field=typeof(Program).GetField("_avatarManager",BindingFlags.NonPublic|BindingFlags.Static)!;
        var previousManager=field.GetValue(null);var previousDna=NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA;
        var saved=false;
        try
        {
            (await local.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=avatarId,Username="native-http-jwt-avatar",Email="native-http-jwt@example.invalid"})).IsError.Should().BeFalse();saved=true;
            var wallet=new ProviderWallet {Id=Guid.NewGuid(),AvatarId=avatarId,IsDefaultWallet=true,ProviderType=ProviderType.TRONOASIS,
                WalletAddress=sender,PrivateKey=encrypted,PublicKey=Convert.ToHexString(new Nethereum.Signer.EthECKey(key).GetPubKey())};
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId,new() {[ProviderType.TRONOASIS]=new List<IProviderWallet> {wallet}})).IsError.Should().BeFalse();
            const string secret="public-test-only-signing-key-0123456789012345678901234567890123456789";
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;dna.OASIS.StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false};
            dna.OASIS.Security=new SecuritySettings {SecretKey=secret,OASISProviderPrivateKeys=new EncryptionSettings {Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32)}};
            var providers=new ProviderManager(null,dna);providers.RegisterProvider(local);providers.RegisterProvider(tron);
            providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            var avatarConstructor=typeof(AvatarManager).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single(item=>item.GetParameters().Length==3);
            field.SetValue(null,avatarConstructor.Invoke(new object[] {local,dna,providers}));NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA=dna;
            var walletConstructor=typeof(WalletManager).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single(item=>item.GetParameters().Length==4);
            _factory.NativeWalletManagerForTest=(WalletManager)walletConstructor.Invoke(new object[] {local,dna,providers,null});
            var before=await runtime.NativeTransfers.GetNativeBalanceAsync(recipient);
            var request=new SendWeb4TokenRequest {FromProvider=new(ProviderType.TRONOASIS),ToProvider=new(ProviderType.TRONOASIS),FromTokenAddress="TRX",
                FromAvatarId=avatarId,FromAvatarUsername="native-http-jwt-avatar",FromAvatarEmail="native-http-jwt@example.invalid",ToWalletAddress=recipient,Amount=0.001m};
            var jwt=new JwtSecurityToken("OASIS","OASIS",new[] {new Claim("sub",avatarId.ToString()),new Claim("id",avatarId.ToString())},
                DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),SecurityAlgorithms.HmacSha256));
            using var message=new HttpRequestMessage(HttpMethod.Post,"/api/wallet/send-token") {Content=JsonContent.Create(request)};
            message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));
            var response=await _client.SendAsync(message);response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body=JsonSerializer.Deserialize<OASISResult<SendWeb4TokenResponse>>(await response.Content.ReadAsStringAsync(),new JsonSerializerOptions {PropertyNameCaseInsensitive=true});
            body.Should().NotBeNull();body!.IsError.Should().BeFalse(body.Message);body.Result.SendTransactionResult.Should().MatchRegex("^[0-9a-f]{64}$");
            ((await runtime.NativeTransfers.GetNativeBalanceAsync(recipient))-before).Should().Be(0.001m);
            (await runtime.NativeTransfers.GetTransactionStatusAsync(body.Result.SendTransactionResult)).Should().Be(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed);
            (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[ProviderType.TRONOASIS][0].PrivateKey.Should().Be(encrypted);
        }
        finally
        {
            field.SetValue(null,previousManager);NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA=previousDna;_factory.NativeWalletManagerForTest=null;
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId,new())).IsError.Should().BeFalse();
            if(saved)(await local.DeleteAvatarAsync(avatarId,softDelete:false)).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();await tron.DeActivateProviderAsync();
        }
    }
}
