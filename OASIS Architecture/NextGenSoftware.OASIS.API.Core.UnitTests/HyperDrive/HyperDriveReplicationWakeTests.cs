using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WakeSubscriberFailuresCannotReplaceCommittedAcknowledgement(bool synchronous)
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var routing = new ProviderManager(null, dna)
        { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = true, IsAutoLoadBalanceEnabled = false };
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.As<IConditionalHolonStorageProvider>();
        var atomic = provider.As<IAtomicHolonReplicationStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = true;
        var root = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), Name = "native acknowledgement" };
        var enrollment = new HolonReplicationEnrollment("source", new[] { "target" });
        var nativeWarning = new IOException("Original native warning.");
        var committed = new OASISResult<IEnumerable<IHolon>>(new[] { root })
        {
            IsSaved = true, IsWarning = true, WarningCount = 1, Exception = nativeWarning,
            Message = "Native transaction committed.", MetaData = new() { ["receipt"] = "original" }
        };
        atomic.Setup(value => value.ExecuteReplicatingHolonWrite(It.IsAny<ConditionalHolonWriteRequest>(), enrollment)).Returns(committed);
        atomic.Setup(value => value.ExecuteReplicatingHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), enrollment, default)).ReturnsAsync(committed);
        routing.RegisterProvider(provider.Object);
        var router = new OASISHyperDrive(routing);
        var calls = new List<int>();
        var ownFlow = new AsyncLocal<bool> { Value = true };
        Action<ProviderType> first = _ => { if (!ownFlow.Value) return; calls.Add(1); throw new InvalidOperationException("First wake failed."); };
        Action<ProviderType> second = _ => { if (!ownFlow.Value) return; calls.Add(2); throw new IOException("Second wake failed."); };
        Action<ProviderType> last = _ => { if (ownFlow.Value) calls.Add(3); };
        HolonReplicationRoutes.EnrolledWriteCommitted += first;
        HolonReplicationRoutes.EnrolledWriteCommitted += second;
        HolonReplicationRoutes.EnrolledWriteCommitted += last;
        try
        {
            var request = new StorageOperationRequest
            {
                Operation = "SaveHolon", Payload = root, PreferredProvider = ProviderType.SQLLiteDBOASIS, SaveChildren = false,
                ConditionalWrite = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS,
                    new[] { new HolonWriteCondition(root.Id, null) }, enrollment)
            };
            var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);

            result.IsError.Should().BeFalse(result.Message);
            result.IsSaved.Should().BeTrue();
            result.Result.VersionId.Should().Be(root.VersionId);
            result.MetaData.Should().ContainKey("receipt").WhoseValue.Should().Be("original");
            result.IsWarning.Should().BeTrue();
            result.WarningCount.Should().Be(3);
            result.Message.Should().Be("Native transaction committed.");
            result.Exception.Should().BeOfType<AggregateException>().Which.Flatten().InnerExceptions.Should().HaveCount(3).And.Contain(nativeWarning);
            result.InnerMessages.Should().Contain(message => message.Contains("wake-up"));
            calls.Should().Equal(1, 2, 3);
            atomic.Invocations.Should().ContainSingle(call => call.Method.Name == (synchronous ? "ExecuteReplicatingHolonWrite" : "ExecuteReplicatingHolonWriteAsync"));
        }
        finally
        {
            HolonReplicationRoutes.EnrolledWriteCommitted -= first;
            HolonReplicationRoutes.EnrolledWriteCommitted -= second;
            HolonReplicationRoutes.EnrolledWriteCommitted -= last;
        }
    }
}
