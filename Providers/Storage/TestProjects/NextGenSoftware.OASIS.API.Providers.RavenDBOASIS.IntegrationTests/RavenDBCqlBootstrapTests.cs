using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS.IntegrationTests;

[TestClass]
public class RavenDBCqlBootstrapTests
{
    private static void ValidateBootstrapSource(RavenDBCqlNativeTests.Source source)
    {
        RavenDBCqlNativeTests.ValidateSource(source);
        const string prefix = "cosmic-cql-ravendb-";
        Assert.IsTrue(source.Database.StartsWith(prefix, StringComparison.Ordinal)
            && Guid.TryParseExact(source.Database[prefix.Length..], "N", out _), "Only a uniquely named owned fixture database may be initialized.");
    }

    private static void WriteNew(string path, object value)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(file, value);
        file.Flush(flushToDisk: true);
    }

    [TestMethod]
    [TestCategory("RavenDBCqlBootstrap")]
    public async Task InitializeOneExplicitOwnedDatabaseOnce()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_RAVENDB_CQL_SOURCE");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path), "Explicit bootstrap source required.");
        var raw = await File.ReadAllBytesAsync(path!);
        var source = JsonSerializer.Deserialize<RavenDBCqlNativeTests.Source>(raw)!;
        ValidateBootstrapSource(source);
        var digest = Convert.ToHexString(SHA256.HashData(raw));
        var intent = path + ".bootstrap.intent.json";
        // Flush the complete original identity before the first native mutation.
        // Any existing intent (including an ACK loss) stops all resubmission.
        WriteNew(intent, new { sourceSha256 = digest, source, maximumDatabaseCreates = 1, recordCount = 4 });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var store = new DocumentStore { Urls = new[] { source.Endpoint }, Database = source.Database };
        store.Initialize();
        await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(source.Database)), timeout.Token);
        using var session = store.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;
        foreach (var id in source.LiveIds.Concat(new[] { source.DeletedId }))
        {
            var row = new Holon
            {
                Id = id, Name = "RavenDB CQL teaching " + id.ToString("D"),
                Description = "Owned local native CQL fixture", CreatedByAvatarId = source.Actor,
                ParentHolonId = source.Actor, IsActive = true, IsPublic = false,
                DeletedDate = id == source.DeletedId ? DateTime.UtcNow : DateTime.MinValue
            };
            var key = "OasisHolons/" + id.ToString("D");
            var envelope = new RavenDBCqlNativeTests.Envelope
            {
                Id = key, IsDeleted = row.IsDeleted,
                DataJson = JsonSerializer.Serialize(row, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
            await session.StoreAsync(envelope, string.Empty, key, timeout.Token);
        }
        await session.SaveChangesAsync(timeout.Token);
        WriteNew(path + ".bootstrap.acknowledged.json", new { sourceSha256 = digest, source.Database, saved = 4 });
        // Retain the database and original intent for the read-only demo and reconciliation.
    }

    [TestMethod]
    [TestCategory("RavenDBCqlBootstrapControls")]
    public void ExistingIntentCannotBeOverwritten()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "original uncertain intent");
            Assert.ThrowsException<IOException>(() => WriteNew(path, new { replacement = true }));
            Assert.AreEqual("original uncertain intent", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    [TestCategory("RavenDBCqlBootstrapControls")]
    public void BootstrapRejectsUnownedDatabaseAndRemoteOrDuplicateIdentities()
    {
        RavenDBCqlNativeTests.Source Valid() => new()
        {
            Endpoint = "http://127.0.0.1:18087", Database = "cosmic-cql-ravendb-" + Guid.NewGuid().ToString("N"),
            Actor = Guid.NewGuid(), LiveIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            DeletedId = Guid.NewGuid(), MissingId = Guid.NewGuid()
        };
        ValidateBootstrapSource(Valid());
        foreach (var fault in new[] { "database", "endpoint", "identity" })
        {
            var source = Valid();
            if (fault == "database") source.Database = "existing-user-database";
            if (fault == "endpoint") source.Endpoint = "http://example.com";
            if (fault == "identity") source.MissingId = source.LiveIds[0];
            Assert.ThrowsException<AssertFailedException>(() => ValidateBootstrapSource(source));
        }
    }
}
