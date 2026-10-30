using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS.UnitTests;

[TestClass]
public class QdrantSaveOwnershipTests
{
    private sealed class Writer : QdrantOASIS
    {
        public Func<Guid, string, Task<ulong>> Write = (_, _) => Task.FromResult(1UL);
        public Writer() : base("http://127.0.0.1:1") { }
        protected override Task<ulong> PersistHolonAsync(Guid id, Guid parentId, int type, bool deleted, string json)
            => Write(id, json);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedAcknowledgementPreservesCaller(bool existing)
    {
        var holon = new Holon { Id = existing ? Guid.NewGuid() : Guid.Empty, Name = "unsaved" };
        var id = holon.Id;
        var keys = holon.ProviderUniqueStorageKey;
        var before = keys == null ? null : new Dictionary<ProviderType, string>(keys);
        var writer = new Writer { Write = (_, _) => throw new IOException("Injected failed acknowledgement") };

        var result = await writer.SaveHolonAsync(holon, saveChildren: false);

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(id, holon.Id);
        Assert.AreSame(keys, holon.ProviderUniqueStorageKey);
        if (before != null) CollectionAssert.AreEquivalent(before.ToArray(), keys!.ToArray());
    }

    [TestMethod]
    public async Task IdentityPublishesOnlyAfterAcknowledgementAndPayloadMatches()
    {
        var holon = new Holon { Name = "coffee" };
        var completion = new TaskCompletionSource<ulong>(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid staged = Guid.Empty;
        var writer = new Writer { Write = (id, json) =>
        {
            staged = id;
            using var payload = JsonDocument.Parse(json);
            Assert.AreEqual(id, payload.RootElement.GetProperty("id").GetGuid());
            Assert.AreEqual(id.ToString("D"), payload.RootElement.GetProperty("providerUniqueStorageKey")
                .GetProperty("QdrantOASIS").GetString());
            return completion.Task;
        }};

        var save = writer.SaveHolonAsync(holon, saveChildren: false);
        Assert.AreEqual(Guid.Empty, holon.Id);
        Assert.IsFalse(save.IsCompleted);
        completion.SetResult(ulong.MaxValue);
        var result = await save;
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.IsSaved);
        Assert.AreEqual(1, result.SavedCount);
        Assert.AreEqual("18446744073709551615", result.MetaData["Qdrant.OperationId"]);
        Assert.AreEqual(staged, holon.Id);
        Assert.AreSame(holon, result.Result);
    }

    [TestMethod]
    public async Task ZeroNativeOperationIdIsAValidCompletedWrite()
    {
        var row = new Holon();
        var writer = new Writer { Write = (_, _) => Task.FromResult(0UL) };
        var result = await writer.SaveHolonAsync(row, saveChildren: false);
        Assert.IsFalse(result.IsError); Assert.IsTrue(result.IsSaved);
        Assert.AreNotEqual(Guid.Empty, row.Id); Assert.AreEqual(1, result.SavedCount);
        Assert.AreEqual("0", result.MetaData["Qdrant.OperationId"]);
    }

    [DataTestMethod]
    [DataRow(false, 1, 0)]
    [DataRow(true, 3, 2)]
    public async Task BatchHonoursStopAndAcknowledgedCounts(bool keepGoing, int attempts, int saves)
    {
        var count = 0;
        var writer = new Writer { Write = (_, _) => ++count == 1
            ? Task.FromException<ulong>(new IOException("Injected first write failure")) : Task.FromResult(1UL) };
        var result = await writer.SaveHolonsAsync(new[] { new Holon(), new Holon(), new Holon() },
            saveChildren: false, continueOnError: keepGoing);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(attempts, count);
        Assert.AreEqual(saves, result.SavedCount);
        Assert.AreEqual(saves != 0, result.IsSaved);
        Assert.AreEqual(saves, result.Result.Count());
    }
}
