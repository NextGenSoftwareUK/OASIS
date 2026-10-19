using System.Reflection;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    private static Task<T> WriteHistory<T>(BlobClient blob, T proposal, bool enabled) where T : class, IHolonBase
        => (Task<T>)typeof(AzureStorageOASIS).GetMethod("WriteVersionedBlobAsync", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(T)).Invoke(null, new object[] { blob, proposal, enabled })!;

    private async Task VerifyNativeHistory<T>(T proposal, int containerIndex) where T : class, IHolonBase
    {
        RequireLive();
        var options = new BlobClientOptions(); options.Retry.MaxRetries = 0;
        var service = new BlobServiceClient("UseDevelopmentStorage=true", options);
        proposal.Id = Guid.NewGuid();
        var blob = service.GetBlobContainerClient(_containers[containerIndex]).GetBlobClient(proposal.Id.ToString());
        Assert.IsFalse((await blob.ExistsAsync()).Value);
        var first = await WriteHistory(blob, proposal, true);
        var before = (await blob.DownloadContentAsync()).Value;
        Assert.AreEqual(1, first.Version);
        Assert.AreEqual(0, proposal.Version);
        Assert.AreEqual(Guid.Empty, proposal.VersionId);
        var second = await WriteHistory(blob, proposal, false);
        var after = (await blob.DownloadContentAsync()).Value;
        Assert.AreNotEqual(before.Details.ETag, after.Details.ETag);
        Assert.AreEqual(2, second.Version);
        Assert.AreEqual(first.VersionId, second.PreviousVersionId);
        var document = JObject.Parse(after.Content.ToString());
        var nativeCurrent = await new EmbeddedRecordHistory().ReadAsync(proposal.Id, document, 0, OasisJson.DeserializeStored<T>);
        var nativePrevious = await new EmbeddedRecordHistory().ReadAsync(proposal.Id, document, 1, OasisJson.DeserializeStored<T>);
        Assert.AreEqual(second.VersionId, nativeCurrent.VersionId);
        Assert.AreEqual(first.VersionId, nativePrevious.VersionId);
        Assert.AreEqual(OasisJson.Serialize(first), OasisJson.Serialize(nativePrevious));
        Assert.AreEqual(0, proposal.Version);
        // Delete only the exact independently verified acknowledged revision.
        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, new BlobRequestConditions { IfMatch = after.Details.ETag });
        Assert.IsFalse((await blob.ExistsAsync()).Value);
    }

    [TestMethod] public Task OfficialSdkConditionalHolonHistoryRetainsPreviousVersion() => VerifyNativeHistory(new Holon { Name = "owned-history-holon" }, 0);
    [TestMethod] public Task OfficialSdkConditionalAvatarHistoryRetainsPreviousVersion() => VerifyNativeHistory(new Avatar { Username = "owned-history-avatar" }, 1);
    [TestMethod] public Task OfficialSdkConditionalDetailHistoryRetainsPreviousVersion() => VerifyNativeHistory(new AvatarDetail { Username = "owned-history-detail", XP = 123, Karma = 45 }, 2);
}
