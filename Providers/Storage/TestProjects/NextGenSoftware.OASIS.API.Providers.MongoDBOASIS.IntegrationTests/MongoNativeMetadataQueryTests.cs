using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests;

[TestClass]
public sealed class MongoNativeMetadataQueryTests
{
    [TestMethod]
    public async Task NativeMongoMetadataQueriesReturnExactAllAndAnyMatches()
    {
        var executable = Environment.GetEnvironmentVariable("OASIS_MONGO_METADATA_MONGOD");
        if (string.IsNullOrWhiteSpace(executable))
            Assert.Inconclusive("Supply the installed mongod executable for an isolated native test.");

        var fixture = Directory.CreateTempSubdirectory("oasis-mongo-metadata-");
        var port = FindLoopbackPort();
        var settings = new ProcessStartInfo(executable!) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "--dbpath", fixture.FullName, "--bind_ip", "127.0.0.1", "--port", port.ToString(),
                     "--wiredTigerCacheSizeGB", "0.25", "--logpath", Path.Combine(fixture.FullName, "mongod.log") })
            settings.ArgumentList.Add(argument);

        using var process = Process.Start(settings) ?? throw new InvalidOperationException("Could not start the isolated MongoDB server.");
        var connection = $"mongodb://127.0.0.1:{port}/?directConnection=true&serverSelectionTimeoutMS=1500";
        var client = new MongoClient(connection);
        var admin = client.GetDatabase("admin");
        var owned = false;
        try
        {
            using var ready = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!process.HasExited)
            {
                try
                {
                    var options = await admin.RunCommandAsync<BsonDocument>("{getCmdLineOpts:1}", cancellationToken: ready.Token);
                    Assert.AreEqual(fixture.FullName, options["parsed"]["storage"]["dbPath"].AsString);
                    owned = true;
                    break;
                }
                catch (TimeoutException) when (!ready.IsCancellationRequested)
                {
                    await Task.Delay(100, ready.Token);
                }
            }
            Assert.IsTrue(owned, "The fresh process must own the MongoDB endpoint before writes.");
            await VerifyMetadataQueriesAsync(connection);
        }
        finally
        {
            if (owned && !process.HasExited)
            {
                using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    await admin.RunCommandAsync<BsonDocument>("{shutdown:1}", cancellationToken: shutdown.Token);
                }
                catch (MongoConnectionException)
                {
                    // A successful server shutdown closes the requesting connection.
                }
                await process.WaitForExitAsync(shutdown.Token);
            }
            if (process.HasExited)
            {
                var current = new DirectoryInfo(fixture.FullName);
                Assert.IsTrue(current.Parent!.FullName == Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar));
                Assert.IsFalse((current.Attributes & FileAttributes.ReparsePoint) != 0);
                current.Delete(recursive: true);
            }
        }
    }

    private static int FindLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task VerifyMetadataQueriesAsync(string connection)
    {
        var provider = new MongoDBOASIS(connection, "oasis_native_metadata");
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var both = new Holon { Id = Guid.NewGuid(), Name = "both", MetaData = new() { ["shape"] = "round", ["color"] = "green" } };
            var one = new Holon { Id = Guid.NewGuid(), Name = "one", MetaData = new() { ["shape"] = "round", ["color"] = "blue" } };
            var neither = new Holon { Id = Guid.NewGuid(), Name = "neither", MetaData = new() { ["shape"] = "square", ["color"] = "blue" } };
            foreach (var holon in new[] { both, one, neither })
            {
                var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
            }

            var filter = new Dictionary<string, string> { ["shape"] = "round", ["color"] = "green" };
            foreach (var mode in new[] { MetaKeyValuePairMatchMode.All, MetaKeyValuePairMatchMode.Any })
            {
                var expected = mode == MetaKeyValuePairMatchMode.All ? new[] { both.Id } : new[] { both.Id, one.Id };
                var sync = provider.LoadHolonsByMetaData(filter, mode, loadChildren: false, loadChildrenFromProvider: true);
                var async = await provider.LoadHolonsByMetaDataAsync(filter, mode, loadChildren: false, loadChildrenFromProvider: true);
                foreach (var result in new[] { sync, async })
                {
                    Assert.IsFalse(result.IsError, result.Message);
                    Assert.AreEqual(expected.Length, result.LoadedCount);
                    CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
                }
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }
}
