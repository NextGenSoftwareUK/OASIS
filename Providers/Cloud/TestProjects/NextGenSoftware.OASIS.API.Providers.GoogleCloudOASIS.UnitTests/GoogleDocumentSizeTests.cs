using System.Reflection;
using System.Runtime.ExceptionServices;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleDocumentSizeTests
{
    private sealed class Client:FirestoreClient{}
    private static long Measure(string path,Dictionary<string,object> fields,string project="owned-size")
    {
        var reference=FirestoreDb.Create(project,new Client()).Document(path);
        try{return (long)typeof(GoogleCloudOASIS).GetMethod("MeasureFirestoreGraphDocument",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{reference,fields})!;}
        catch(TargetInvocationException cause)when(cause.InnerException!=null){ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
    }
    [TestMethod]
    public void OfficialDocumentExampleIs147BytesNotPadded()
        =>Assert.AreEqual(147L,Measure("users/jeff/tasks/my_task_id",new(){{"type","Personal"},{"done",false},{"priority",1},{"description","Learn Cloud Firestore"}}));
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public void CanonicalScalarSizesUseDocumentedStorageWidths(int kind)
    {
        object value=kind switch{0=>null!,1=>true,2=>1,3=>1L,4=>1d,_=>Timestamp.FromDateTime(new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc))};
        Assert.AreEqual(kind<2?55L:62L,Measure("c/d",new(){{"x",value}}));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void ValidExactDocumentBoundaryIsNotRejectedByInventedPadding(bool unicode)
    {
        const int limit=1024*1024;const string path="holons/00000000-0000-0000-0000-000000000001";
        const int overhead=32+16+7+37+10+1;
        var bytes=limit-overhead;var value=unicode?new string('x',bytes-4)+"🌱":new string('x',bytes);
        Assert.AreEqual((long)limit,Measure(path,new(){{"data_json",value}}));
        Assert.ThrowsException<ArgumentException>(()=>Measure(path,new(){{"data_json",value+"x"}}));
    }
    [TestMethod]
    public void ProjectPrefixIsNotPartOfStoredDocumentNameSize()
        =>Assert.AreEqual(Measure("c/d",new(){{"x",1}},"owned-short"),Measure("c/d",new(){{"x",1}},"owned-much-longer-project-name"));
}
