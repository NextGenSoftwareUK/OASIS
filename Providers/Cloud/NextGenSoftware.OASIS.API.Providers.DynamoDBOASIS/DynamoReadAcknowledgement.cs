using System.IO;
using System.Net;
using Amazon.DynamoDBv2.Model;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    // One SDK acknowledgement boundary for point reads, mutation snapshots and lifecycle admission.
    // An acknowledged empty Item is absence; a missing response/collection is not.
    private static void RequireItemRead(GetItemResponse response)
    {
        if(response?.HttpStatusCode!=HttpStatusCode.OK||response.Item==null)
            throw new InvalidDataException("DynamoDB item snapshot lacks an authoritative read acknowledgement.");
    }
}
