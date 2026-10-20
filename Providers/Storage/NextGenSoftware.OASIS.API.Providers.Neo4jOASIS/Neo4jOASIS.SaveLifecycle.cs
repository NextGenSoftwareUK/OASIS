using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Save is not an implicit restore or a replacement deletion command.
    // Call only with the authoritative record protected by the native write lock.
    private static void RequireSaveLifecycle(IHolonBase previous,IHolonBase proposed)
    {
        if(previous?.IsDeleted!=true)return;
        if(proposed.IsActive || !proposed.IsDeleted || proposed.DeletedDate!=previous.DeletedDate
            || proposed.DeletedByAvatarId!=previous.DeletedByAvatarId)
            throw new StorageLifecycleConflictException("Save cannot clear or replace a committed Neo4j tombstone.");
    }
}
