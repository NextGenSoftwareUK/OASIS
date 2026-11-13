using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifySqliteOutageAsync(Web3CoreOASISBaseProvider chain, OASISDNA dna,
        EnumValue<ProviderType>[] providers, Guid holonId, string unavailablePath)
    {
        Assert.IsFalse(File.Exists(unavailablePath));
        using var unavailable = new SQLLiteDBOASIS.SQLLiteDBOASIS(
            $"Data Source={unavailablePath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        var manager = new ProviderManager(null, dna);
        manager.RegisterProvider(unavailable);
        manager.RegisterProvider(chain);
        var configured = manager.SetAndReplaceAutoFailOverListForProviders(providers);
        Assert.IsFalse(configured.IsError, configured.Message);
        var router = new OASISHyperDrive(manager);

        StorageOperationRequest ReadFromSqlite() => new()
        {
            Operation = "LoadHolon",
            HolonId = holonId,
            LoadChildren = false,
            PreferredProvider = ProviderType.SQLLiteDBOASIS
        };

        foreach (var synchronous in new[] { true, false })
        {
            manager.IsAutoFailOverEnabled = false;
            var rejected = synchronous
                ? router.RouteRequest<IHolon>(ReadFromSqlite())
                : await router.RouteRequestAsync<IHolon>(ReadFromSqlite());
            Assert.IsTrue(rejected.IsError, rejected.Message);

            manager.IsAutoFailOverEnabled = true;
            var recovered = synchronous
                ? router.RouteRequest<IHolon>(ReadFromSqlite())
                : await router.RouteRequestAsync<IHolon>(ReadFromSqlite());
            Assert.IsFalse(recovered.IsError, recovered.Message);
            Assert.IsTrue(recovered.IsLoaded);
            Assert.AreEqual(holonId, recovered.Result.Id);
            Assert.AreEqual("replicated holon", recovered.Result.Name);
            Assert.AreEqual("owned-value", recovered.Result.MetaData["owned-field"].ToString());
            using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
            Assert.AreEqual(ProviderType.SQLLiteDBOASIS.ToString(),
                route.RootElement.GetProperty("RequestedProvider").GetString());
            Assert.AreEqual(chain.ProviderType.Value.ToString(),
                route.RootElement.GetProperty("ServingProvider").GetString());
        }

        Assert.IsFalse(File.Exists(unavailablePath));
    }
}
