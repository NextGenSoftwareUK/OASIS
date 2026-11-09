using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.UnitTests;

[TestClass]
public class SurrealDbSaveOwnershipTests
{
    // Unsupported protocol fails client construction without a network dependency.
    private static SurrealDBOASIS RejectedConnection()
        => new("invalid://no-network", "test", "test");

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedWritePreservesCallerIdentityAndProviderKeys(bool existingIdentity)
    {
        var row = new Holon { Id = existingIdentity ? Guid.NewGuid() : Guid.Empty, IsNewHolon = true };
        row.ProviderUniqueStorageKey[ProviderType.SurrealDBOASIS] = "original-key";
        row.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "other-provider";
        var originalId = row.Id;
        var originalKeys = row.ProviderUniqueStorageKey;

        var result = await RejectedConnection().SaveHolonAsync(row, saveChildren: false);

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(originalId, row.Id);
        Assert.AreSame(originalKeys, row.ProviderUniqueStorageKey);
        Assert.AreEqual("original-key", row.ProviderUniqueStorageKey[ProviderType.SurrealDBOASIS]);
        Assert.AreEqual("other-provider", row.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
        Assert.IsTrue(row.IsNewHolon);
    }

    [DataTestMethod]
    [DataRow(false, 1)]
    [DataRow(true, 2)]
    public async Task BatchStopsOrContinuesExactlyAsRequested(bool continueOnError, int expectedAttempts)
    {
        var attempts = 0;
        IEnumerable<IHolon> Source()
        {
            attempts++;
            yield return new Holon { Id = Guid.Empty };
            attempts++;
            yield return new Holon { Id = Guid.Empty };
        }

        var result = await RejectedConnection().SaveHolonsAsync(Source(),
            saveChildren: false, continueOnError: continueOnError);

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(expectedAttempts, result.ErrorCount);
        Assert.AreEqual(expectedAttempts, attempts);
    }
}
