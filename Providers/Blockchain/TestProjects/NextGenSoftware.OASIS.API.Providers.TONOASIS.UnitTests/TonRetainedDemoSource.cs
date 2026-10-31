using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

internal static class TonRetainedDemoSource
{
    internal sealed record Seed(Guid SourceId, Guid Actor, Guid[] Ids, string CodeHash);
    internal static string State => Required("OASIS_TON_CQL_STATE");
    internal static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException("Explicit demo configuration required: " + name);
    internal static TonSandboxStorageOptions Options(Seed seed, bool readOnly)
        => new(Required("OASIS_TON_NODE"), Required("OASIS_TON_WORKER"), Required("OASIS_TON_BINDING"),
            Path.Combine(State, "native"), seed.SourceId, seed.CodeHash, readOnly);

    internal static async Task<Seed> OpenOrCreateAsync(byte[] key)
    {
        Assert.AreEqual("owned-local-tvm-demo", Required("OASIS_TON_CQL_MODE"));
        var manifest = Path.Combine(State, "demo-seed.json");
        var pending = Path.Combine(State, "demo-setup.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve original setup; never replay/reseed it.");
        if (File.Exists(manifest))
        {
            var original = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.AreEqual(Required("OASIS_TON_CODE_HASH"), original.CodeHash);
            Assert.AreEqual(3, original.Ids.Distinct().Count());
            Assert.AreNotEqual(Guid.Empty, original.Actor);
            return original;
        }
        var nativeDirectory = Path.Combine(State, "native");
        Assert.IsFalse(Directory.Exists(nativeDirectory), "Unmanifested source must be inspected, not replaced.");
        var seed = new Seed(Guid.NewGuid(), Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(),
            Required("OASIS_TON_CODE_HASH"));
        await WriteNew(pending, seed);
        Directory.CreateDirectory(nativeDirectory);
        var native = new TonSandboxStorageBackend(Options(seed, false));
        await native.CreateSourceAsync();
        using (var provider = new TONOASIS(Options(seed, false), () => (byte[])key.Clone()) { IsVersionControlEnabled = false })
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
            try
            {
                var names = new[] { "Espresso ☕", "Latte", "Tea" };
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[index], Name = names[index],
                        Description = "Retained TON local TVM CQL demo", CreatedByAvatarId = seed.Actor }, saveChildren: false);
                    Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                }
            }
            finally { await provider.DeActivateProviderAsync(); }
        }
        Assert.IsFalse(File.Exists(Path.Combine(nativeDirectory, "pending.json")));
        Assert.AreEqual(4, Directory.GetFiles(Path.Combine(nativeDirectory, "operations"), "*.json").Length);
        await WriteNew(manifest, seed);
        File.Delete(pending);
        return seed;
    }

    internal static Dictionary<string,string> Capture()
    {
        Assert.IsFalse(File.Exists(Path.Combine(State, "demo-setup.pending.json")));
        Assert.IsFalse(File.Exists(Path.Combine(State, "native/pending.json")));
        var files = Directory.GetFiles(Path.Combine(State, "native/operations"), "*.json")
            .Append(Path.Combine(State, "native/source.json")).Append(Path.Combine(State, "demo-seed.json"));
        return files.ToDictionary(file => file, file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));
    }
    internal static void Unchanged(Dictionary<string,string> before)
    {
        var after = Capture();
        Assert.AreEqual(before.Count, after.Count);
        foreach (var item in before) Assert.AreEqual(item.Value, after[item.Key]);
    }
    private static async Task WriteNew<T>(string path, T value)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(file, value);
        file.Flush(true);
    }
}
