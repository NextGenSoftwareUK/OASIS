using System.IO;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static void RequireAcknowledgedRecord(IHolonBase submitted,IHolonBase acknowledged)
    {
        if(!StoredRecordPayloadEquality.Equivalent(JObject.Parse(OasisJson.Serialize(submitted)),JObject.Parse(OasisJson.Serialize(acknowledged))))
            throw new InvalidDataException("Neo4j acknowledged canonical payload differs from the submitted record.");
    }
}
