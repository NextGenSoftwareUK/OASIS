using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandRecordBoxTests
{
    [TestMethod]
    public async Task PurgesPublishedRevisionsAndFencesRepublishing()
    {
        var id = Guid.Parse(Required("ID"));
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            using var node = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:4001") };
            node.DefaultRequestHeaders.Add("X-Algo-API-Token", new string('a', 64));
            var appId = ulong.Parse(Required("APP"), CultureInfo.InvariantCulture);
            async Task<string[]> NativeBoxNames()
            {
                var response = await node.GetStringAsync($"/v2/applications/{appId}/boxes");
                using var document = JsonDocument.Parse(response);
                return document.RootElement.GetProperty("boxes").EnumerateArray()
                    .Select(box => box.GetProperty("name").GetString()!).ToArray();
            }

            var unrelatedId = Guid.NewGuid();
            var unrelated = await provider.SaveStoredHolonAsync(new Holon { Id = unrelatedId, Name = "Keep this record" }, 0);
            Assert.IsFalse(unrelated.IsError, unrelated.Exception?.ToString() ?? unrelated.Message);
            var unrelatedBoxes = await NativeBoxNames();

            var record = new Holon { Id = id, Name = "First", Description = new string('x', 850) };
            var first = await provider.SaveStoredHolonAsync(record, 0);
            Assert.IsFalse(first.IsError, first.Exception?.ToString() ?? first.Message);
            record.Name = "Second";
            var second = await provider.SaveStoredHolonAsync(record, 1);
            Assert.IsFalse(second.IsError, second.Exception?.ToString() ?? second.Message);
            Assert.IsTrue((await NativeBoxNames()).Length > unrelatedBoxes.Length + 1);

            var deleted = await provider.DeleteHolonAsync(id.ToString("D"));
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(1, deleted.DeletedCount);
            Assert.AreEqual("Second", deleted.Result.Name);
            var opaqueKey = HMACSHA256.HashData(storageKey, Encoding.UTF8.GetBytes($"oasis:algorand:box:v1:holon:{id:D}"));
            var fenceName = Convert.ToBase64String([(byte)'h', ..opaqueKey]);
            CollectionAssert.AreEquivalent(unrelatedBoxes.Append(fenceName).ToArray(), await NativeBoxNames());
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await provider.LoadStoredHolonAsync(id)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(id, version: 1)).ErrorCode);
            Assert.AreEqual(unrelatedId, (await provider.DiscoverStoredHolonIdsAsync()).Result.Single());
            Assert.IsTrue((await provider.SaveStoredHolonAsync(record, 0)).IsError);
            var repeated = await provider.DeleteHolonAsync(id);
            Assert.IsFalse(repeated.IsError, repeated.Message);
            Assert.IsTrue(repeated.IsDeleted);
            Assert.AreEqual(0, repeated.DeletedCount);
            CollectionAssert.AreEquivalent(unrelatedBoxes.Append(fenceName).ToArray(), await NativeBoxNames());
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task SoftDeletesStoredHolonWithDurableAuditAndHistory()
    {
        var id = Guid.Parse(Required("ID"));
        var actor = Guid.NewGuid();
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var first = await provider.SaveStoredHolonAsync(new Holon { Id = id, Name = "Owned soft delete", IsActive = true }, 0);
            Assert.IsFalse(first.IsError, first.Exception?.ToString() ?? first.Message);
            Assert.IsTrue((await provider.SoftDeleteStoredHolonAsync(id, Guid.Empty)).IsError);

            var deleted = await provider.SoftDeleteStoredHolonAsync(id, actor);
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(1, deleted.DeletedCount);
            Assert.AreEqual(2, deleted.Result.Version);
            Assert.AreEqual(first.Result.VersionId, deleted.Result.PreviousVersionId);

            var repeated = await provider.SoftDeleteStoredHolonAsync(id, Guid.NewGuid());
            Assert.IsFalse(repeated.IsError, repeated.Message);
            Assert.AreEqual(0, repeated.DeletedCount);
            Assert.AreEqual(actor, repeated.Result.DeletedByAvatarId);
            Assert.AreEqual(deleted.Result.VersionId, repeated.Result.VersionId);

            var reader = Create(Required("PROGRAM"), storageKey);
            Assert.IsFalse((await reader.ActivateProviderAsync()).IsError);
            try
            {
                var current = await reader.LoadStoredHolonAsync(id);
                Assert.IsFalse(current.IsError, current.Message);
                Assert.IsTrue(current.Result.IsDeleted);
                Assert.IsFalse(current.Result.IsActive);
                Assert.AreEqual(actor, current.Result.DeletedByAvatarId);
                var original = await reader.LoadStoredHolonAsync(id, version: 1);
                Assert.IsFalse(original.IsError, original.Message);
                Assert.IsFalse(original.Result.IsDeleted);
                Assert.IsTrue(original.Result.IsActive);
            }
            finally
            {
                await reader.DeActivateProviderAsync();
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task PublishesSignedEncryptedRevisionsThroughNativeBoxes()
    {
        var id = Guid.Parse(Required("ID"));
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var holon = new Holon { Id = id, Version = 1, Name = "Signed native version one", Description = new string('b', 950) };
            var initial = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(initial.IsError, initial.Exception?.ToString() ?? initial.Message);
            Assert.AreEqual(1, initial.Result.Version);

            holon.Name = "Signed native version two";
            Assert.IsTrue((await provider.SaveHolonAsync(holon, saveChildren: false)).IsError);
            holon.Version = 2;
            var update = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(update.IsError, update.Exception?.ToString() ?? update.Message);
            Assert.AreEqual(2, update.Result.Version);
            Assert.AreEqual(initial.Result.VersionId, update.Result.PreviousVersionId);
            Assert.AreNotEqual(initial.Result.VersionId, update.Result.VersionId);
            var secondId = Guid.NewGuid();
            var second = await provider.SaveHolonAsync(new Holon
                { Id = secondId, ParentHolonId = id, Name = "Another record" }, saveChildren: false);
            Assert.IsFalse(second.IsError, second.Exception?.ToString() ?? second.Message);

            var separateReader = Create(Required("PROGRAM"), storageKey);
            Assert.IsFalse((await separateReader.ActivateProviderAsync()).IsError);
            try
            {
                var latest = await separateReader.LoadHolonAsync(id, loadChildren: false);
                Assert.IsFalse(latest.IsError, latest.Exception?.ToString() ?? latest.Message);
                Assert.AreEqual("Signed native version two", latest.Result.Name);
                Assert.AreEqual(950, latest.Result.Description.Length);
                var original = separateReader.LoadHolon(id.ToString("D"), loadChildren: false, version: 1);
                Assert.IsFalse(original.IsError, original.Exception?.ToString() ?? original.Message);
                Assert.AreEqual("Signed native version one", original.Result.Name);
                var discovered = await separateReader.DiscoverStoredHolonIdsAsync();
                Assert.IsFalse(discovered.IsError, discovered.Exception?.ToString() ?? discovered.Message);
                CollectionAssert.AreEquivalent(new[] { id, secondId }, System.Linq.Enumerable.ToArray(discovered.Result));
                var all = await separateReader.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(all.IsError, all.Exception?.ToString() ?? all.Message);
                CollectionAssert.AreEquivalent(new[] { id, secondId },
                    System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(all.Result, record => record.Id)));
                var children = separateReader.LoadHolonsForParent(id.ToString("D"), loadChildren: false);
                Assert.IsFalse(children.IsError, children.Exception?.ToString() ?? children.Message);
                Assert.AreEqual(secondId, System.Linq.Enumerable.Single(children.Result).Id);
            }
            finally
            {
                await separateReader.DeActivateProviderAsync();
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task ReadsEncryptedCurrentAndHistoricalRecordsFromNativeBoxes()
    {
        var id = Guid.Parse(Required("ID"));
        var key = Convert.FromBase64String(Required("KEY"));
        var program = Required("PROGRAM");

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

    private static AlgorandOASIS Create(string programDigest, byte[] storageKey, byte[]? writerSeed = null) => new(
        "http://127.0.0.1:4001", "http://127.0.0.1:8980", new string('a', 64), null,
        new AlgorandRecordBoxSettings(ulong.Parse(Required("APP"), CultureInfo.InvariantCulture),
            Required("GENESIS"), Required("WRITER"), programDigest, storageKey, writerSeed));

    private static string Required(string name) => Environment.GetEnvironmentVariable("OASIS_ALGORAND_RECORD_" + name)
        ?? throw new InvalidOperationException($"Missing native record fixture setting {name}.");
}
