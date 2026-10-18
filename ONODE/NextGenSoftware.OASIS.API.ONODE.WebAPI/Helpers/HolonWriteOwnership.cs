using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

internal static class HolonWriteOwnership
{
    internal static OASISResult<bool> Validate(OASISResult<IHolon> lookup,Guid requestedId,Guid callerId)
    {
        if(lookup == null || lookup.IsError || lookup.IsWarning || (lookup.Result==null && !lookup.IsLoaded))
            return new OASISResult<bool> {IsError=true,ErrorCode="OWNERSHIP_LOOKUP_FAILED",
                Message="Cannot verify ownership because the storage lookup failed or was inconclusive.",Exception=lookup?.Exception};
        if(lookup.Result != null && lookup.Result.Id != requestedId)
            return new OASISResult<bool> {IsError=true,ErrorCode="OWNERSHIP_LOOKUP_FAILED",
                Message="Ownership lookup returned a different record."};
        if(callerId==Guid.Empty || (lookup.Result != null && lookup.Result.CreatedByAvatarId != callerId))
            return new OASISResult<bool> {IsError=true,ErrorCode="OWNERSHIP_FORBIDDEN",
                Message="Forbidden. You do not have permission to update this holon."};
        // A successful, non-warning lookup with no record permits creation with a caller-supplied ID.
        return new OASISResult<bool> {Result=true};
    }
}
