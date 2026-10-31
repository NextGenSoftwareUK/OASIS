using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Save is not an implicit restore or a replacement deletion command.
    // Call only with the authoritative record protected by the native write lock.
    private static void RequireSaveLifecycle(IHolonBase previous,IHolonBase proposed)
        =>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonSaveLifecycle.RequireCompatibleSave(previous,proposed);
}
