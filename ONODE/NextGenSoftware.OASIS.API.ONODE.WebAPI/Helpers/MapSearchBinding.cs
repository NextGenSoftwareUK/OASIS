using System;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>Map-search transport binding; the host's existing authorization policy remains authoritative.</summary>
    public static class MapSearchBinding
    {
        public static Guid RequireAvatarId(HttpContext? context) => AuthenticatedRequestAvatar.RequireId(context);

        public static LocationType? ParseLocationType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!Enum.TryParse<LocationType>(value, true, out var type) || !Enum.IsDefined(typeof(LocationType), type))
                throw new ArgumentException("The map location type is invalid.", nameof(value));
            return type;
        }
    }
}
