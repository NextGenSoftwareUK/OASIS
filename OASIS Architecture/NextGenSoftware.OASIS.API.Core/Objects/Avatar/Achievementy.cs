using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Achievement : IAchievement
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid AvatarId { get; set; }
        public KarmaTypePositive AchievementType { get; set; }
        public DateTime AchievementEarnt { get; set; }
        public string KarmaSourceTitle { get; set; } //Name of the app/website/game etc.
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public EnumValue<KarmaSourceType> KarmaSource { get; set; } //App, dApp, hApp, Website or Game.
        public EnumValue<ProviderType> Provider { get; set; }
    }
}