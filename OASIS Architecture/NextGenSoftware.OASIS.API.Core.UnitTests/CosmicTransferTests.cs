using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicTransferTests
{
    [Fact]
    public void DefaultSourceResolvesTheCurrentProviderOnceForAPinnedTransfer()
    {
        var f = Fixture(true, true);
        var result = f.Manager.SendHolon<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.Default, ProviderType.MongoDBOASIS);
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.IsSaved.Should().BeTrue();
        f.Source.Verify(provider => provider.LoadHolon(f.Root.Id, true, true, 0, true, true, 0), Times.Once);
        f.Destination.Verify(provider => provider.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, true), Times.Once);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EachReplicaGetsAnIsolatedCopyOfThePrimaryAcknowledgement(bool synchronous)
    {
        var f = Fixture(true, false);
        Guid firstRevision = Guid.Empty; Guid lastRevision = Guid.Empty; string lastAcknowledgement = null;
        OASISResult<IHolon> MutateReplica(IHolon input)
        {
            firstRevision = input.VersionId;
            input.MetaData["acknowledged"] = "replica-mutated";
            input.Name = "replica mutated name"; input.VersionId = Guid.NewGuid();
            return new OASISResult<IHolon>(input) { IsSaved = true };
        }
        f.Replica.Setup(provider => provider.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, true))
            .Returns((IHolon h, bool a, bool b, int c, bool d, bool e) => MutateReplica(h));
        f.Replica.Setup(provider => provider.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, true))
            .ReturnsAsync((IHolon h, bool a, bool b, int c, bool d, bool e) => MutateReplica(h));
        var last = new Mock<IOASISStorageProvider>(); last.SetupAllProperties();
        last.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.IPFSOASIS);
        last.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        last.Object.ProviderName = "last owned replica"; last.Object.IsProviderActivated = true;
        OASISResult<IHolon> Inspect(IHolon input)
        {
            lastRevision = input.VersionId; lastAcknowledgement = (string)input.MetaData["acknowledged"];
            return new OASISResult<IHolon>(input) { IsSaved = true };
        }
        last.Setup(provider => provider.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, true))
            .Returns((IHolon h, bool a, bool b, int c, bool d, bool e) => Inspect(h));
        last.Setup(provider => provider.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, true))
            .ReturnsAsync((IHolon h, bool a, bool b, int c, bool d, bool e) => Inspect(h));
        f.Providers.RegisterProvider(last.Object);
        f.Providers.GetProvidersThatAreAutoReplicating().Add(new EnumValue<ProviderType>(ProviderType.IPFSOASIS));
        var result = synchronous
            ? f.Manager.SendHolon<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS, true)
            : await f.Manager.SendHolonAsync<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS, true);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        firstRevision.Should().Be(result.Result.VersionId); lastRevision.Should().Be(firstRevision);
        lastAcknowledgement.Should().Be("destination"); result.Result.MetaData["acknowledged"].Should().Be("destination");
        result.Result.Name.Should().Be("clean source");
    }

    public static IEnumerable<object[]> TransferPolicies()
    {
        foreach (var v2 in new[] { false, true })
        foreach (var synchronous in new[] { false, true })
        foreach (var global in new[] { false, true })
        foreach (var replicate in new[] { false, true })
            yield return new object[] { v2, synchronous, global, replicate };
    }

    [Theory]
    [MemberData(nameof(TransferPolicies))]
    public async Task CleanSourceIsExplicitlyWrittenToDestinationWithOperationLocalReplication(bool v2, bool synchronous, bool global, bool replicate)
    {
        var f = Fixture(v2, global); var actor = Guid.NewGuid();
        var revision = f.Root.VersionId;
        var result = synchronous
            ? f.Manager.SendHolon<Holon>(f.Root.Id, actor, ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS, replicate)
            : await f.Manager.SendHolonAsync<Holon>(f.Root.Id, actor, ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS, replicate);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.IsSaved.Should().BeTrue(); result.Result.Should().NotBeSameAs(f.Root);
        result.Result.Id.Should().Be(f.Root.Id); result.Result.Children.Should().ContainSingle();
        result.Result.Version.Should().Be(1, "the destination owns its acknowledged revision");
        result.Result.MetaData["acknowledged"].Should().Be("destination");
        f.Destination.Invocations.Count(call => call.Method.Name is "SaveHolon" or "SaveHolonAsync").Should().Be(1);
        f.Replica.Invocations.Count(call => call.Method.Name is "SaveHolon" or "SaveHolonAsync").Should().Be(replicate ? 1 : 0,
            result.Message + " " + string.Join("; ", result.InnerMessages));
        f.Source.Invocations.Count(call => call.Method.Name.StartsWith("SaveHolon")).Should().Be(0);
        f.Root.VersionId.Should().Be(revision); f.Root.MetaData.Should().NotContainKey("acknowledged");
        f.Providers.IsAutoReplicationEnabled.Should().Be(global);
        f.Providers.IsAutoFailOverEnabled.Should().BeTrue(); f.Providers.IsAutoLoadBalanceEnabled.Should().BeTrue();
        result.Result.HasHolonChanged().Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DestinationRejectionCannotBecomeSuccessAtAnotherProvider(bool synchronous)
    {
        var f = Fixture(true, true); var cause = new IOException("required destination rejected");
        f.Destination.Setup(provider => provider.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, true))
            .Returns(new OASISResult<IHolon> { IsError = true, ErrorCode = "DESTINATION_REJECTED", Exception = cause });
        f.Destination.Setup(provider => provider.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, true))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCode = "DESTINATION_REJECTED", Exception = cause });
        var result = synchronous
            ? f.Manager.SendHolon<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS)
            : await f.Manager.SendHolonAsync<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS);
        result.IsError.Should().BeTrue(); result.IsSaved.Should().BeFalse();
        result.ErrorCode.Should().Be("DESTINATION_REJECTED"); result.Exception.Should().BeSameAs(cause);
        f.Replica.Invocations.Where(call => call.Method.Name.StartsWith("SaveHolon")).Should().BeEmpty();
    }

    [Fact]
    public void SourceFailureRetainsNativeCauseAndDoesNotReadAnAlternateSource()
    {
        var f = Fixture(false, true); var cause = new IOException("required source unavailable");
        f.Source.Setup(provider => provider.LoadHolon(f.Root.Id, true, true, 0, true, true, 0))
            .Returns(new OASISResult<IHolon> { IsError = true, ErrorCode = "SOURCE_UNAVAILABLE", Exception = cause });
        var result = f.Manager.SendHolon<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS);
        result.ErrorCode.Should().Be("SOURCE_UNAVAILABLE"); result.Exception.Should().BeSameAs(cause);
        f.Replica.Invocations.Where(call => call.Method.Name.StartsWith("LoadHolon") || call.Method.Name.StartsWith("SaveHolon")).Should().BeEmpty();
        f.Destination.Invocations.Where(call => call.Method.Name.StartsWith("SaveHolon")).Should().BeEmpty();
    }

    [Fact]
    public async Task MissingActorAndSameProviderRejectBeforeSourceIo()
    {
        var f = Fixture(true, true);
        f.Manager.SendHolon<Holon>(f.Root.Id, Guid.Empty, ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS)
            .ErrorCode.Should().Be("HOLON_TRANSFER_ACTOR_REQUIRED");
        (await f.Manager.SendHolonAsync<Holon>(f.Root.Id, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, ProviderType.SQLLiteDBOASIS))
            .ErrorCode.Should().Be("HOLON_TRANSFER_SAME_PROVIDER");
        f.Source.Invocations.Where(call => call.Method.Name.StartsWith("LoadHolon")).Should().BeEmpty();
    }

    private static (HolonManager Manager, ProviderManager Providers, Mock<IOASISStorageProvider> Source,
        Mock<IOASISStorageProvider> Destination, Mock<IOASISStorageProvider> Replica, Holon Root) Fixture(bool v2, bool global)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode = v2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
            StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        Mock<IOASISStorageProvider> Provider(ProviderType type, string name)
        {
            var mock = new Mock<IOASISStorageProvider>(); mock.SetupAllProperties();
            mock.Object.ProviderType = new EnumValue<ProviderType>(type);
            mock.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
            mock.Object.ProviderName = name; mock.Object.IsProviderActivated = true;
            mock.Setup(provider => provider.ActivateProvider()).Returns(new OASISResult<bool>(true));
            OASISResult<IHolon> Save(IHolon source)
            {
                var copy = ImportGraphSnapshot.Create(new[] { source }).CreateAttempt().Single();
                copy.Version = 1; copy.VersionId = Guid.NewGuid(); copy.MetaData["acknowledged"] = name;
                return new OASISResult<IHolon>(copy) { IsSaved = true, Message = name + " acknowledged" };
            }
            mock.Setup(provider => provider.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, true))
                .Returns((IHolon h, bool a, bool b, int c, bool d, bool e) => Save(h));
            mock.Setup(provider => provider.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, true))
                .ReturnsAsync((IHolon h, bool a, bool b, int c, bool d, bool e) => Save(h));
            return mock;
        }
        var source = Provider(ProviderType.SQLLiteDBOASIS, "source");
        var destination = Provider(ProviderType.MongoDBOASIS, "destination"); var replica = Provider(ProviderType.LocalFileOASIS, "replica");
        var root = new Holon { Id = Guid.NewGuid(), Name = "clean source", Version = 7, VersionId = Guid.NewGuid(),
            CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), CreatedByAvatarId = Guid.NewGuid() };
        root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "source child" });
        root.AcceptChanges();
        source.Setup(provider => provider.LoadHolon(root.Id, true, true, 0, true, true, 0)).Returns(new OASISResult<IHolon>(root) { IsLoaded = true });
        source.Setup(provider => provider.LoadHolonAsync(root.Id, true, true, 0, true, true, 0)).ReturnsAsync(new OASISResult<IHolon>(root) { IsLoaded = true });
        var providers = new ProviderManager(null, dna) { IsAutoReplicationEnabled = global,
            IsAutoFailOverEnabled = true, IsAutoLoadBalanceEnabled = true };
        foreach (var mock in new[] { source, destination, replica }) providers.RegisterProvider(mock.Object);
        providers.SetAndActivateCurrentStorageProvider(source.Object).IsError.Should().BeFalse();
        providers.GetProvidersThatAreAutoReplicating().Add(new EnumValue<ProviderType>(ProviderType.LocalFileOASIS));
        providers.GetProviderAutoFailOverList().Add(new EnumValue<ProviderType>(ProviderType.LocalFileOASIS));
        providers.GetProviderAutoLoadBalanceList().Add(new EnumValue<ProviderType>(ProviderType.LocalFileOASIS));
        return (new HolonManager(null, dna, providers), providers, source, destination, replica, root);
    }
}
