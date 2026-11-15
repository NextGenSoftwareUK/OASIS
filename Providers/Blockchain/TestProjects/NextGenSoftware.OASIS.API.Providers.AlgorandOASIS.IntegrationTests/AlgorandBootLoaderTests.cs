using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using System.Globalization;
using System.Security.Cryptography;
using BootLoader = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class AlgorandBootLoaderTests
{
    [TestMethod]
    public async Task RegistersAndUsesTheEnrolledAlgorandProviderOnLocalNet()
    {
        const string tokenVariable = "OASIS_ALGORAND_RECORD_LOCALNET_TOKEN";
        Assert.IsFalse(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(tokenVariable)));
        var settings = new AlgorandOASISProviderSettings
        {
            AlgodUrl = "http://127.0.0.1:4001",
            IndexerUrl = "http://127.0.0.1:8980",
            AlgodTokenEnvironmentVariable = tokenVariable,
            RecordStorage = new AlgorandRecordStorageSettings
            {
                ApplicationId = ulong.Parse(Required("APP"), CultureInfo.InvariantCulture),
                GenesisHash = Required("GENESIS"),
                WriterAddress = Required("WRITER"),
                ApprovalProgramSha256 = Required("PROGRAM"),
                StorageKeyEnvironmentVariable = "OASIS_ALGORAND_RECORD_KEY",
                WriterSeedEnvironmentVariable = "OASIS_ALGORAND_RECORD_WRITER_SEED"
            }
        };
        var dna = new OASISDNA();
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.OASISHyperDriveConfig = new OASISHyperDriveConfig();
        dna.OASIS.Security = new SecuritySettings { SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };
        dna.OASIS.Logging = new LoggingSettings { LoggingFramework = "Default", LogToFile = false };
        dna.OASIS.ErrorHandling = new ErrorHandlingSettings();
        dna.OASIS.StorageProviders = new StorageProviderSettings
        {
            OASISProviderBootType = "Warm",
            AutoFailOverProviders = nameof(ProviderType.AlgorandOASIS),
            AlgorandOASIS = settings
        };

        var boot = await BootLoader.BootOASISForAdmittedStorageAsync(dna);
        Assert.IsFalse(boot.IsError, boot.Message);
        Assert.IsTrue(boot.Result, boot.Message);
        var provider = ProviderManager.Instance.GetAllRegisteredProviders().OfType<AlgorandOASIS>().Single();
        Assert.IsFalse(provider.IsProviderActivated, "Warm boot unexpectedly opened a network connection.");
        try
        {
            var active = await BootLoader.GetAndActivateStorageProviderAsync(ProviderType.AlgorandOASIS);
            Assert.IsFalse(active.IsError, active.Message);
            Assert.AreSame(provider, active.Result);
            var record = new Holon { Id = Guid.NewGuid(), Name = "BootLoader native Algorand write", Version = 1, VersionId = Guid.NewGuid() };
            var written = await provider.SaveHolonAsync(record, saveChildren: false);
            Assert.IsFalse(written.IsError, written.Exception?.ToString() ?? written.Message);
            var read = await provider.LoadHolonAsync(record.Id, loadChildren: false);
            Assert.IsFalse(read.IsError, read.Exception?.ToString() ?? read.Message);
            Assert.AreEqual(record.Name, read.Result.Name);
            Assert.AreEqual(record.VersionId, read.Result.VersionId);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static string Required(string field) => Environment.GetEnvironmentVariable("OASIS_ALGORAND_RECORD_" + field)
        ?? throw new InvalidOperationException("The owned Algorand fixture did not provide " + field + ".");
}
