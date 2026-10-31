using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ExactStorageSizeBoundaryHasIndependentNativeAcceptance(bool unicode)
    {
        var work=Admit();var id=Guid.NewGuid();var database=Database();var verifier=Database();
        var reference=database.Collection("holons").Document(id.ToString());
        var independent=verifier.Collection("holons").Document(id.ToString());
        var journal=Path.Combine(work,"history-native-document-boundary.pending.json");
        const int limit=1024*1024;
        // Document-name component bytes+terminators+16, field name+terminator,
        // value terminator and32document overhead. This is NOT protobuf wire size.
        const int overhead=7+37+16+10+1+32;
        var value=unicode?new string('x',limit-overhead-4)+"🌱":new string('x',limit-overhead);
        var fields=new Dictionary<string,object>{{"data_json",value}};
        var measure=typeof(GoogleCloudOASIS).GetMethod("MeasureFirestoreGraphDocument",BindingFlags.Static|BindingFlags.NonPublic)!;
        Assert.AreEqual((long)limit,(long)measure.Invoke(null,new object[]{reference,fields})!);
        Assert.IsFalse((await independent.GetSnapshotAsync()).Exists);
        Journal(journal,id,"holons");
        await reference.CreateAsync(fields);
        var actual=await independent.GetSnapshotAsync();Assert.IsTrue(actual.Exists);
        Assert.AreEqual(value,actual.GetValue<string>("data_json"));
        var originalRevision=actual.UpdateTime;
        var overAccepted=false;Grpc.Core.RpcException? rejection=null;
        try{
            await reference.UpdateAsync(new Dictionary<string,object>{{"data_json",value+"x"}},Precondition.LastUpdated(originalRevision!.Value));
            overAccepted=true;
        }catch(Grpc.Core.RpcException cause)when(cause.StatusCode==Grpc.Core.StatusCode.InvalidArgument){rejection=cause;}
        actual=await independent.GetSnapshotAsync();Assert.IsTrue(actual.Exists);
        Assert.AreEqual(overAccepted?value+"x":value,actual.GetValue<string>("data_json"));
        if(!overAccepted)Assert.AreEqual(originalRevision,actual.UpdateTime,"Rejected native write must preserve the exact original revision.");
        await independent.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));
        Assert.IsFalse((await independent.GetSnapshotAsync()).Exists);File.Delete(journal);
        Assert.IsFalse(overAccepted,"Native service accepted one storage-calculated byte above the guard: investigate the size contract, do not shrink the fixture.");
        Assert.IsNotNull(rejection);
    }
}
