using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public class AzureRecordHistoryTests
{
    private static Task<JObject> Stage<T>(T record, string? previous, bool enabled) where T : class, IHolonBase
        => (Task<JObject>)typeof(AzureStorageOASIS).GetMethod("StageBlobHistoryAsync", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(T)).Invoke(null, new object?[] { record, previous, enabled, null })!;

    private static async Task VerifyHistory<T>(T record) where T : class, IHolonBase
    {
        record.Id = Guid.NewGuid();
        var first = await Stage(record, null, true);
        Assert.AreEqual(0, record.Version, "Detached staging must not publish caller version.");
        Assert.AreEqual(Guid.Empty, record.VersionId);
        var current = OasisJson.DeserializeStored<T>(first.ToString());
        Assert.AreEqual(1, current.Version);
        var second = await Stage(record, first.ToString(), false);
        var next = OasisJson.DeserializeStored<T>(second.ToString());
        Assert.AreEqual(2, next.Version, "Disabling new history must not drop an existing archive.");
        Assert.AreEqual(current.VersionId, next.PreviousVersionId);
        Assert.AreNotEqual(current.VersionId, next.VersionId);
        var historical = await new EmbeddedRecordHistory().ReadAsync(record.Id, second, 1, OasisJson.DeserializeStored<T>);
        Assert.AreEqual(current.VersionId, historical.VersionId);
        Assert.AreEqual(0, record.Version);
    }

    [TestMethod] public Task HolonHistoryUsesSharedFormat() => VerifyHistory(new Holon());
    [TestMethod] public Task AvatarHistoryUsesSharedFormat() => VerifyHistory(new Avatar());
    [TestMethod] public Task DetailHistoryUsesSharedFormat() => VerifyHistory(new AvatarDetail());

    [TestMethod]
    public async Task DisabledHistoryDoesNotInventArchiveOrChangeCaller()
    {
        var record = new Holon { Id = Guid.NewGuid(), Version = 7, VersionId = Guid.NewGuid() };
        var staged = await Stage(record, null, false);
        Assert.IsNull(staged[EmbeddedRecordHistory.PropertyName]);
        Assert.AreEqual(7, OasisJson.DeserializeStored<Holon>(staged.ToString()).Version);
        Assert.AreEqual(record.VersionId, OasisJson.DeserializeStored<Holon>(staged.ToString()).VersionId);
    }

    [TestMethod]
    public async Task IdentityRebindingCannotReplaceExistingBlob()
    {
        var old = new Holon { Id = Guid.NewGuid() };
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Stage(new Holon { Id = Guid.NewGuid() }, OasisJson.Serialize(old), true));
    }

    [TestMethod]
    public async Task CorruptArchiveCannotBeDroppedWhenHistoryDisabled()
    {
        var record = new Holon { Id = Guid.NewGuid() };
        var previous = await Stage(record, null, true);
        previous[EmbeddedRecordHistory.PropertyName] = new JObject { ["schema"] = 99 };
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Stage(record, previous.ToString(), false));
    }

    [TestMethod]
    public async Task MalformedCanonicalDocumentHasTypedIntegrityCause()
    {
        var failure = await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Stage(new Holon { Id = Guid.NewGuid() }, "{ invalid", true));
        Assert.IsInstanceOfType(failure.InnerException, typeof(Newtonsoft.Json.JsonException));
    }

    [TestMethod]
    public async Task HistoryIdentityCannotBeSilentlyRepaired()
    {
        var record = new Holon { Id = Guid.NewGuid() };
        var previous = await Stage(record, null, true);
        previous["id"] = Guid.NewGuid().ToString();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Stage(record, previous.ToString(), true));
    }

    [TestMethod]
    public async Task ExhaustedVersionPreservesOriginalAndCaller()
    {
        var record = new Holon { Id = Guid.NewGuid(), Version = int.MaxValue, VersionId = Guid.NewGuid() };
        var previous = await Stage(record, null, true);
        var original = previous.ToString();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Stage(record, original, false));
        Assert.AreEqual(original, previous.ToString());
        Assert.AreEqual(int.MaxValue, record.Version);
    }
}
