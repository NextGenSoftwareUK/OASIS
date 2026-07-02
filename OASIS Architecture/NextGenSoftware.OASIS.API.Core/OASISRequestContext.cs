using System;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core
{
    /// <summary>
    /// Request-scoped ambient context for the current avatar.
    /// Set by WEB4/WEB5 middleware per request. AvatarManager.LoggedInAvatar reads this first, then falls back to static.
    /// Uses AsyncLocal so each concurrent request has its own value (safe for multiple clients).
    /// </summary>
    public static class OASISRequestContext
    {
        private static readonly AsyncLocal<IAvatar> _currentAvatar = new AsyncLocal<IAvatar>();
        private static readonly AsyncLocal<Guid?> _currentAvatarId = new AsyncLocal<Guid?>();

        /// <summary>
        /// The avatar for the current async request flow. Set by API middleware; clear when request ends.
        /// </summary>
        public static IAvatar CurrentAvatar
        {
            get => _currentAvatar.Value;
            set => _currentAvatar.Value = value;
        }

        /// <summary>
        /// The avatar id for the current async request flow. Set by API middleware when avatar not loaded; clear when request ends.
        /// </summary>
        public static Guid? CurrentAvatarId
        {
            get => _currentAvatarId.Value;
            set => _currentAvatarId.Value = value;
        }
    }
}
