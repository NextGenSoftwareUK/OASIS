using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAchievement
    {
        DateTime AchievementEarnt { get; set; }
        KarmaTypePositive AchievementType { get; set; }
        Guid AvatarId { get; set; }
        string Description { get; set; }
        EnumValue<KarmaSourceType> KarmaSource { get; set; }
        string KarmaSourceDesc { get; set; }
        string KarmaSourceTitle { get; set; }
        string Name { get; set; }
        EnumValue<ProviderType> Provider { get; set; }
        string WebLink { get; set; }
    }
}