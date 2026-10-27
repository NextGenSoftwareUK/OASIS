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
    public sealed class ResolverRejectedChild : Holon { }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task RequestResolverAuthorizesAssignedIdsAndOnlyTheSelectedDepth(bool synchronous)
    {
        var f = Fixture(true, conditional: true, preparation: true);
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid();
        var fallback = MetadataPolicy(new MetadataResolver(), context: "unused-fallback");
        f.Manager.PreparationEncryptionPolicy = fallback;
        var policies = new Dictionary<Guid, HolonMetadataEncryptionPolicy>();
        f.Manager.PreparationPolicyResolver = selected =>
        {
            selected.RecordId.Should().NotBeEmpty(); selected.RuntimeType.Should().Be(typeof(Holon));
            var policy = MetadataPolicy(new MetadataResolver(), context: "node-" + selected.RecordId.ToString("N"));
            policies.Add(selected.RecordId, policy); return policy;
        };
        var scoped = f.Manager.CreateRequestContext(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS).Result.Manager;
        f.Manager.PreparationPolicyResolver = _ => throw new InvalidOperationException("Later parent change must not affect the request.");
        HolonWritePreparation? observed = null;
        OASISResult<HolonWritePreparation> Capture(HolonWritePreparation value)
        { observed = value; return new() { IsError = true, ErrorCode = "TEST_PREPARATION_CAPTURED" }; }
        f.Provider.As<IHolonWritePreparationStorageProvider>().Setup(value => value.ClaimHolonWritePreparation(It.IsAny<HolonWritePreparation>()))
            .Returns((HolonWritePreparation value) => Capture(value));
        f.Provider.As<IHolonWritePreparationStorageProvider>().Setup(value => value.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolonWritePreparation value, CancellationToken _) => Capture(value));
        var root = new Holon { Name = "selected root" }; var child = new Holon { Name = "selected child" };
        var excluded = new Holon { Name = "excluded grandchild" }; root.Children.Add(child); child.Children.Add(excluded);
        var result = synchronous ? scoped.SaveHolon(root, actor, new CosmicWriteOptions(operation), true, true, 1, ProviderType.SQLLiteDBOASIS)
            : await scoped.SaveHolonAsync(root, actor, new CosmicWriteOptions(operation), true, true, 1, ProviderType.SQLLiteDBOASIS);
        result.ErrorCode.Should().Be("TEST_PREPARATION_CAPTURED");
        policies.Should().HaveCount(2); observed!.Records.Should().HaveCount(2);
        foreach (var record in observed.Records)
            HolonPreparationCipher.Decrypt(record.HolonId, operation, actor, ProviderType.SQLLiteDBOASIS,
                record.Envelope, policies[record.HolonId]).Should().NotContain("excluded grandchild");
        root.Id.Should().BeEmpty(); child.Id.Should().BeEmpty(); excluded.Id.Should().BeEmpty();
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task DeniedSelectedChildCannotFallBackToGlobalProtection(bool returnNull)
    {
        var f = Fixture(true, conditional: true, preparation: true);
        var fallback = MetadataPolicy(new MetadataResolver()); f.Manager.PreparationEncryptionPolicy = fallback;
        var calls = 0;
        f.Manager.PreparationPolicyResolver = selected =>
        {
            calls++;
            if (selected.RuntimeType == typeof(ResolverRejectedChild))
                return returnNull ? null! : throw new InvalidOperationException("The child context is denied.");
            return fallback;
        };
        var root = new Holon(); root.Children.Add(new ResolverRejectedChild());
        var result = await f.Manager.SaveHolonAsync(root, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("HOLON_SAVE_PREPARATION_FAILED");
        calls.Should().Be(2); root.Id.Should().BeEmpty(); root.Children.Single().Id.Should().BeEmpty();
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ClaimHolon") || call.Method.Name.Contains("ExecuteConditional")).Should().BeEmpty();
    }
}
