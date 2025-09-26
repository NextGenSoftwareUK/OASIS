using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarGift
    {
        Guid AvatarId { get; set; }
        DateTime GiftEarnt { get; set; }
        KarmaTypePositive GiftType { get; set; }
        EnumValue<KarmaSourceType> KarmaSource { get; set; }
        string KarmaSourceDesc { get; set; }
        string KarmaSourceTitle { get; set; }
        EnumValue<ProviderType> Provider { get; set; }
        string WebLink { get; set; }
    }
}