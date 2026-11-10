using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    // Activation checks configured resources; it does not provision resources or test writes
    // outside the normal mutation/admission path. Read probes can still incur cloud charges.
    private static Task VerifyGoogleBucketAsync(StorageClient client,string bucket)
        =>client.GetBucketAsync(bucket);

    private static Task VerifyFirestoreAvailabilityAsync(FirestoreDb database)
        =>database.Collection("_oasis_health").Limit(1).GetSnapshotAsync();
}
