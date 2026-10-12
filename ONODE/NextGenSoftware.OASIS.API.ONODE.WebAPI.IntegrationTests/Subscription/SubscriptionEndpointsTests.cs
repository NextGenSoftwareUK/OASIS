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
    public class SubscriptionEndpointsTests : IClassFixture<SubscriptionWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly HttpClient _authenticatedClient;
        private readonly Mock<ISubscriptionService> _svc;

        public SubscriptionEndpointsTests(SubscriptionWebAppFactory factory)
        {
            _svc = factory.SvcMock;
            _client = factory.CreateClient();
            _authenticatedClient = factory.CreateClient();
            _authenticatedClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, "true");
        }

        // ── GET /api/subscription/plans — public endpoint ────────────────────

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
    /// This avoids spinning up OASIS storage providers in CI.
    /// </summary>
    public class SubscriptionWebAppFactory : WebApplicationFactory<Program>
    {
        public Mock<ISubscriptionService> SvcMock { get; } = new();
        public Mock<ISubscriptionUsageRepository> UsageRepositoryMock { get; } = new();
        public Mock<ISubscriptionBillingRepository> BillingRepositoryMock { get; } = new();

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
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISubscriptionService>();
                services.RemoveAll<ISubscriptionUsageRepository>();
                services.RemoveAll<ISubscriptionBillingRepository>();
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                services.AddSingleton(SvcMock.Object);
                services.AddSingleton(UsageRepositoryMock.Object);
                services.AddSingleton(BillingRepositoryMock.Object);

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
