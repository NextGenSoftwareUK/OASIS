using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static void RequireAcknowledgedRecord(IHolonBase submitted,IHolonBase acknowledged)
    {
        if(!StoredRecordPayloadEquality.Equivalent(OasisJson.ParseStoredObject(OasisJson.Serialize(submitted)),OasisJson.ParseStoredObject(OasisJson.Serialize(acknowledged))))
            throw new InvalidDataException("Neo4j acknowledged canonical payload differs from the submitted record.");
    }
}
