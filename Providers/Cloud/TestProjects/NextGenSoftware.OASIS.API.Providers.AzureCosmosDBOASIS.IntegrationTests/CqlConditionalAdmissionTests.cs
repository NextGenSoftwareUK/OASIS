using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlConditionalAdmissionTests
{
    private sealed class UnactivatedCosmos : AzureCosmosDBOASIS
    {
        public int ActivationCalls;
        public UnactivatedCosmos() : base(new Uri("https://127.0.0.1:8082"), "unused", "unused", new()) { }
        public override OASISResult<bool> ActivateProvider()
        {
            ActivationCalls++;
            throw new InvalidOperationException("Conditional rejection must precede activation.");
        }
        public override Task<OASISResult<bool>> ActivateProviderAsync()
        {
            ActivationCalls++;
            throw new InvalidOperationException("Conditional rejection must precede activation.");
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task UnsupportedCosmicConditionalSaveCannotDowngradeToOrdinaryCrud(bool synchronous)
    {
        var provider = new UnactivatedCosmos();
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        };
        var registry = new ProviderManager(null, dna)
        {
            IsAutoFailOverEnabled = true,
            IsAutoReplicationEnabled = true,
            IsAutoLoadBalanceEnabled = false
        };
        Assert.IsTrue(registry.RegisterProvider(provider));
        var manager = new HolonManager(null!, dna, registry);
        var source = new Holon { Name = "pending-cloud-demo" };
        var options = new CosmicWriteOptions(Guid.NewGuid());
        var result = synchronous
            ? manager.SaveHolon(source, Guid.NewGuid(), options, saveChildren: false, providerType: ProviderType.AzureCosmosDBOASIS)
            : await manager.SaveHolonAsync(source, Guid.NewGuid(), options, saveChildren: false, providerType: ProviderType.AzureCosmosDBOASIS);

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("COSMIC_CONDITIONAL_WRITE_UNSUPPORTED", result.ErrorCode);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(Guid.Empty, source.Id);
        Assert.AreEqual(0, source.Version);
        Assert.AreEqual("pending-cloud-demo", source.Name);
        Assert.AreEqual(0, provider.ActivationCalls);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsFalse((object)provider is IConditionalHolonStorageProvider);
    }
}
