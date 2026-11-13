using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.UnitTests;

[TestClass]
public class S3FamilyConformanceTests
{
    [DataTestMethod]
    [DataRow("AWS", false), DataRow("AWS", true)]
    [DataRow("MinIO", false), DataRow("MinIO", true)]
    [DataRow("ThreeFold", false), DataRow("ThreeFold", true)]
    public async Task SameCanonicalSuiteRunsThroughEveryShippingFamilyFacade(string family, bool synchronous)
    {
        using var client = new ControlledS3Client();
        using var provider = Create(family, client);
        var minio = provider as MinIOOASIS;
        Func<CoreStorageConformance.Report, Task> checkpoint = _ => Task.CompletedTask;
        var capture = Environment.GetEnvironmentVariable("OASIS_S3_FAMILY_CONTROLLED_REPORTS");
        if (family != "AWS" && (capture == "1" || capture == family))
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
            Assert.IsNotNull(root);
            var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", provider.ProviderName, synchronous ? "sync" : "async");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "report.json");
            Assert.IsFalse(File.Exists(path) || File.Exists(path + ".next"), "Preserve previous controlled report before dispatch.");
            checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(path).WriteAsync;
        }
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "controlled", synchronous,
            checkpoint,
            minio == null ? null : (id, sync) => sync
                ? Task.FromResult(minio.DeleteHolonSoft(id, false)) : minio.DeleteHolonSoftAsync(id, false),
            expectedDefaultDeletion: minio == null ? "absent" : "soft");
        Assert.IsTrue(report.Succeeded, report.CurrentCase + ": " + report.FailureType);
        Assert.AreEqual(26, report.Passed.Count);
        Assert.AreEqual(0, client.Objects.Count, "Controlled fixtures must leave no encrypted records/indexes.");
    }

    [DataTestMethod]
    [DataRow("oasis-avatars/"), DataRow("oasis-avatar-details/"), DataRow("oasis-holons/"), DataRow("oasis-indexes/")]
    public async Task LegacyObjectsBlockActivationAndDirectWritesWithoutMutation(string prefix)
    {
        using var client = new ControlledS3Client();
        var original = Encoding.UTF8.GetBytes("preserved synthetic legacy bytes");
        client.Objects[prefix + "synthetic.json"] = new(original, "legacy-revision");
        using var provider = Create("MinIO", client);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsTrue(activated.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        var listCalls = client.ListCalls;
        var saved = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "must-not-write" });
        Assert.IsTrue(saved.IsError);
        Assert.AreEqual(listCalls, client.ListCalls, "Failed layout checks must not be replayed by the same instance.");
        Assert.AreEqual(0, client.PutCalls);
        Assert.AreEqual(1, client.Objects.Count);
        CollectionAssert.AreEqual(original, client.Objects[prefix + "synthetic.json"].Bytes);
    }

    [TestMethod]
    public async Task PrivateRecordsAndIdentityIndexesUseEncryptedOpaqueObjects()
    {
        using var client = new ControlledS3Client();
        using var provider = Create("MinIO", client);
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "private-synthetic-username", Email = "private-synthetic@example.invalid" };
        var saved = await provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        foreach (var entry in client.Objects)
        {
            Assert.IsFalse(entry.Key.Contains(avatar.Username, StringComparison.Ordinal));
            Assert.IsFalse(entry.Key.Contains(avatar.Email, StringComparison.Ordinal));
            var encoded = Encoding.UTF8.GetString(entry.Value.Bytes);
            Assert.IsFalse(encoded.Contains(avatar.Username, StringComparison.Ordinal));
            Assert.IsFalse(encoded.Contains(avatar.Email, StringComparison.Ordinal));
        }
        var selected = await provider.LoadAvatarByEmailAsync(avatar.Email);
        Assert.IsFalse(selected.IsError, selected.Message);
        Assert.AreEqual(avatar.Id, selected.Result.Id);
        var deleted = await provider.DeleteAvatarAsync(avatar.Id, false);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.AreEqual(0, client.Objects.Count);
    }

    [DataTestMethod, DataRow("count"), DataRow("prefix"), DataRow("cursor"), DataRow("truncated")]
    public async Task IncompleteLegacyInspectionCannotPermitNewLayoutWrites(string corruption)
    {
        using var client = new ControlledS3Client();
        client.AlterListing = response =>
        {
            switch (corruption)
            {
                case "count": response.KeyCount = 1; break;
                case "prefix": response.Prefix = "unrelated/"; break;
                case "cursor": response.NextContinuationToken = "unexpected-next-page"; break;
                case "truncated": response.IsTruncated = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(corruption));
            }
        };
        using var provider = Create("MinIO", client);
        var saved = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "must-not-write" });
        Assert.IsTrue(saved.IsError);
        Assert.AreEqual(0, client.PutCalls);
        Assert.AreEqual(0, client.Objects.Count);
    }

    [TestMethod]
    public async Task MissingStorageKeyFailsBeforeTransportAccess()
    {
        using var client = new ControlledS3Client();
        using var provider = new MinIOOASIS(client, "synthetic", () => throw new InvalidOperationException("Synthetic missing persisted key"));
        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(0, client.ListCalls);
        Assert.AreEqual(0, client.PutCalls);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public void ClientLifetimeFollowsExplicitOwnership(bool ownsClient)
    {
        using var client = new ControlledS3Client();
        var provider = new MinIOOASIS(client, "synthetic", () => new byte[32], ownsClient: ownsClient);
        provider.Dispose();
        Assert.AreEqual(ownsClient, client.WasDisposed);
    }

    [DataTestMethod, DataRow(false, false), DataRow(false, true), DataRow(true, false), DataRow(true, true)]
    public async Task AvatarDetailDeletionAcknowledgesOneSelectedRoot(bool softDelete, bool synchronous)
    {
        using var client = new ControlledS3Client();
        using var provider = new MinIOOASIS(client, "synthetic", () => new byte[32]);
        var id = Guid.NewGuid();
        var saved = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = "synthetic-detail" });
        Assert.IsFalse(saved.IsError, saved.Message);
        var deleted = synchronous ? provider.DeleteAvatarDetail(id, softDelete) : await provider.DeleteAvatarDetailAsync(id, softDelete);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.IsTrue(deleted.IsDeleted);
        Assert.AreEqual(1, deleted.DeletedCount);
    }

    private static S3StorageProviderBase Create(string family, ControlledS3Client client) => family switch
    {
        "AWS" => new AWSS3OASIS.AWSS3OASIS(client, "synthetic", () => new byte[32]),
        "MinIO" => new MinIOOASIS(client, "synthetic", () => new byte[32]),
        "ThreeFold" => new ThreeFoldOASIS.ThreeFoldOASIS(client, "synthetic", () => new byte[32]),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };
}
