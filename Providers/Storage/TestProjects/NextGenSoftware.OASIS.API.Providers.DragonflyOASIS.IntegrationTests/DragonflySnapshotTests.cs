using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class DragonflySnapshotTests
{
    [TestMethod]
    public async Task NativeSnapshotAcknowledgesExactOriginalSourceOnce()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_DRAGONFLY_CQL_STATE")!;
        Assert.AreEqual("/var/tmp/oasis-dragonfly-cql", state);
        using var sourceLease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var source = Path.Combine(state, "data", "source.rdb");
        var receipt = Path.Combine(state, "checkpoint.json");
        var pending = Path.Combine(state, "checkpoint.pending.json");
        Assert.IsFalse(File.Exists(pending), "Unknown snapshot outcome must be preserved without replay.");
        await DragonflyCqlNativeTests.AssertSourceUnchanged(state);
        var expected = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(state, "source-snapshot.json"))));

        // Persist once through the native product API, after all source acknowledgements.
        // Later runs verify native-loaded snapshots; they do not issue another SAVE.
        if (File.Exists(receipt))
        {
            using var record = JsonDocument.Parse(await File.ReadAllTextAsync(receipt));
            Assert.AreEqual(expected, record.RootElement.GetProperty("sourceSnapshotSha256").GetString());
            Assert.IsTrue(record.RootElement.GetProperty("acknowledged").GetBoolean());
            // Native shutdown saves a new RDB file, including changing metadata.
            // Bind this process's loaded artifact, while AssertSourceUnchanged
            // above independently checks every original encrypted record/index.
            using var process = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "active-process.json")));
            CollectionAssert.AreEqual(
                Convert.FromHexString(process.RootElement.GetProperty("startupSnapshotSha256").GetString()!),
                SHA256.HashData(await File.ReadAllBytesAsync(source)));
        }
        else
        {
            Assert.IsFalse(File.Exists(source), "Never replace an unadmitted native snapshot.");
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, new { source, sourceSnapshotSha256 = expected }); intent.Flush(true); }
            using var native = await ConnectionMultiplexer.ConnectAsync(DragonflyCqlNativeTests.Options());
            var acknowledged = await native.GetServer("127.0.0.1", 16391).ExecuteAsync("SAVE", "RDB", "source.rdb");
            Assert.AreEqual("OK", acknowledged.ToString());
            Assert.IsTrue(new FileInfo(source).Length > 0, "Native SAVE must produce its acknowledged snapshot.");
            var complete = Path.Combine(state, "checkpoint.complete.json");
            using (var ack = new FileStream(complete, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(ack, new { source, sourceSnapshotSha256 = expected,
                    fileSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(source))), acknowledged = true });
                ack.Flush(true);
            }
            File.Move(complete, receipt);
            File.Delete(pending);
        }
        await DragonflyCqlNativeTests.AssertSourceUnchanged(state);
    }
}
