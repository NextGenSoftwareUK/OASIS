using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class ProviderRoutingPolicyTests
{
    [Fact]
    public async Task CapturedMissingDefaultCannotBorrowALaterGlobalWriteAuthority()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var source = new ProviderManager(null, dna);
        var captured = source.CaptureRoutingPolicy().Result;
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        source.RegisterProvider(provider.Object).Should().BeTrue();
        source.PublishRoutingOverrides(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS).IsError.Should().BeFalse();
        var manager = new HolonManager(null, dna, source);

        var context = manager.CreateRequestContext(captured.ToOverrides());
        context.IsError.Should().BeFalse(context.Message + context.Exception);
        context.Result.Policy.PreferredProvider.Should().Be(ProviderType.Default);
        var written = await context.Result.Manager.SaveOwnedHolonAsync(new NextGenSoftware.OASIS.API.Core.Holons.Holon(),
            Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);

        written.IsError.Should().BeTrue();
        provider.Invocations.Where(call => call.Method.Name == "ActivateProvider" || call.Method.Name == "ActivateProviderAsync" ||
            call.Method.Name.Contains("LoadHolon") || call.Method.Name.Contains("SaveHolon")).Should().BeEmpty();
        source.CurrentStorageProvider.Should().BeSameAs(provider.Object);
    }

    [Fact]
    public void ExplicitFullPolicyPublicationCanClearAnObservedDefaultWithoutInconsistentMetadata()
    {
        var source = new ProviderManager(null, new OASISDNA());
        var emptyDefault = source.CaptureRoutingPolicy().Result;
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        source.RegisterProvider(provider.Object).Should().BeTrue();
        source.PublishRoutingOverrides(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS).IsError.Should().BeFalse();

        var published = source.PublishRoutingOverrides(emptyDefault.ToOverrides());

        published.IsError.Should().BeFalse(published.Message + published.Exception);
        published.Result.PreferredProvider.Should().Be(ProviderType.Default);
        source.CurrentStorageProviderType.Value.Should().Be(ProviderType.Default);
        source.CurrentStorageProvider.Should().BeNull();
        source.DefaultGlobalStorageProvider.Should().BeNull();
    }

    [Fact]
    public void CapturedPreferredAuthorityCannotBeReplacedDuringManagerConstruction()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var source = new ProviderManager(null, dna);
        foreach (var type in new[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS })
        {
            var provider = new Mock<IOASISStorageProvider>();
            provider.SetupAllProperties();
            provider.Object.ProviderType = new EnumValue<ProviderType>(type);
            provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
            source.RegisterProvider(provider.Object).Should().BeTrue();
        }
        source.PublishRoutingOverrides(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS).IsError.Should().BeFalse();
        var captured = source.ResolveRoutingOverrides(new ProviderRoutingOverrides()).Result;
        source.PublishRoutingOverrides(new ProviderRoutingOverrides(), ProviderType.MongoDBOASIS).IsError.Should().BeFalse();
        var manager = new HolonManager(null, dna, source);

        var request = manager.CreateRequestContext(captured.ToOverrides());

        request.IsError.Should().BeFalse(request.Message + request.Exception);
        request.Result.Policy.PreferredProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
        source.CurrentStorageProviderType.Value.Should().Be(ProviderType.MongoDBOASIS);
    }

    [Fact]
    public void MissingRuntimeDnaCannotSilentlyLoadAnotherConfigurationForAScope()
    {
        var source = new ProviderManager(null, new OASISDNA());
        source.OASISDNA = null!;

        var scoped = source.CreateRoutingScope(new ProviderRoutingOverrides());

        scoped.IsError.Should().BeTrue();
        scoped.Exception.Should().BeOfType<InvalidOperationException>();
        source.CurrentStorageProvider.Should().BeNull();
    }

    [Fact]
    public void LocalResolutionDoesNotPublishAndCanBeReboundWithoutChangingInheritance()
    {
        var source = new ProviderManager(null, new OASISDNA());
        var resolved = source.ResolveRoutingOverrides(new ProviderRoutingOverrides(autoReplicationEnabled: false));
        resolved.IsError.Should().BeFalse();
        source.IsAutoReplicationEnabled.Should().BeTrue();
        source.PublishRoutingOverrides(new ProviderRoutingOverrides(autoFailoverEnabled: false));
        var scope = source.CreateRoutingScope(resolved.Result.ToOverrides());

        scope.IsError.Should().BeFalse();
        scope.Result.IsAutoReplicationEnabled.Should().BeFalse();
        scope.Result.IsAutoFailOverEnabled.Should().BeTrue();
        source.IsAutoFailOverEnabled.Should().BeFalse();
    }

    [Fact]
    public void MissingGlobalPreferredProviderRejectsAllChangesBeforePublication()
    {
        var source = new ProviderManager(null, new OASISDNA());

        var rejected = source.PublishRoutingOverrides(new ProviderRoutingOverrides(
            autoFailoverEnabled: false, autoReplicationEnabled: false), ProviderType.SQLLiteDBOASIS);

        rejected.IsError.Should().BeTrue();
        source.IsAutoFailOverEnabled.Should().BeTrue();
        source.IsAutoReplicationEnabled.Should().BeTrue();
        source.CurrentStorageProvider.Should().BeNull();
    }

    [Fact]
    public void GlobalPreferencePublicationDoesNotActivateItsRegisteredProvider()
    {
        var source = new ProviderManager(null, new OASISDNA());
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        source.RegisterProvider(provider.Object).Should().BeTrue();

        var published = source.PublishRoutingOverrides(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS);

        published.IsError.Should().BeFalse(published.Message + published.Exception);
        source.CurrentStorageProvider.Should().BeSameAs(provider.Object);
        source.DefaultGlobalStorageProvider.Should().BeSameAs(provider.Object);
        provider.Object.IsProviderActivated.Should().BeFalse();
        provider.Invocations.Where(call => call.Method.Name == "ActivateProvider" || call.Method.Name == "ActivateProviderAsync").Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScopedReadsKeepConfiguredAffinityUnlessAnExplicitProviderWins(bool explicitProvider)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var registry = new ProviderManager(null, dna);
        var id = Guid.NewGuid();
        Mock<IOASISStorageProvider> Store(ProviderType type)
        {
            var provider = new Mock<IOASISStorageProvider>();
            provider.SetupAllProperties();
            provider.Object.ProviderType = new EnumValue<ProviderType>(type);
            provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
            provider.Object.IsProviderActivated = true;
            provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new NextGenSoftware.OASIS.Common.OASISResult<IHolon>(new NextGenSoftware.OASIS.API.Core.Holons.Holon
                { Id = id, Name = type.ToString(), IsActive = true }) { IsLoaded = true });
            registry.RegisterProvider(provider.Object).Should().BeTrue();
            return provider;
        }
        var configured = Store(ProviderType.SQLLiteDBOASIS);
        var other = Store(ProviderType.MongoDBOASIS);
        var manager = new HolonManager(configured.Object, dna, registry);
        var context = manager.CreateRequestContext(new ProviderRoutingOverrides(autoLoadBalancingEnabled: true,
            loadBalancingProviders: new[] { ProviderType.MongoDBOASIS }),
            explicitProvider ? ProviderType.MongoDBOASIS : ProviderType.Default);

        context.IsError.Should().BeFalse(context.Message + context.Exception);
        var loaded = await context.Result.Manager.LoadHolonAsync(id, loadChildren: false);

        loaded.IsError.Should().BeFalse(loaded.Message + loaded.Exception);
        loaded.Result.Name.Should().Be((explicitProvider ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS).ToString());
        (explicitProvider ? configured : other).Invocations.Where(call => call.Method.Name == "LoadHolonAsync").Should().BeEmpty();
        registry.CurrentStorageProvider.Should().BeNull();
    }

    [Fact]
    public void RequestManagerRetainsConfiguredAuthorityWithoutActivationOrPersistentEventHandlers()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var registry = new ProviderManager(null, dna);
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = true;
        var manager = new HolonManager(provider.Object, dna, registry);
        var subscriptions = provider.Invocations.Count(call => call.Method.Name.StartsWith("add_"));
        provider.Object.IsProviderActivated = false;

        var context = manager.CreateRequestContext(new ProviderRoutingOverrides(autoReplicationEnabled: false));

        context.IsError.Should().BeFalse(context.Message + context.Exception);
        context.Result.Policy.PreferredProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
        context.Result.Policy.AutoReplicationEnabled.Should().BeFalse();
        context.Result.Manager.Should().NotBeSameAs(manager);
        registry.IsAutoReplicationEnabled.Should().BeTrue();
        registry.CurrentStorageProvider.Should().BeNull();
        provider.Invocations.Count(call => call.Method.Name.StartsWith("add_")).Should().Be(subscriptions);
        provider.Object.IsProviderActivated.Should().BeFalse();
        provider.Invocations.Where(call => call.Method.Name == "ActivateProvider" || call.Method.Name == "ActivateProviderAsync").Should().BeEmpty();
    }

    [Fact]
    public void RequestRegistryOwnsItsSettingsAndListsWithoutChangingTheSource()
    {
        var source = new ProviderManager(null, new OASISDNA());
        var scoped = source.CreateRoutingScope(new ProviderRoutingOverrides(autoFailoverEnabled: false,
            failoverProviders: new[] { ProviderType.SQLLiteDBOASIS }));

        scoped.IsError.Should().BeFalse(scoped.Message + scoped.Exception);
        scoped.Result.IsAutoFailOverEnabled.Should().BeFalse();
        source.IsAutoFailOverEnabled.Should().BeTrue();
        scoped.Result.GetProviderAutoFailOverList().Clear();
        source.GetProviderAutoFailOverList().Should().BeEmpty();
        scoped.Result.PerformanceMonitor.Should().BeSameAs(source.PerformanceMonitor);
        source.PublishRoutingOverrides(new ProviderRoutingOverrides(autoReplicationEnabled: false));
        scoped.Result.IsAutoReplicationEnabled.Should().BeTrue();
    }

    [Fact]
    public void RequestRegistriesShareRoundRobinHistoryRatherThanRestartingEveryRequest()
    {
        var source = new ProviderManager(null, new OASISDNA());
        var changes = new ProviderRoutingOverrides(autoLoadBalancingEnabled: true,
            loadBalancingProviders: new[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS });
        var first = source.CreateRoutingScope(changes);
        var second = source.CreateRoutingScope(changes);
        first.IsError.Should().BeFalse(first.Message + first.Exception);
        second.IsError.Should().BeFalse(second.Message + second.Exception);

        var firstProvider = first.Result.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value;
        var secondProvider = second.Result.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value;

        new[] { firstProvider, secondProvider }.Should().BeEquivalentTo(new[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS });
        source.CurrentStorageProvider.Should().BeNull();
    }

    [Fact]
    public void AnUnregisteredPreferredProviderCannotBeActivatedByPolicyCapture()
    {
        var source = new ProviderManager(null, new OASISDNA());

        var scoped = source.CreateRoutingScope(new ProviderRoutingOverrides(), ProviderType.SQLLiteDBOASIS);

        scoped.IsError.Should().BeTrue();
        scoped.Exception.Should().BeOfType<InvalidOperationException>();
        source.GetAllRegisteredProviders().Should().BeEmpty();
        source.CurrentStorageProvider.Should().BeNull();
    }

    [Fact]
    public void ExplicitPublicationPreservesOldSnapshotsAndDoesNotActivateProviders()
    {
        var manager = new ProviderManager(null, new OASISDNA());
        var before = manager.CaptureRoutingPolicy();
        before.IsError.Should().BeFalse();
        var changed = manager.PublishRoutingOverrides(new ProviderRoutingOverrides(
            autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false,
            failoverProviders: new[] { ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS }));

        changed.IsError.Should().BeFalse(changed.Message);
        changed.Result.AutoFailoverEnabled.Should().BeFalse();
        changed.Result.FailoverProviders.Should().Equal(ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS);
        before.Result.AutoFailoverEnabled.Should().BeTrue();
        before.Result.FailoverProviders.Should().BeEmpty();
        manager.CurrentStorageProvider.Should().BeNull();
        manager.GetAllRegisteredProviders().Should().BeEmpty();
    }

    [Fact]
    public void RejectedPublicationDoesNotChangeAnyGlobalPolicy()
    {
        var manager = new ProviderManager(null, new OASISDNA());
        var before = manager.CaptureRoutingPolicy().Result;

        var rejected = manager.PublishRoutingOverrides(null!);

        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().BeOfType<ArgumentNullException>();
        var after = manager.CaptureRoutingPolicy().Result;
        after.AutoReplicationEnabled.Should().Be(before.AutoReplicationEnabled);
        after.AutoFailoverEnabled.Should().Be(before.AutoFailoverEnabled);
        after.AutoLoadBalancingEnabled.Should().Be(before.AutoLoadBalancingEnabled);
        after.PreferredProvider.Should().Be(before.PreferredProvider);
        after.ReplicationProviders.Should().Equal(before.ReplicationProviders);
    }

    [Fact]
    public async Task ConcurrentCapturesObserveOneCompletePublication()
    {
        var manager = new ProviderManager(null, new OASISDNA());
        var writer = Task.Run(() =>
        {
            for (var index = 0; index < 200; index++)
            {
                var enabled = index % 2 == 0;
                manager.PublishRoutingOverrides(new ProviderRoutingOverrides(enabled, enabled, enabled))
                    .IsError.Should().BeFalse();
            }
        });
        var reader = Task.Run(() =>
        {
            for (var index = 0; index < 200; index++)
            {
                var captured = manager.CaptureRoutingPolicy();
                captured.IsError.Should().BeFalse();
                captured.Result.AutoFailoverEnabled.Should().Be(captured.Result.AutoReplicationEnabled);
                captured.Result.AutoLoadBalancingEnabled.Should().Be(captured.Result.AutoReplicationEnabled);
            }
        });
        await Task.WhenAll(writer, reader);
    }

    [Fact]
    public void CapturedListsCannotBeChangedThroughInputsOrPublishedCollections()
    {
        var input = new List<ProviderType> { ProviderType.SQLLiteDBOASIS };
        var changes = new ProviderRoutingOverrides(replicationProviders: input);
        input.Clear();

        changes.ReplicationProviders.Should().Equal(ProviderType.SQLLiteDBOASIS);
        Action mutate = () => ((IList<ProviderType>)changes.ReplicationProviders).Clear();
        mutate.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void NullInheritsWhileExplicitEmptyClearsOnlyItsOwnList()
    {
        var original = Policy();
        var effective = original.Apply(new ProviderRoutingOverrides(autoReplicationEnabled: false,
            failoverProviders: Array.Empty<ProviderType>()), ProviderType.Default);

        effective.AutoFailoverEnabled.Should().BeTrue();
        effective.AutoReplicationEnabled.Should().BeFalse();
        effective.AutoLoadBalancingEnabled.Should().BeTrue();
        effective.PreferredProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
        effective.FailoverProviders.Should().BeEmpty();
        effective.ReplicationProviders.Should().Equal(ProviderType.MongoDBOASIS);
        effective.LoadBalancingProviders.Should().Equal(ProviderType.Neo4jOASIS);
        original.AutoReplicationEnabled.Should().BeTrue();
        original.FailoverProviders.Should().Equal(ProviderType.SQLLiteDBOASIS);
    }

    [Fact]
    public void ExplicitPreferredProviderWinsWithoutChangingTheCapturedDefault()
    {
        var original = Policy();
        var effective = original.Apply(new ProviderRoutingOverrides(), ProviderType.MongoDBOASIS);

        effective.PreferredProvider.Should().Be(ProviderType.MongoDBOASIS);
        original.PreferredProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
    }

    [Theory]
    [InlineData(ProviderType.Default)]
    [InlineData(ProviderType.None)]
    [InlineData(ProviderType.All)]
    [InlineData((ProviderType)(-1))]
    public void ProviderListsRequireConcreteDefinedIdentities(ProviderType invalid)
    {
        Action capture = () => new ProviderRoutingOverrides(failoverProviders: new[] { invalid });
        capture.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DuplicateProviderIdentitiesAreRejectedInsteadOfReorderedOrDeduplicated()
    {
        Action capture = () => new ProviderRoutingOverrides(replicationProviders:
            new[] { ProviderType.MongoDBOASIS, ProviderType.MongoDBOASIS });
        capture.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnboundedInputIsStoppedAtTheAdmissionLimit()
    {
        var visited = 0;
        IEnumerable<ProviderType> Infinite()
        {
            while (true)
            {
                visited++;
                if (visited > 257) throw new InvalidOperationException("Enumeration exceeded its bound.");
                yield return ProviderType.SQLLiteDBOASIS;
            }
        }

        Action capture = () => new ProviderRoutingOverrides(loadBalancingProviders: Infinite());
        capture.Should().Throw<ArgumentException>();
        visited.Should().Be(257);
    }

    [Fact]
    public void NullOverridesCannotSilentlySelectAnUnspecifiedPolicy()
    {
        Action apply = () => Policy().Apply(null!, ProviderType.Default);
        apply.Should().Throw<ArgumentNullException>();
    }

    private static ProviderRoutingPolicy Policy() => new(true, true, true,
        ProviderType.SQLLiteDBOASIS, new[] { ProviderType.SQLLiteDBOASIS },
        new[] { ProviderType.MongoDBOASIS }, new[] { ProviderType.Neo4jOASIS });
}
