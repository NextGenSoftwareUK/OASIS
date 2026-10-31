using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    // Activation checks configured resources; it does not provision resources or test writes
    // outside the normal mutation/admission path. Read probes can still incur cloud charges.
    private static async Task VerifyGoogleBucketAsync(StorageClient client,string bucket)
        =>await client.GetBucketAsync(bucket);

    private static async Task VerifyFirestoreAvailabilityAsync(FirestoreDb database)
        =>await database.Collection("_oasis_health").Limit(1).GetSnapshotAsync();
}
