using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ScopedWriteRetainsTheCapturedProtectionConfiguration(bool preparationOnly)
    {
        var f = Fixture(true, conditional: true, preparation: true);
        var policy = MetadataPolicy(new MetadataResolver(), context: "original-authorized-context");
        if (preparationOnly) f.Manager.PreparationEncryptionPolicy = policy;
        else f.Manager.MetadataEncryptionPolicy = policy;
        var scoped = f.Manager.CreateRequestContext(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS);
        scoped.IsError.Should().BeFalse(scoped.Message);
        // A later global change must not alter an already captured request.
        var later = MetadataPolicy(new MetadataResolver(), context: "later-global-context");
        f.Manager.MetadataEncryptionPolicy = later;
        f.Manager.PreparationEncryptionPolicy = later;
        HolonWritePreparation? observed = null;
        f.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(provider => provider.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .Callback<HolonWritePreparation, CancellationToken>((value, _) => observed = value)
            .ReturnsAsync(new OASISResult<HolonWritePreparation> { IsError = true, ErrorCode = "TEST_PREPARATION_OBSERVED" });
        var source = new Holon { Name = "private scoped original" };
        source.MetaData["private-note"] = "private scoped metadata";
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid();
        var result = await scoped.Result.Manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(operation),
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        result.ErrorCode.Should().Be("TEST_PREPARATION_OBSERVED");
        observed.Should().NotBeNull();
        var record = observed!.Records.Single();
        record.Envelope.Should().NotContain(source.Name);
        var plaintext = HolonPreparationCipher.Decrypt(record.HolonId, operation, actor, ProviderType.SQLLiteDBOASIS, record.Envelope, policy);
        plaintext.Should().Contain("private scoped original");
        source.Id.Should().BeEmpty();
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional")).Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentScopedPreparationPoliciesCannotBleedBetweenClientsOrIntoTheParent()
    {
        var f = Fixture(true, conditional: true, preparation: true);
        var global = MetadataPolicy(new MetadataResolver(), context: "parent-default");
        f.Manager.PreparationEncryptionPolicy = global;
        var first = f.Manager.CreateRequestContext(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS).Result.Manager;
        var second = f.Manager.CreateRequestContext(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS).Result.Manager;
        var policyA = MetadataPolicy(new MetadataResolver(), context: "request-a");
        var policyB = MetadataPolicy(new MetadataResolver(), context: "request-b");
        first.PreparationEncryptionPolicy = policyA;
        second.PreparationEncryptionPolicy = policyB;
        var operationA = Guid.NewGuid(); var operationB = Guid.NewGuid();
        var actorA = Guid.NewGuid(); var actorB = Guid.NewGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var captured = new Dictionary<Guid, HolonWritePreparation>();
        f.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(provider => provider.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .Returns(async (HolonWritePreparation value, CancellationToken _) =>
            {
                captured.Add(value.OperationId, value);
                if (value.OperationId == operationA) { entered.TrySetResult(); await release.Task; }
                return new OASISResult<HolonWritePreparation> { IsError = true, ErrorCode = "TEST_PREPARATION_OBSERVED" };
            });
        var pending = first.SaveHolonAsync(new Holon { Name = "client a private value" }, actorA,
            new CosmicWriteOptions(operationA), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var completed = await second.SaveHolonAsync(new Holon { Name = "client b private value" }, actorB,
                new CosmicWriteOptions(operationB), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
            completed.ErrorCode.Should().Be("TEST_PREPARATION_OBSERVED");
        }
        finally { release.TrySetResult(); }
        (await pending).ErrorCode.Should().Be("TEST_PREPARATION_OBSERVED");
        foreach (var row in new[] { (operationA, actorA, policyA, "client a private value"), (operationB, actorB, policyB, "client b private value") })
        {
            var record = captured[row.Item1].Records.Single();
            HolonPreparationCipher.Decrypt(record.HolonId, row.Item1, row.Item2, ProviderType.SQLLiteDBOASIS,
                record.Envelope, row.Item3).Should().Contain(row.Item4);
        }
        f.Manager.PreparationEncryptionPolicy.Should().BeSameAs(global);
        first.PreparationEncryptionPolicy.Should().BeSameAs(policyA);
        second.PreparationEncryptionPolicy.Should().BeSameAs(policyB);
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional")).Should().BeEmpty();
    }
}
