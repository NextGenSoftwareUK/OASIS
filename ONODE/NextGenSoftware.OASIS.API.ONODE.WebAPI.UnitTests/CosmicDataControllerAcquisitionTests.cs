using System.Reflection;
using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Boot = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[Collection("COSMIC hosted global state")]
public sealed class CosmicDataControllerAcquisitionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryV2AcquisitionPreservesDefaultBalancingAndExplicitProviderPrecedence(bool explicitProvider)
    {
        Assert.Null(PreparedStorageExecution.Current);
        var singleton = typeof(ProviderManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)!;
        var previous = singleton.GetValue(null);
        var previousDna = Boot.OASISDNA;
        var id = Guid.NewGuid();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var registry = new ProviderManager(null, dna);
        var mongo = Provider(ProviderType.MongoDBOASIS, id);
        var sqlite = Provider(ProviderType.SQLLiteDBOASIS, id);
        registry.RegisterProvider(mongo.Object); registry.RegisterProvider(sqlite.Object);
        var selected = registry.SetAndActivateCurrentStorageProvider(mongo.Object);
        Assert.False(selected.IsError, selected.Message);
        try
        {
            singleton.SetValue(null, registry); Boot.OASISDNA = dna;
            // Exercise the actual parameterless MVC controller's lazy property,
            // not its trusted injected-manager constructor used by older tests.
            var controller = new DataController();
            var property = typeof(DataController).GetProperty("HolonManager", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var manager = Assert.IsType<HolonManager>(property.GetValue(controller));
            var requested = explicitProvider ? ProviderType.MongoDBOASIS : ProviderType.Default;
            var context = manager.CreateRequestContext(new ProviderRoutingOverrides(
                autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: true,
                loadBalancingProviders: new[] { ProviderType.SQLLiteDBOASIS }), requested);
            Assert.False(context.IsError, context.Message);
            var result = await context.Result.Manager.LoadHolonAsync(id, loadChildren: false);
            Assert.False(result.IsError, result.Message);
            using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
            Assert.Equal(requested.ToString(), route.RootElement.GetProperty("RequestedProvider").GetString());
            var serving = explicitProvider ? "MongoDBOASIS" : "SQLLiteDBOASIS";
            Assert.Equal(serving, route.RootElement.GetProperty("InitialProvider").GetString());
            Assert.Equal(serving, route.RootElement.GetProperty("ServingProvider").GetString());
            mongo.Verify(p => p.ActivateProviderAsync(), Times.Never);
            sqlite.Verify(p => p.ActivateProviderAsync(), Times.Never);
        }
        finally { singleton.SetValue(null, previous); Boot.OASISDNA = previousDna; }
    }

    private static Mock<IOASISStorageProvider> Provider(ProviderType type, Guid id)
    {
        var provider = new Mock<IOASISStorageProvider>(); provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(type);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = true;
        provider.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(p => p.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { Id = id, Name = "original", Version = 1 }) { IsLoaded = true });
        return provider;
    }
}
