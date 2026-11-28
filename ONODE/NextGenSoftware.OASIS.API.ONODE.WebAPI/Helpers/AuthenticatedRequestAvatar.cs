using System;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>Reads the existing validated request avatar; never trusts an acting-identity argument or resource owner.</summary>
    public static class AuthenticatedRequestAvatar
    {
        public static Guid RequireId(HttpContext? context)
        {
            if (context?.Items["Avatar"] is not IAvatar avatar || avatar.Id == Guid.Empty)
                throw new InvalidOperationException("The authenticated request avatar is unavailable.");
            return avatar.Id;
        }
    }
}
