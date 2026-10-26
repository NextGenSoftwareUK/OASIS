using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HostedCosmicRecoveryTests
{
    [Theory][InlineData("not-enrolled")][InlineData("missing-policy")][InlineData("configured")]
    public async Task ReadinessRequiresPolicyForEnrolledRecoveryWithoutResolvingKeysOrActors(string mode)
    {
        var policy = new Policy((_, _) => throw new InvalidOperationException("Readiness must not authorize or resolve keys."));
        using var services = Services(mode == "configured" ? policy : null, mode != "not-enrolled");
        var readiness = new CosmicRecoveryReadiness(services);
        if (mode == "missing-policy") await Assert.ThrowsAsync<InvalidOperationException>(() => readiness.StartAsync(default));
        else await readiness.StartAsync(default);
        Assert.Equal(0, policy.Calls);
    }
    private sealed class Keys : IHolonMetadataKeyResolver
    {
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            => new(new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)73, 32).ToArray()));
    }
    private sealed class Policy(Func<Guid, ProviderType, OASISResult<CosmicOperationRecoveryGrant>> authorize) : ICosmicOperationRecoveryPolicy
    {
        public int Calls;
        public OASISResult<CosmicOperationRecoveryGrant> Authorize(Guid actor, ProviderType authority)
        { Calls++; return authorize(actor, authority); }
    }
    private static readonly ProviderType Authority = ProviderType.SQLLiteDBOASIS;
    private static UsageEndpointPolicy Budget() => new()
    {
        MeterCategory = "cosmic-recovery", PricingCatalogueVersion = "test-v1", ReservedUnits = 1,
        EstimatedCostUsd = 1, MaximumNativeFeeCostUsd = 0, MaximumProviderAttempts = 2, RequiresProviderMeasurement = true
    };
    private static DefaultHttpContext Context(Guid actor, IServiceProvider services) => new()
    {
        RequestServices = services, Items = new Dictionary<object, object?> { ["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor } }
    };
    private static ServiceProvider Services(Policy? policy, bool enrolled = true)
    {
        var services = new ServiceCollection();
        if (policy != null) services.AddSingleton<ICosmicOperationRecoveryPolicy>(policy);
        if (enrolled) services.AddSingleton(new StorageAdmissionRoutes(new[]
            { new StorageAdmissionRoute("POST", CosmicOperationRecoveryRoutes.Recover, Budget()) }));
        return services.BuildServiceProvider();
    }
    private static async Task<PreparedStorageExecution> Admission(Guid actor)
    {
        var outbox = new Mock<IUsageOutboxStore>();
        outbox.Setup(value => value.TryBeginAsync(It.IsAny<UsageOutboxOperation>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var authority = new Mock<IUsageReservationAuthority>();
        authority.Setup(value => value.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, UsageAuthorizationRequest request, CancellationToken _) => new SubscriptionAuthorizationResult
                { Allowed = true, UserId = actor.ToString("D"), OperationId = request.OperationId, Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) });
        authority.Setup(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageStartRequest request, CancellationToken _) => new UsageStartResult { Started = true, OperationId = request.OperationId });
        var handle = await new Web4UsageExecution(authority.Object, outbox.Object, new Web4UsageLedgerOptions { Service = "recovery-unit" })
            .BeginAsync("controlled-bearer", actor.ToString("D"), "recovery-test", new string('a', 64),
                "POST " + CosmicOperationRecoveryRoutes.Recover, Budget());
        return new PreparedStorageExecution(handle, "recovery-test", new Dictionary<ProviderType, string>(), new[] { Authority },
            Array.Empty<StorageQueryTariff>(), new[] { "ReadHolonWritePreparation", "ReadConditionalHolonWriteOutcome" }
                .Select(operation => new StorageAttemptTariff(Authority, operation, 0.001m, "test-v1")), true);
    }
    private static void Route(DefaultHttpContext context)
    { context.Request.Method = "POST"; context.Request.Path = CosmicOperationRecoveryRoutes.Recover; }

    [Theory][InlineData("actor")][InlineData("legacy")][InlineData("operation")][InlineData("default")][InlineData("numeric")][InlineData("admission")]
    public async Task InvalidRecoveryStopsBeforePolicyOrManagerAccess(string mode)
    {
        var policy = new Policy((_, _) => throw new InvalidOperationException("Policy must not be reached."));
        using var services = Services(policy);
        var context = Context(mode == "actor" ? Guid.Empty : Guid.NewGuid(), services); Route(context);
        var result = await HostedCosmicRecoveryEndpoint.RecoverAsync(context, mode == "legacy" ? HyperDriveModes.Legacy : HyperDriveModes.V2,
            mode == "operation" ? Guid.Empty : Guid.NewGuid(), mode == "default" ? "Default" : mode == "numeric" ? ((int)Authority).ToString() : Authority.ToString(),
            () => throw new InvalidOperationException("Manager must not be acquired."));
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal(0, policy.Calls);
        Assert.Equal(mode == "actor" ? "COSMIC_RECOVERY_ACTOR_REQUIRED" : mode == "legacy" ? "COSMIC_RECOVERY_V2_REQUIRED"
            : mode == "admission" ? "COSMIC_RECOVERY_ADMISSION_REQUIRED" : "COSMIC_RECOVERY_INPUT_INVALID", result.ErrorCode);
    }

    [Theory][InlineData("missing")][InlineData("denied")][InlineData("wrong-actor")][InlineData("wrong-authority")][InlineData("unregistered")]
    public async Task AdmittedRecoveryStillRequiresAnExactHostGrant(string mode)
    {
        var actor = Guid.NewGuid();
        var protection = new HolonMetadataEncryptionPolicy("host-test", "key-v1", new Keys());
        var policy = new Policy((_, _) => mode == "denied" ? new() { IsError = true, ErrorCode = "COSMIC_RECOVERY_FORBIDDEN" }
            : new(new CosmicOperationRecoveryGrant(mode == "wrong-actor" ? Guid.NewGuid() : actor,
                mode == "wrong-authority" ? ProviderType.MongoDBOASIS : Authority, _ => protection, new[] { typeof(Holon) })));
        using var services = Services(mode == "missing" ? null : policy, mode != "unregistered");
        var context = Context(actor, services); Route(context);
        var admission = await Admission(actor);
        var result = await admission.RunAsync(() => HostedCosmicRecoveryEndpoint.RecoverAsync(context, HyperDriveModes.V2,
            Guid.NewGuid(), Authority.ToString(), () => throw new InvalidOperationException("Manager must not be acquired.")));
        Assert.True(result.IsError); Assert.Null(result.Result);
        Assert.Equal(mode == "unregistered" ? "COSMIC_RECOVERY_ADMISSION_REQUIRED" : mode == "missing" ? "COSMIC_RECOVERY_POLICY_REQUIRED"
            : mode == "denied" ? "COSMIC_RECOVERY_FORBIDDEN" : "COSMIC_RECOVERY_POLICY_BINDING_INVALID", result.ErrorCode);
    }

    [Theory][InlineData("empty")][InlineData("interface")][InlineData("duplicate")][InlineData("non-holon")]
    public void GrantRejectsAnInvalidRuntimeCatalogue(string mode)
    {
        var types = mode == "empty" ? Array.Empty<Type>() : mode == "interface" ? new[] { typeof(IHolon) }
            : mode == "duplicate" ? new[] { typeof(Holon), typeof(Holon) } : new[] { typeof(string) };
        Assert.Throws<ArgumentException>(() => new CosmicOperationRecoveryGrant(Guid.NewGuid(), Authority,
            _ => new HolonMetadataEncryptionPolicy("host-test", "key-v1", new Keys()), types));
    }

    [Fact]
    public async Task AuthorizedRecoveryReadsStoredOriginalAndReturnsOnlyRedactedOutcome()
    {
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid();
        var protection = new HolonMetadataEncryptionPolicy("host-test", "key-v1", new Keys());
        var provider = new Mock<IOASISStorageProvider>(); provider.SetupAllProperties();
        provider.As<IConditionalHolonStorageProvider>(); provider.As<IHolonWritePreparationStorageProvider>();
        provider.As<IConditionalHolonWriteRecoveryStorageProvider>(); provider.As<IHolonWritePreparationRecoveryStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(Authority);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage); provider.Object.IsProviderActivated = true;
        provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
        HolonWritePreparation? original = null;
        provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(value => value.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .Callback<HolonWritePreparation, CancellationToken>((value, _) => original = value)
            .ReturnsAsync(new OASISResult<HolonWritePreparation> { IsError = true, ErrorCode = "TEST_CAPTURED_ORIGINAL" });
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var registry = new ProviderManager(null, dna); registry.RegisterProvider(provider.Object); registry.SetAndActivateCurrentStorageProvider(provider.Object);
        var manager = new HolonManager(null, dna, registry) { PreparationEncryptionPolicy = protection };
        var captured = await manager.SaveHolonAsync(new Holon { Name = "private original" }, actor, new CosmicWriteOptions(operation), false, providerType: Authority);
        Assert.Equal("TEST_CAPTURED_ORIGINAL", captured.ErrorCode); Assert.NotNull(original);
        provider.As<IHolonWritePreparationRecoveryStorageProvider>()
            .Setup(value => value.ReadHolonWritePreparationAsync(operation, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation>(original) { IsLoaded = true });
        provider.As<IConditionalHolonWriteRecoveryStorageProvider>()
            .Setup(value => value.ReadConditionalHolonWriteOutcomeAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new(operation, actor, Authority,
                ConditionalHolonOperation.Save, ConditionalHolonReceiptState.Committed)) { IsLoaded = true });
        var policy = new Policy((a, p) => new(new CosmicOperationRecoveryGrant(a, p, _ => protection, new[] { typeof(Holon) })));
        using var services = Services(policy); var context = Context(actor, services); Route(context);
        provider.Invocations.Clear();
        var result = await (await Admission(actor)).RunAsync(() => HostedCosmicRecoveryEndpoint.RecoverAsync(context,
            HyperDriveModes.V2, operation, Authority.ToString(), () => manager));
        Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(ConditionalHolonReceiptState.Committed, result.Result.ReceiptState);
        Assert.DoesNotContain("private original", System.Text.Json.JsonSerializer.Serialize(result.Result));
        Assert.DoesNotContain(provider.Invocations, call => call.Method.Name.Contains("ClaimHolon") || call.Method.Name.Contains("ExecuteConditional"));
        Assert.Equal(1, policy.Calls);
    }

    [Fact]
    public void RecoveryActionUsesAuthenticationAndTheExactEnrolledRoute()
    {
        var action = typeof(DataController).GetMethod(nameof(DataController.RecoverConditionalOperation))!;
        Assert.Single(action.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Equal("~" + CosmicOperationRecoveryRoutes.Recover, Assert.Single(action.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>()).Template);
    }
}
