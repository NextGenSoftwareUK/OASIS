using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.Subscription
{
    /// <summary>
    /// HTTP-level integration tests using WebApplicationFactory.
    /// Subscription services and repositories are replaced with mocks so no OASIS storage layer is required.
    /// To run tests against the real Railway stack instead, set ONODE_BASE_URL and skip these tests.
    /// </summary>
    public partial class SubscriptionEndpointsTests : IClassFixture<SubscriptionWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly HttpClient _authenticatedClient;
        private readonly Mock<ISubscriptionService> _svc;
        private readonly SubscriptionWebAppFactory _factory;

        public SubscriptionEndpointsTests(SubscriptionWebAppFactory factory)
        {
            _factory = factory;
            _svc = factory.SvcMock;
            _client = factory.CreateClient();
            _authenticatedClient = factory.CreateClient();
            _authenticatedClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, "true");
        }

        // ── GET /api/subscription/plans — public endpoint ────────────────────

        [Theory]
        [InlineData("id")]
        [InlineData("username")]
        [InlineData("email")]
        public async Task WalletSendRejectsConflictingSenderAvatarBeforeManagerAccess(string selector)
        {
            var request=new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb4TokenRequest {Amount=0.001m};
            if(selector=="id")request.FromAvatarId=Guid.Parse("22222222-2222-2222-2222-222222222222");
            if(selector=="username")request.FromAvatarUsername="another-avatar";
            if(selector=="email")request.FromAvatarEmail="another-avatar@example.invalid";
            var response=await _authenticatedClient.PostAsJsonAsync("/api/wallet/send-token",request);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("AVATAR_OPERATION_FORBIDDEN");
        }

        [Fact]
        public async Task WalletHttpSendCommitsRealSuiTransferUsingEncryptedAvatarWallet()
        {
            var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Real retained Sui RPC required.");
            new Uri(rpc).IsLoopback.Should().BeTrue();
            using var deployment=JsonDocument.Parse(System.IO.File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained Sui deployment required.")));
            var settings=deployment.RootElement;
            using var sui=new NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!);
            var issuer=await sui.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);issuer.IsError.Should().BeFalse();
            var recipient=await sui.GenerateKeyPairAsync();recipient.IsError.Should().BeFalse();
            var directory=new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while(directory!=null&&!System.IO.File.Exists(System.IO.Path.Combine(directory.FullName,"Scripts","verify_hyperdrive_v2_operation_contracts.ps1")))directory=directory.Parent;
            directory.Should().NotBeNull();
            var root=System.IO.Path.Combine(directory!.FullName,"TestResults","Web4Current","JwtAvatar");
            System.IO.Directory.CreateDirectory(root);
            using var lease=new System.IO.FileStream(System.IO.Path.Combine(root,"store.lock"),System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None);
            var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:System.IO.Path.Combine(root,"files"));
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            var avatarId=Guid.Parse("11111111-1111-1111-1111-111111111111");
            var encrypted=Rijndael256.Rijndael.Encrypt(issuer.Result.PrivateKey,new string('k',32),Rijndael256.KeySize.Aes256);
            var wallet=new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet {Id=Guid.NewGuid(),AvatarId=avatarId,IsDefaultWallet=true,
                ProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS,WalletAddress=issuer.Result.WalletAddressLegacy,PublicKey=issuer.Result.PublicKey,PrivateKey=encrypted};
            var managerField=typeof(Program).GetField("_avatarManager",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
            var priorManager=managerField.GetValue(null);
            var priorDna=NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA;
            var avatarSaved=false;
            var recipientAvatarId=Guid.Parse("22222222-2222-2222-2222-222222222222");
            var recipientSaved=false;
            try
            {
                (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId,new() {[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS]=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IProviderWallet> {wallet}})).IsError.Should().BeFalse();
                var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA();dna.OASIS.HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2;
                dna.OASIS.StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings {LogSwitchingProviders=false};
                const string secret="public-test-only-signing-key-0123456789012345678901234567890123456789";
                dna.OASIS.Security=new NextGenSoftware.OASIS.API.DNA.SecuritySettings {SecretKey=secret,OASISProviderPrivateKeys=new NextGenSoftware.OASIS.API.DNA.EncryptionSettings {Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32)}};
                var providers=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna);providers.RegisterProvider(local);providers.RegisterProvider(sui);
                providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
                (await local.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=avatarId,Username="native-http-jwt-avatar",Email="native-http-jwt@example.invalid"})).IsError.Should().BeFalse();
                avatarSaved=true;
                (await local.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=recipientAvatarId,Username="native-http-recipient",Email="native-http-recipient@example.invalid"})).IsError.Should().BeFalse();
                recipientSaved=true;
                var recipientWallet=new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet {Id=Guid.NewGuid(),AvatarId=recipientAvatarId,IsDefaultWallet=true,
                    ProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS,WalletAddress=recipient.Result.WalletAddressLegacy,PublicKey=recipient.Result.PublicKey};
                var decoyWallet=new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet {Id=Guid.NewGuid(),AvatarId=recipientAvatarId,IsDefaultWallet=false,
                    ProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS,WalletAddress=issuer.Result.WalletAddressLegacy,PublicKey=issuer.Result.PublicKey};
                (await local.SaveProviderWalletsForAvatarByIdAsync(recipientAvatarId,new() {[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS]=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IProviderWallet> {decoyWallet,recipientWallet}})).IsError.Should().BeFalse();
                var avatarConstructor=typeof(NextGenSoftware.OASIS.API.Core.Managers.AvatarManager).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Single(item=>item.GetParameters().Length==3);
                managerField.SetValue(null,avatarConstructor.Invoke(new object[] {local,dna,providers}));
                NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA=dna;
                var constructor=typeof(NextGenSoftware.OASIS.API.Core.Managers.WalletManager).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Single(item=>item.GetParameters().Length==4);
                _factory.NativeWalletManagerForTest=(NextGenSoftware.OASIS.API.Core.Managers.WalletManager)constructor.Invoke(new object[] {local,dna,providers,null});
                var before=await sui.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy);before.IsError.Should().BeFalse();
                var request=new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb4TokenRequest {
                    FromAvatarId=avatarId,FromAvatarUsername="native-http-jwt-avatar",FromAvatarEmail="native-http-jwt@example.invalid",
                    ToAvatarId=recipientAvatarId,ToAvatarUsername="native-http-recipient",ToAvatarEmail="native-http-recipient@example.invalid",
                    FromProvider=new(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS),ToProvider=new(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS),
                    FromTokenAddress="0x2::sui::SUI",ToWalletAddress=recipient.Result.WalletAddressLegacy,Amount=0.001m};
                var claims=new[] {new Claim("sub",avatarId.ToString()),new Claim("id",avatarId.ToString())};
                var jwt=new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("OASIS","OASIS",claims,DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),
                    new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
                var bearer=new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);
                var senderBefore=await sui.GetAccountBalanceAsync(issuer.Result.WalletAddressLegacy);senderBefore.IsError.Should().BeFalse();
                request.ToWalletAddress=issuer.Result.WalletAddressLegacy;
                using(var conflict=new HttpRequestMessage(HttpMethod.Post,"/api/wallet/send-token") {Content=JsonContent.Create(request)})
                {
                    conflict.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",bearer);
                    var rejected=await _client.SendAsync(conflict);
                    var failure=JsonSerializer.Deserialize<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.SendWeb4TokenResponse>>(await rejected.Content.ReadAsStringAsync(),new JsonSerializerOptions {PropertyNameCaseInsensitive=true});
                    failure.Should().NotBeNull();failure!.IsError.Should().BeTrue();
                    failure.Message.Should().Contain("different wallets");failure.Result.SendTransactionResult.Should().BeNullOrEmpty();
                }
                var senderAfter=await sui.GetAccountBalanceAsync(issuer.Result.WalletAddressLegacy);senderAfter.IsError.Should().BeFalse();
                senderAfter.Result.Should().Be(senderBefore.Result,"recipient disagreement must not submit or burn gas");
                (await sui.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy)).Result.Should().Be(before.Result);
                request.ToWalletAddress=recipient.Result.WalletAddressLegacy;
                using var message=new HttpRequestMessage(HttpMethod.Post,"/api/wallet/send-token") {Content=JsonContent.Create(request)};
                message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",bearer);
                var response=await _client.SendAsync(message);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var body=JsonSerializer.Deserialize<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.SendWeb4TokenResponse>>(await response.Content.ReadAsStringAsync(),new JsonSerializerOptions {PropertyNameCaseInsensitive=true});
                body.Should().NotBeNull();body!.IsError.Should().BeFalse(body.Message);
                body.Result.Should().NotBeNull();body.Result.SendTransactionResult.Should().NotBeNullOrWhiteSpace();
                var after=await sui.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy);after.IsError.Should().BeFalse();
                (after.Result-before.Result).Should().Be(0.001m);
                (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS][0].PrivateKey.Should().Be(encrypted);
            }
            finally
            {
                managerField.SetValue(null,priorManager);
                NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA=priorDna;
                _factory.NativeWalletManagerForTest=null;
                (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId,new())).IsError.Should().BeFalse();
                if(avatarSaved)(await local.DeleteAvatarAsync(avatarId,softDelete:false)).IsError.Should().BeFalse();
                if(recipientSaved)
                {
                    (await local.SaveProviderWalletsForAvatarByIdAsync(recipientAvatarId,new())).IsError.Should().BeFalse();
                    (await local.DeleteAvatarAsync(recipientAvatarId,softDelete:false)).IsError.Should().BeFalse();
                }
                await local.DeActivateProviderAsync();
            }
        }

        [Fact]
        public async Task NativeWalletInjectionUsesSuppliedManagerWithoutProviderActivation()
        {
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA();
            var providers=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna);
            var constructor=typeof(NextGenSoftware.OASIS.API.Core.Managers.WalletManager).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Single(item=>item.GetParameters().Length==4);
            var manager=(NextGenSoftware.OASIS.API.Core.Managers.WalletManager)constructor.Invoke(new object[] {null,dna,providers,null});
            var controller=new NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.WalletController();
            var action=new Microsoft.AspNetCore.Mvc.ActionContext(new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                new Microsoft.AspNetCore.Routing.RouteData(),new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
            var filters=new List<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>();
            var context=new Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext(action,filters,new Dictionary<string,object>(),controller);
            var proceeded=false;
            await new NativeWalletTestInjectionFilter(()=>manager).OnActionExecutionAsync(context,()=>
            {
                proceeded=true;
                return Task.FromResult(new Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext(action,filters,controller));
            });
            proceeded.Should().BeTrue();controller.WalletManager.Should().BeSameAs(manager);
        }

        [Fact]
        public async Task NftImportRejectsAnotherAvatarBeforeManagerAccess()
        {
            var response=await _authenticatedClient.PostAsJsonAsync("/api/nft/import-web3-nft",
                new {ImportedByAvatarId=Guid.Parse("22222222-2222-2222-2222-222222222222"),Title="Test import",NFTTokenAddress="fixture-nft"});
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("AVATAR_OPERATION_FORBIDDEN");
        }

        [Fact]
        public async Task Web4NftImportBindsNestedConcreteBodyThenRejectsAnotherAvatar()
        {
            var response = await _authenticatedClient.PostAsJsonAsync("/api/nft/import-web4-nft/22222222-2222-2222-2222-222222222222",
                new { Id = Guid.NewGuid(), Title = "HTTP binding fixture", Web3NFTs = new[] { new { NFTTokenAddress = "fixture-object" } } });
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("AVATAR_OPERATION_FORBIDDEN");
        }

        [Fact]
        public async Task Web4NftImportRejectsMalformedBodyBeforeAction()
        {
            var response = await _authenticatedClient.PostAsync("/api/nft/import-web4-nft/22222222-2222-2222-2222-222222222222",
                new StringContent("{\"id\":\"not-a-guid\"}", Encoding.UTF8, "application/json"));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Web4NftFileImportRejectsAnotherAvatarBeforeFileAccess()
        {
            var response = await _authenticatedClient.PostAsync("/api/nft/import-web4-nft-from-file/22222222-2222-2222-2222-222222222222/nonexistent-owned-test-fixture.json", null);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("AVATAR_OPERATION_FORBIDDEN");
        }

        [Fact]
        public async Task Web4NftExportRejectsMalformedBodyBeforeFileAccess()
        {
            var response = await _authenticatedClient.PostAsync("/api/nft/export-web4-nft",
                new StringContent("{\"id\":\"not-a-guid\"}", Encoding.UTF8, "application/json"));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("burn-token","BurntByAvatarId")]
        [InlineData("lock-token","LockedByAvatarId")]
        [InlineData("unlock-token","UnlockedByAvatarId")]
        public async Task WalletMutationsRejectOtherAvatarBeforeProviderAccess(string route,string actorField)
        {
            var body=new Dictionary<string,string> { [actorField]="22222222-2222-2222-2222-222222222222",
                ["OwnerPublicKey"]="public-test-only",["OwnerPrivateKey"]="public-test-only",["OwnerSeedPhrase"]="public-test-only",["TokenAddress"]="fixture-token" };
            var response=await _authenticatedClient.PostAsJsonAsync("/api/wallet/"+route,body);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("AVATAR_OPERATION_FORBIDDEN");
        }

        [Theory]
        [InlineData("LocalFile")]
        [InlineData("Sui")]
        [InlineData("TRON")]
        public async Task HyperDriveReadRoutesAcceptRealJwtAfterV2PersistedAvatarLookup(string storage)
        {
            var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName,"Scripts","verify_hyperdrive_v2_operation_contracts.ps1"))) directory=directory.Parent;
            directory.Should().NotBeNull();
            var root=System.IO.Path.Combine(directory!.FullName,"TestResults","Web4Current","JwtAvatar");
            System.IO.Directory.CreateDirectory(root);
            using var lease=new System.IO.FileStream(System.IO.Path.Combine(root,"store.lock"),System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None);
            NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider local;
            IAsyncDisposable nativeRuntime=null;
            IDisposable privateStorage=null;
            var providerType=storage=="Sui"?NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS:
                storage=="TRON"?NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS:NextGenSoftware.OASIS.API.Core.Enums.ProviderType.LocalFileOASIS;
            if(storage=="TRON")
            {
                var key=new string('0',63)+"1"; // Public local-chain fixture only.
                var contract=System.IO.File.ReadAllText(System.IO.Path.Combine(directory.FullName,"TestResults","TronRuntime","local-store-address.txt")).Trim();
                var runtime=new NextGenSoftware.OASIS.API.Providers.TRONOASIS.TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),
                    "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f",contractFeeLimitSun:1000000000);
                nativeRuntime=runtime;
                var backend=new NextGenSoftware.OASIS.API.Providers.TRONOASIS.TronPrivateStorageBackend(runtime.NativeTransfers,
                    new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress(),contract,()=>key,
                    ()=>System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
                privateStorage=backend;
                local=new NextGenSoftware.OASIS.API.Providers.TRONOASIS.TRONOASIS(rpcEndpoint:"http://127.0.0.1:9090",nativeTransfers:runtime.NativeTransfers,storageBackend:backend);
            }
            else if(storage=="Sui")
            {
                var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Retained real Sui RPC required; no skipped evidence.");
                new Uri(rpc).IsLoopback.Should().BeTrue();
                using var deployment=JsonDocument.Parse(System.IO.File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained Sui deployment required.")));
                var settings=deployment.RootElement;
                local=new NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!);
            }
            else local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:System.IO.Path.Combine(root,"files"));
            const string secret="public-test-only-signing-key-0123456789012345678901234567890123456789";
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA();
            dna.OASIS.HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2;
            dna.OASIS.StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings();
            dna.OASIS.Security=new NextGenSoftware.OASIS.API.DNA.SecuritySettings {SecretKey=secret};
            var providers=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna)
                {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            providers.RegisterProvider(local);
            providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=Guid.Parse("d9b133dc-4a57-4fa1-9bba-8d123ea580a3"),Username="jwt-http-local-avatar",Email="jwt-http@example.invalid"};
            var managerField=typeof(Program).GetField("_avatarManager",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
            var priorManager=managerField.GetValue(null);
            var priorDna=NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA;
            var deleted=false;
            try
            {
                (await local.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
                var constructor=typeof(NextGenSoftware.OASIS.API.Core.Managers.AvatarManager).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Single(item=>item.GetParameters().Length==3);
                managerField.SetValue(null,constructor.Invoke(new object[] {local,dna,providers}));
                NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA=dna;
                var claims=new[] {new Claim("sub",avatar.Id.ToString()),new Claim("id",avatar.Id.ToString())};
                var token=new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("OASIS","OASIS",claims,DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),
                    new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
                var encoded=new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
                foreach(var route in new[] {"mode","config","status"})
                {
                    var before=providers.PerformanceMonitor.GetMetrics(providerType)?.TotalRequests??0;
                    using var request=new HttpRequestMessage(HttpMethod.Get,"/api/hyperdrive/"+route);
                    request.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",encoded);
                    var response=await _client.SendAsync(request);
                    response.StatusCode.Should().Be(HttpStatusCode.OK);
                    providers.PerformanceMonitor.GetMetrics(providerType).TotalRequests.Should().Be(before+1);
                    var body=JsonSerializer.Deserialize<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>>(
                        await response.Content.ReadAsStringAsync(),new JsonSerializerOptions {PropertyNameCaseInsensitive=true});
                    body.Should().NotBeNull();body!.IsError.Should().BeFalse(body.Message);
                }
                (await local.DeleteAvatarAsync(avatar.Id,softDelete:false)).IsError.Should().BeFalse();
                deleted=true;
                using var stale=new HttpRequestMessage(HttpMethod.Get,"/api/hyperdrive/mode");
                stale.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",encoded);
                var denied=await _client.SendAsync(stale);
                denied.StatusCode.Should().Be(HttpStatusCode.Unauthorized,"authoritative absence is not a provider outage");
                (await denied.Content.ReadAsStringAsync()).Should().Contain("INVALID_AVATAR");
            }
            finally
            {
                managerField.SetValue(null,priorManager);
                NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA=priorDna;
                if(!deleted)(await local.DeleteAvatarAsync(avatar.Id,softDelete:false)).IsError.Should().BeFalse();
                await local.DeActivateProviderAsync();
                if(local is IDisposable disposable)disposable.Dispose();
                privateStorage?.Dispose();
                if(nativeRuntime!=null)await nativeRuntime.DisposeAsync();
            }
        }

        [Theory]
        [InlineData("Bearer not-a-jwt")]
        [InlineData("Basic invalid")]
        [InlineData("Bearer")]
        public async Task HyperDriveReadRoutesRejectInvalidAuthorizationHeaders(string authorization)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/hyperdrive/mode");
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("mode")]
        [InlineData("config")]
        [InlineData("status")]
        public async Task HyperDriveReadRoutesRejectUnauthenticatedHttpRequests(string route)
        {
            var response = await _client.GetAsync("/api/hyperdrive/" + route);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("mode")]
        [InlineData("config")]
        [InlineData("status")]
        public async Task HyperDriveReadRoutesReturnSuccessfulEnvelopeForTestAuthenticatedAvatar(string route)
        {
            var response = await _authenticatedClient.GetAsync("/api/hyperdrive/" + route);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = JsonSerializer.Deserialize<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>>(
                await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            body.Should().NotBeNull();
            body!.IsError.Should().BeFalse(body.Message);
            body.Result.ValueKind.Should().NotBe(JsonValueKind.Undefined);
            body.Result.ValueKind.Should().NotBe(JsonValueKind.Null);
        }

        [Fact]
        public async Task GET_Plans_Returns200()
        {
            var response = await _client.GetAsync("/api/subscription/plans");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_Plans_BodyContainsFivePlans()
        {
            var body = await _client.GetStringAsync("/api/subscription/plans");
            body.Should().Contain("\"free\"");
            body.Should().Contain("\"bronze\"");
            body.Should().Contain("\"silver\"");
            body.Should().Contain("\"gold\"");
            body.Should().Contain("\"enterprise\"");
        }

        [Fact]
        public async Task GET_Plans_IsErrorFalse()
        {
            var body = await _client.GetStringAsync("/api/subscription/plans");
            body.Should().Contain("\"isError\":false");
        }

        // ── POST /api/subscription/checkout/session — unauthenticated ────────

        [Fact]
        public async Task POST_CheckoutSession_NoAuth_EnterprisePlan_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/checkout/session",
                new { PlanId = "enterprise" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task POST_CheckoutSession_NoAuth_UnknownPlan_Returns400Or401()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/checkout/session",
                new { PlanId = "nonexistent" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/subscriptions/me — unauthenticated ─────────

        [Fact]
        public async Task GET_SubscriptionsMe_Unauthenticated_Returns401()
        {
            var response = await _client.GetAsync("/api/subscription/subscriptions/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/orders/me — unauthenticated ────────────────

        [Fact]
        public async Task GET_OrdersMe_Unauthenticated_Returns401()
        {
            var response = await _client.GetAsync("/api/subscription/orders/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/usage — unauthenticated ────────────────────

        [Fact]
        public async Task GET_Usage_Unauthenticated_Returns401()
        {
            var response = await _client.GetAsync("/api/subscription/usage");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/hyperdrive-usage — authenticated ───────────

        [Fact]
        public async Task GET_HyperdriveUsage_Returns200()
        {
            var response = await _authenticatedClient.GetAsync("/api/subscription/hyperdrive-usage");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ── POST /api/subscription/webhooks/stripe — no secret ───────────────

        [Fact]
        public async Task POST_StripeWebhook_NoSecret_Returns400()
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/subscription/webhooks/stripe", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_StripeWebhook_MissingSignatureHeader_Returns400()
        {
            // Set a fake webhook secret so it gets past the "not configured" check
            Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", "whsec_fake_test_secret");
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/subscription/webhooks/stripe", content);

                // No Stripe-Signature header → 400
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally
            {
                Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", null);
            }
        }

        // ── POST /api/subscription/check-hyperdrive-quota ────────────────────

        [Fact]
        public async Task POST_CheckHyperdriveQuota_NullBody_Returns400()
        {
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await _authenticatedClient.PostAsync("/api/subscription/check-hyperdrive-quota", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_CheckHyperdriveQuota_ValidOperationType_Returns200()
        {
            var response = await _authenticatedClient.PostAsJsonAsync("/api/subscription/check-hyperdrive-quota",
                new { OperationType = "Requests" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// Shared fixture: boots the WebAPI with mocked subscription storage and explicit test authentication.
    /// Native-provider cases supply retained local runtimes explicitly; the factory never installs them.
    /// </summary>
    public class SubscriptionWebAppFactory : WebApplicationFactory<Program>
    {
        public NextGenSoftware.OASIS.API.Core.Managers.WalletManager NativeWalletManagerForTest { get; set; }
        public NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager NativeNftManagerForTest { get; set; }
        public Mock<ISubscriptionService> SvcMock { get; } = new();
        public Mock<ISubscriptionUsageRepository> UsageRepositoryMock { get; } = new();
        public Mock<ISubscriptionBillingRepository> BillingRepositoryMock { get; } = new();
        public Mock<ISubscriptionPlanPriceReader> PriceReaderMock { get; } = new();
        public Mock<IPaidOverageStatementStore> OverageStoreMock { get; } = new();
        public Mock<IPaidOverageGateway> OverageGatewayMock { get; } = new();

        public SubscriptionWebAppFactory()
        {
            // Default stub: no subscription record, empty usage, empty orders
            SvcMock.Setup(s => s.GetSubscriptionAsync(It.IsAny<string>()))
                   .ReturnsAsync((SubscriptionRecord?)null);
            SvcMock.Setup(s => s.GetUsageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                   .ReturnsAsync(new UsageRecord());
            SvcMock.Setup(s => s.GetOrdersAsync(It.IsAny<string>()))
                   .ReturnsAsync(new List<OrderRecord>());
            SvcMock.Setup(s => s.UpsertSubscriptionAsync(It.IsAny<SubscriptionRecord>()))
                   .Returns(Task.CompletedTask);
            SvcMock.Setup(s => s.AddOrderAsync(It.IsAny<OrderRecord>()))
                   .Returns(Task.CompletedTask);
            SvcMock.Setup(s => s.SetPayAsYouGoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns(Task.CompletedTask);
            PriceReaderMock.Setup(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new SubscriptionPlanPrice(10m, "USD", "month"));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
                    options.Filters.Add(new NativeWalletTestInjectionFilter(() => NativeWalletManagerForTest, () => NativeNftManagerForTest)));
                services.RemoveAll<ISubscriptionService>();
                services.RemoveAll<ISubscriptionUsageRepository>();
                services.RemoveAll<ISubscriptionBillingRepository>();
                services.RemoveAll<ISubscriptionPlanPriceReader>();
                services.RemoveAll<SubscriptionPlanCatalogue>();
                services.RemoveAll<IPaidOverageStatementStore>();
                services.RemoveAll<IPaidOverageGateway>();
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                services.AddSingleton(SvcMock.Object);
                services.AddSingleton(UsageRepositoryMock.Object);
                services.AddSingleton(BillingRepositoryMock.Object);
                services.AddSingleton(PriceReaderMock.Object);
                services.AddSingleton(new SubscriptionPlanCatalogue(PriceReaderMock.Object));
                services.AddSingleton(OverageStoreMock.Object);
                services.AddSingleton(OverageGatewayMock.Object);

                services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName, _ => { });
            });
        }
    }

    // Test-only injection into the existing concrete controller. The supplied
    // WalletManager still executes real provider operations; no provider mock.
    public sealed class NativeWalletTestInjectionFilter(Func<NextGenSoftware.OASIS.API.Core.Managers.WalletManager> manager,
        Func<NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager>? nftManager = null)
        : Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            if (context.Controller is NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.WalletController wallet && manager() is { } configured)
                typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.WalletController)
                    .GetField("_walletManager",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!
                    .SetValue(wallet,configured);
            if (context.Controller is NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.NftController nft && nftManager?.Invoke() is { } configuredNft)
                typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.NftController)
                    .GetField("_NFTManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .SetValue(nft, configuredNft);
            await next();
        }
    }

    public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "SubscriptionIntegrationTest";
        public const string HeaderName = "X-OASIS-Test-Authenticated";
        private const string TestAvatarId = "11111111-1111-1111-1111-111111111111";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var value) || value.ToString() != "true")
                return Task.FromResult(AuthenticateResult.NoResult());

            var avatarId = Guid.Parse(TestAvatarId);
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, TestAvatarId),
                    new Claim("sub", TestAvatarId)
                },
                SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            Context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
