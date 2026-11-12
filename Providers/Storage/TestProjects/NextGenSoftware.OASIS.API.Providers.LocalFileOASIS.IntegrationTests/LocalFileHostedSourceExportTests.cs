using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class LocalFileHostedSourceExportTests
{
    private static string Root()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) return directory.FullName;
        throw new DirectoryNotFoundException("Assigned checkout required.");
    }

    private static async Task WriteNew(string path, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.WriteAsync(bytes);
        stream.Flush(flushToDisk: true);
    }

    [TestMethod]
    public async Task CreatePublicTeachingSourceAndExportExactHostedBundleOnce()
    {
        if (Environment.GetEnvironmentVariable("OASIS_CREATE_LOCALFILE_HOSTED_SOURCE") != "1")
            Assert.Inconclusive("Explicit fixed-source export invocation required;ordinary tests must not create data.");
        var state = Path.Combine(Root(), "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent");
        if (Directory.Exists(state) && Directory.EnumerateFileSystemEntries(state).Any())
            Assert.Fail("Original source export exists;reconcile instead of reseeding/replaying.");
        Directory.CreateDirectory(state);
        var owner = Guid.NewGuid();
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        const string target = "/data/cql-local-file-demo";
        await WriteNew(Path.Combine(state, "setup.intent.json"), JsonSerializer.SerializeToUtf8Bytes(new {
            sourceOwner = owner, ids, hostedDirectory = target, existingSourcesUnchanged = true }));
        var native = Path.Combine(state, "source");
        Directory.CreateDirectory(Path.Combine(native, "Holons"));
        var payloads = new Dictionary<Guid, byte[]>();
        var hashes = new Dictionary<Guid, string>();
        var names = new[] { "Espresso", "Latte", "Tea" };
        for (int index = 0; index < ids.Length; index++)
        {
            var row = new Holon { Id = ids[index], Name = names[index],
                Description = "Public LocalFile teaching record;canonical native JSON.",
                CreatedByAvatarId = owner, IsPublic = true, IsActive = true,
                MetaData = new Dictionary<string, object> { ["Category"] = index < 2 ? "coffee" : "tea" } };
            var bytes = Encoding.UTF8.GetBytes(OasisJson.Serialize(row));
            Assert.IsTrue(bytes.Length <= 32768);
            await WriteNew(Path.Combine(native, "Holons", ids[index].ToString("D") + ".json"), bytes);
            payloads[ids[index]] = bytes;
            hashes[ids[index]] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        var records = ids.Select(id => new { id, sha256 = hashes[id] }).ToArray();
        await WriteNew(Path.Combine(state, "local-source.json"), JsonSerializer.SerializeToUtf8Bytes(new {
            schema = 1, directory = native, sourceOwner = owner, records }));
        await WriteNew(Path.Combine(state, "source-config.json"), JsonSerializer.SerializeToUtf8Bytes(new {
            schema = 1, directory = target, sourceOwner = owner, records }));
        var bundle = JsonSerializer.SerializeToUtf8Bytes(new { schema = 1, records = ids.Select(id => new {
            id, sha256 = hashes[id], contentBase64 = Convert.ToBase64String(payloads[id]) }).ToArray() });
        Assert.IsTrue(bundle.Length <= 131072);
        await WriteNew(Path.Combine(state, "bootstrap.json"), bundle);

        using var source = LocalFilePublicCqlProfile.FromJson(await File.ReadAllTextAsync(Path.Combine(state, "local-source.json")));
        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activated.IsError, activated.Message);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var actor = Guid.NewGuid();
        var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.LocalFileOASIS, ids).Result;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.LocalFileOASIS).Result).WithReadAccess(access).Result;
        var linq = CosmicLinq.Create(manager, source, query, maximumResults: 3);
        Assert.IsFalse(linq.IsError, linq.Message);
        var full = await linq.Result.ToCosmicListAsync();
        var selected = ids.Take(2).ToArray();
        var subset = await linq.Result.Where(row => selected.Contains(row.Id)).ToCosmicListAsync();
        Assert.IsFalse(full.IsError, full.Message);
        Assert.IsFalse(subset.IsError, subset.Message);
        Assert.AreEqual(3, full.Result.Count);
        Assert.AreEqual(2, subset.Result.Count);
        foreach (var id in ids)
            CollectionAssert.AreEqual(payloads[id], await File.ReadAllBytesAsync(Path.Combine(native, "Holons", id.ToString("D") + ".json")));
        await WriteNew(Path.Combine(state, "export.acknowledged.json"), JsonSerializer.SerializeToUtf8Bytes(new {
            provider = "LocalFileOASIS", sourceOwner = owner, ids,
            sdkFull = 3, sdkSubset = 2, nativeSourceUnchanged = true,
            sourceConfigSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(state, "source-config.json")))).ToLowerInvariant(),
            bootstrapSha256 = Convert.ToHexString(SHA256.HashData(bundle)).ToLowerInvariant(),
            storage = "Canonical public plaintext LocalFile records;not encrypted", hostedVerified = false }));
    }
}
