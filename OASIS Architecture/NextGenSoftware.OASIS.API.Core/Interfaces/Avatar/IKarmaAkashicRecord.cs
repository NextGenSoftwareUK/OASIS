using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Avatar
{
    public interface IKarmaAkashicRecord
    {
        Guid AvatarId { get; set; }
        DateTime Date { get; set; }
        int Karma { get; set; }
        EnumValue<KarmaEarntOrLost> KarmaEarntOrLost { get; set; }
        EnumValue<KarmaSourceType> KarmaSource { get; set; }
        string KarmaSourceDesc { get; set; }
        string KarmaSourceTitle { get; set; }
        EnumValue<KarmaTypeNegative> KarmaTypeNegative { get; set; }
        EnumValue<KarmaTypePositive> KarmaTypePositive { get; set; }
        EnumValue<ProviderType> Provider { get; set; }
        long TotalKarma { get; set; }
        string WebLink { get; set; }
    }
}