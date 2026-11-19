using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedSavePublishesCommittedCallerDespiteWakeFailure(bool batch)
    {
        var fixture = Fixture(true, conditional: true, atomicReplication: true);
        fixture.Provider.As<IAtomicHolonReplicationStorageProvider>()
            .Setup(value => value.ExecuteReplicatingHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<HolonReplicationEnrollment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalHolonWriteRequest request, HolonReplicationEnrollment _, CancellationToken __) =>
                new OASISResult<IEnumerable<IHolon>>(request.Roots) { IsSaved = true });
        var source = new Holon { Name = "committed caller" };
        var ownFlow = new AsyncLocal<bool> { Value = true };
        Action<ProviderType> observer = _ => { if (ownFlow.Value) throw new IOException("Controlled wake failure."); };
        HolonReplicationRoutes.EnrolledWriteCommitted += observer;
        try
        {
            var options = new CosmicWriteOptions(Guid.NewGuid(), null, new HolonReplicationEnrollment("source", new[] { "target" }));
            if (batch)
            {
                var result = await fixture.Manager.SaveOwnedHolonsAsync(new[] { source }, Guid.NewGuid(), options);
                result.IsError.Should().BeFalse(result.Message);
                result.IsSaved.Should().BeTrue();
                result.IsWarning.Should().BeTrue();
                result.WarningCount.Should().Be(1);
                result.Result.Single().Should().BeSameAs(source);
            }
            else
            {
                var result = await fixture.Manager.SaveOwnedHolonAsync(source, Guid.NewGuid(), options);
                result.IsError.Should().BeFalse(result.Message);
                result.IsSaved.Should().BeTrue();
                result.IsWarning.Should().BeTrue();
                result.WarningCount.Should().Be(1);
                result.Result.Should().BeSameAs(source);
            }
            source.Id.Should().NotBeEmpty();
            source.Version.Should().Be(1);
            HolonChangeTracking.HasChanges(source).Should().BeFalse();
            fixture.Provider.Invocations.Count(call => call.Method.Name == "ExecuteReplicatingHolonWriteAsync").Should().Be(1);
        }
        finally { HolonReplicationRoutes.EnrolledWriteCommitted -= observer; }
    }
}
