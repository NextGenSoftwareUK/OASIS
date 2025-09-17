
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;
using System;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarGift : IAvatarGift //TODO: Needs more thought... How does this relate to Achievements/Badges/Virtues/Karma, etc? Think Achievements and Badges are the same thing, but prefer Achievements.
    {
        public Guid AvatarId { get; set; }
        public KarmaTypePositive GiftType { get; set; }
        public DateTime GiftEarnt { get; set; }
        public string KarmaSourceTitle { get; set; } //Name of the app/website/game etc.
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public EnumValue<KarmaSourceType> KarmaSource { get; set; } //App, dApp, hApp, Website or Game.
        public EnumValue<ProviderType> Provider { get; set; }
    }
}