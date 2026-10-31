using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleWriteAcknowledgementTests
{
    [TestMethod]
    public async Task MissingWriteResultRetainsAttemptedDocumentIdentity()
    {
        const string path="projects/owned/databases/(default)/documents/avatars/owned";
        Func<Task<WriteResult>> operation=()=>Task.FromResult<WriteResult>(null!);
        var observe=typeof(GoogleCloudOASIS).GetMethod("ObserveFirestoreMutationAsync",BindingFlags.Static|BindingFlags.NonPublic)!;
        var pending=(Task<WriteResult>)observe.Invoke(null,new object[]{operation,path})!;
        var failure=await Assert.ThrowsExceptionAsync<GoogleFirestoreMutationReconciliationException>(async()=>await pending);
        Assert.AreEqual(path,failure.DocumentPath);Assert.AreEqual(string.Empty,failure.OriginalTransactionId);
        Assert.IsInstanceOfType<InvalidDataException>(failure.InnerException);Assert.IsTrue(NativeMutationReconciliation.IsRequired(failure));
    }
}
