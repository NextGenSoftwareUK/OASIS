using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize][TestCategory("RoleFixtureReconciliation")]
public class GoogleRoleFixtureReconciliationTests
{
    [TestMethod]
    public async Task ReleaseOnlyVerifiedAcknowledgedPartialProbeOriginalsWithoutReplay()
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")!;
        var report=Path.Combine(work,"google-native-current.trx");Assert.AreEqual("351C5367C67BD080FBD51C534B3B494D810F64FD5A8BA2ED930EFA3708E2922E",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var journal=Path.Combine(work,"history-native-partial.pending.json");CollectionAssert.AreEqual(new[]{journal},Directory.GetFiles(work,"*.pending.json"));
        using var intent=JsonDocument.Parse(await File.ReadAllBytesAsync(journal));Assert.AreEqual("holons",intent.RootElement.GetProperty("collection").GetString());Assert.AreEqual("before-partial-batch",intent.RootElement.GetProperty("state").GetString());
        var ids=new[]{Guid.Parse("a66c5d3e-0ea5-4575-b02d-e52b2d74520b"),Guid.Parse("f202642b-b827-4702-8140-6a7009d31e0d"),Guid.Parse("be541360-57f6-44bd-aac7-557a8a8703f9")};
        CollectionAssert.AreEqual(ids,intent.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray());
        var db=GoogleNativeFirestore.Database();var observed=new List<DocumentSnapshot>();
        foreach(var id in ids){
            var snapshot=await db.Collection("holons").Document(id.ToString()).GetSnapshotAsync();Assert.IsTrue(snapshot.Exists);Assert.IsTrue(snapshot.UpdateTime.HasValue);
            var row=OasisJson.DeserializeStored<Holon>(snapshot.GetValue<string>("data_json"))!;
            Assert.AreEqual(id,row.Id);Assert.AreEqual("owned partial",row.Name);Assert.IsTrue(row.IsActive);Assert.IsFalse(row.IsDeleted);Assert.AreEqual(Guid.Empty,row.ParentHolonId);Assert.AreEqual(0,row.Version);Assert.IsNull(row.ChildRoleManifestJson);
            observed.Add(snapshot);
        }
        var pending=Path.Combine(work,"history-native-role-cleanup.pending.json");var completed=Path.Combine(work,"role-fixture-reconciliation.json");Assert.IsFalse(File.Exists(pending));Assert.IsFalse(File.Exists(completed));
        using(var stream=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{originalReportHash="351C5367C67BD080FBD51C534B3B494D810F64FD5A8BA2ED930EFA3708E2922E",records=observed.Select(row=>new{row.Reference.Path,revision=row.UpdateTime!.Value.ToString(),dataHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.GetValue<string>("data_json"))))})}));stream.Flush(true);
        }
        foreach(var row in observed){await row.Reference.DeleteAsync(Precondition.LastUpdated(row.UpdateTime!.Value));Assert.IsFalse((await row.Reference.GetSnapshotAsync()).Exists);}
        File.Move(journal,Path.Combine(work,"history-native-partial.reconciled.json"));File.Move(pending,completed);
    }
}
