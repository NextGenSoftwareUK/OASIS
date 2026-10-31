using System.Reflection;
using Google.Cloud.Firestore.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class GoogleIdentityTransactionInspectionTests
{
    [TestMethod][TestCategory("IdentityTransactionInspection")]
    public async Task InspectOriginalIdentityTransactionReadWithoutMutation()
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var db=GoogleNativeFirestore.Database();var path=$"projects/{db.ProjectId}/databases/{db.DatabaseId}";
        var opened=await db.Client.BeginTransactionAsync(new BeginTransactionRequest{Database=path});
        Assert.IsFalse(opened.Transaction.IsEmpty);
        try{
            var method=typeof(GoogleCloudOASIS).GetMethod("ReadFirestoreIdentityDocumentAsync",BindingFlags.NonPublic|BindingFlags.Static)!;
            foreach(var collection in new[]{"avatars","avatarDetails"}){
                var reference=db.Collection(collection).Document("b81a1c54-d392-441b-be7d-8e3ead8181ee");
                var result=await (Task<Document>)method.Invoke(null,new object[]{reference,opened.Transaction})!;
                Assert.IsNull(result,"Original failed create must not be replayed or assumed absent.");
            }
        }finally{await db.Client.RollbackAsync(new RollbackRequest{Database=path,Transaction=opened.Transaction});}
    }
}
