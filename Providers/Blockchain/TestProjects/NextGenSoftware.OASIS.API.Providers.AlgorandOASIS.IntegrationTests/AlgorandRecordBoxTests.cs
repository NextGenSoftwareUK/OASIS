using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandRecordBoxTests
{
    [TestMethod]
    public async Task ReadsEncryptedCurrentAndHistoricalRecordsFromNativeBoxes()
    {
        var id = Guid.Parse(Required("ID"));
        var appId = ulong.Parse(Required("APP"), CultureInfo.InvariantCulture);
        var key = Convert.FromBase64String(Required("KEY"));
        var program = Required("PROGRAM");
        AlgorandOASIS Create(string programDigest, byte[] storageKey) => new(
            "http://127.0.0.1:4001", "http://127.0.0.1:8980", new string('a', 64), null,
            new AlgorandRecordBoxSettings(appId, Required("GENESIS"), Required("WRITER"), programDigest, storageKey));

        var rejected = Create(new string('0', 64), key);
        Assert.IsTrue((await rejected.ActivateProviderAsync()).IsError);
        Assert.IsTrue((await rejected.LoadStoredHolonAsync(id)).IsError);

        var provider = Create(program, key);
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var latest = await provider.LoadStoredHolonAsync(id);
            Assert.IsFalse(latest.IsError, latest.Exception?.ToString() ?? latest.Message);
            Assert.AreEqual(id, latest.Result.Id);
            Assert.AreEqual(2, latest.Result.Version);
            Assert.AreEqual("Native version 2", latest.Result.Name);

            var prior = await provider.LoadStoredHolonAsync(id, version: 1);
            Assert.IsFalse(prior.IsError, prior.Exception?.ToString() ?? prior.Message);
            Assert.AreEqual(1, prior.Result.Version);
            Assert.AreEqual("Native version 1", prior.Result.Name);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(id, version: 3)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(Guid.NewGuid())).ErrorCode);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable("OASIS_ALGORAND_RECORD_" + name)
        ?? throw new InvalidOperationException($"Missing native record fixture setting {name}.");
}
