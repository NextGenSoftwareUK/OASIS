
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class HolonSavedHelper
    {
        public static IHolon MapBaseHolonProperties(IHolon sourceHolon, IHolon targetHolon)
        {
            targetHolon.Id = sourceHolon.Id;
            targetHolon.ProviderKey = sourceHolon.ProviderKey;
            //targetHolon.CelestialBodyCore.Id = sourceHolon.Id;
            //targetHolon.CelestialBodyCore.ProviderKey = sourceHolon.ProviderKey;
            targetHolon.CreatedByAvatar = sourceHolon.CreatedByAvatar;
            targetHolon.CreatedByAvatarId = sourceHolon.CreatedByAvatarId;
            targetHolon.CreatedDate = sourceHolon.CreatedDate;
            targetHolon.ModifiedByAvatar = sourceHolon.ModifiedByAvatar;
            targetHolon.ModifiedByAvatarId = sourceHolon.ModifiedByAvatarId;
            targetHolon.ModifiedDate = sourceHolon.ModifiedDate;
            targetHolon.Children = sourceHolon.Children;

            return targetHolon;
        }
    }
}