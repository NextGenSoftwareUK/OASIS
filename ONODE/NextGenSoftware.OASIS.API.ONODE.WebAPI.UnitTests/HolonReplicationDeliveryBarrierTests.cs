using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HolonReplicationDeliveryBarrierTests
{
    [Fact]
    public async Task DurableReconciliationBarrierIsReportedWithoutActivatingOrReclaimingItsTarget()
    {
        var source = Storage(ProviderType.MongoDBOASIS, true, true);
        var outbox = new Mock<IHolonReplicationReadinessOutbox>(MockBehavior.Strict);
        var operation = Guid.NewGuid();
        outbox.Setup(store => store.PeekNextAsync("hyperdrive-target:SQLLiteDBOASIS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(6, operation,
                "hyperdrive-target:SQLLiteDBOASIS", HolonDeliveryState.ReconciliationRequired, 1,
                "COSMIC_REPLICA_LINEAGE_CONFLICT", "Business payload must not be logged", null)));
        source.As<IHolonReplicationOutboxSource>().Setup(provider => provider.GetHolonReplicationOutbox())
            .Returns(new OASISResult<IHolonReplicationReadinessOutbox>(outbox.Object));

        var target = Storage(ProviderType.SQLLiteDBOASIS, false);
        var providers = new ProviderManager(null, new OASISDNA());
        providers.RegisterProvider(source.Object);
        providers.RegisterProvider(target.Object);
        var log = new Mock<ILogger<HolonReplicationDeliveryHostedService>>();
        using var worker = new HolonReplicationDeliveryHostedService(log.Object);

        await worker.DeliverAsync(providers, CancellationToken.None);

        outbox.VerifyAll(); // Strict store rejects any attempt to claim, renew or finish the barrier.
        target.Verify(provider => provider.ActivateProvider(), Times.Never);
        target.Verify(provider => provider.ActivateProviderAsync(), Times.Never);
        var warning = Assert.Single(log.Invocations.Where(call => call.Method.Name == "Log" && (LogLevel)call.Arguments[0] == LogLevel.Warning));
        var message = warning.Arguments[2].ToString()!;
        Assert.Contains("COSMIC_REPLICA_LINEAGE_CONFLICT", message);
        Assert.Contains(operation.ToString(), message);
        Assert.DoesNotContain("Business payload", message);
    }

    private static Mock<IOASISStorageProvider> Storage(ProviderType type, bool activated, bool source = false)
    {
        var provider = new Mock<IOASISStorageProvider>();
        if (source) provider.As<IHolonReplicationOutboxSource>();
        else provider.As<ICommittedHolonReplicaStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(type);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = activated;
        return provider;
    }
}
