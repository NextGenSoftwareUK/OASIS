using System.Reflection;
using Google.Api.Gax;
using Google.Cloud.Firestore;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

internal static class GoogleNativeFirestore
{
    internal sealed class CommittedReplyLossClient(Google.Cloud.Firestore.V1.FirestoreClient inner):Google.Cloud.Firestore.V1.FirestoreClient
    {
        internal int Commits;
        internal readonly IOException Lost=new("Owned native response loss after an actual acknowledged commit.");
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>inner.Settings;
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
            =>inner.BatchGetDocuments(request,callSettings);
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings callSettings=null)
        {
            await inner.CommitAsync(request,callSettings);Commits++;throw Lost;
        }
    }
    internal static FirestoreDb Database(string project="oasis-google-native")
        =>new FirestoreDbBuilder{ProjectId=project,EmulatorDetection=EmulatorDetection.EmulatorOnly,
            Settings=(Google.Cloud.Firestore.V1.FirestoreSettings)typeof(GoogleCloudOASIS)
                .GetMethod("CreateFirestoreSettings",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!}.Build();
}
