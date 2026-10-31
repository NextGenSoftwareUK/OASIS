using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests;

/// <summary>Explicit operator export; no Pod request, native seed or existing-record mutation.</summary>
public class SolidHostedSourceExportTests
{
    [Fact, Trait("Category", "SolidHostedSourceExport")]
    public async Task ExportOnceOrVerifyThreePublicEncryptedRecordsForTheExactHostedUri()
    {
        var folder = Environment.GetEnvironmentVariable("OASIS_HOSTED_SOLID_SOURCE_ROOT")
            ?? throw new InvalidOperationException("Use Prepare-HostedSolidSource.ps1.");
        var keyFile = Environment.GetEnvironmentVariable("OASIS_HOSTED_SOLID_ORIGINAL_KEY")
            ?? throw new InvalidOperationException("The retained protected key path is required.");
        var key = ProtectedData.Unprotect(await File.ReadAllBytesAsync(keyFile), null, DataProtectionScope.CurrentUser);

        try
        {
            Assert.Equal(32, key.Length);
            using var codec = new SolidPrivateRecordCodec(new Uri("http://localhost:13000/"), key);
            var intentPath = Path.Combine(folder, "export.intent.json");
            var ackPath = Path.Combine(folder, "export.acknowledged.json");
            var bundlePath = Path.Combine(folder, "source-bundle.json");
            Directory.CreateDirectory(folder);
            object? acknowledgement = null;

            if (!File.Exists(ackPath))
            {
                Assert.False(File.Exists(intentPath) || File.Exists(bundlePath),
                    "Unacknowledged export exists; reconcile it rather than regenerating ciphertext.");
                var owner = Guid.NewGuid();
                var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
                await WriteNew(intentPath, new { schema = 1, owner, ids, root = "http://localhost:13000/" });

                var names = new[] { "SOLID Espresso", "SOLID Flat White", "SOLID Tea" };
                var records = ids.Select((id, index) =>
                {
                    var record = new Holon
                    {
                        Id = id,
                        Name = names[index],
                        Description = "Public synthetic CQL sample held by the native SOLID service.",
                        CreatedByAvatarId = owner,
                        CreatedDate = DateTime.UtcNow,
                        IsPublic = true,
                        IsActive = true,
                        Version = 1,
                        MetaData = new() { ["demoCategory"] = index == 2 ? "tea" : "coffee" }
                    };
                    var bytes = Encoding.UTF8.GetBytes(codec.Encode<IHolon>(record));
                    return new { id = id.ToString("D"), sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                        turtleBase64 = Convert.ToBase64String(bytes) };
                }).ToArray();

                await WriteNew(bundlePath, new { schema = 1, root = "http://localhost:13000/", records });
                acknowledgement = new { schema = 1, owner, ids, bundleSha256 =
                    Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(bundlePath))).ToLowerInvariant() };
            }

            using var ack = JsonDocument.Parse(acknowledgement == null
                ? await File.ReadAllTextAsync(ackPath) : JsonSerializer.Serialize(acknowledgement));
            var bundleBytes = await File.ReadAllBytesAsync(bundlePath);
            Assert.Equal(ack.RootElement.GetProperty("bundleSha256").GetString(),
                Convert.ToHexString(SHA256.HashData(bundleBytes)).ToLowerInvariant());
            using var bundle = JsonDocument.Parse(bundleBytes);
            Assert.Equal("http://localhost:13000/", bundle.RootElement.GetProperty("root").GetString());
            var exported = bundle.RootElement.GetProperty("records").EnumerateArray().ToArray();
            Assert.Equal(3, exported.Length);
            var expectedIds = ack.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
            Assert.Equal(expectedIds, exported.Select(record => Guid.Parse(record.GetProperty("id").GetString()!)));

            foreach (var record in exported)
            {
                var id = Guid.Parse(record.GetProperty("id").GetString()!);
                var bytes = Convert.FromBase64String(record.GetProperty("turtleBase64").GetString()!);
                Assert.Equal(record.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
                var decoded = codec.Decode<IHolon>(Encoding.UTF8.GetString(bytes), id);
                Assert.True(decoded.IsPublic);
                Assert.Equal(ack.RootElement.GetProperty("owner").GetGuid(), decoded.CreatedByAvatarId);
                Assert.StartsWith("SOLID ", decoded.Name);

                // Associated data must reject relocation into the retained private Pod.
                using var foreign = new SolidPrivateRecordCodec(new Uri("http://localhost:13000/oasis-cosmic-cql/"), key);
                Assert.ThrowsAny<Exception>(() => foreign.Decode<IHolon>(Encoding.UTF8.GetString(bytes), id));
            }

            // Acknowledge only after every exported envelope is decoded and checked.
            if (acknowledgement != null) await WriteNew(ackPath, acknowledgement);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private static async Task WriteNew(string path, object value)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, value);
        stream.Flush(flushToDisk: true);
    }
}
