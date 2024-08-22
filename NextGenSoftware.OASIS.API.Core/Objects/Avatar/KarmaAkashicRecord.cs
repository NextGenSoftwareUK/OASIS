
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class KarmaAkashicRecord : IKarmaAkashicRecord
    {
        public Guid AvatarId { get; set; }
        public DateTime Date { get; set; }
        public int Karma { get; set; } // Calculated from the KarmaType.
        public long TotalKarma { get; set; } // The new karma value for the avatar.
        public string KarmaSourceTitle { get; set; } //Name of the app/website/game etc.
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public EnumValue<KarmaSourceType> KarmaSource { get; set; } //App, dApp, hApp, Website or Game.
        public EnumValue<KarmaEarntOrLost> KarmaEarntOrLost { get; set; }
        public EnumValue<KarmaTypePositive> KarmaTypePositive { get; set; }
        public EnumValue<KarmaTypeNegative> KarmaTypeNegative { get; set; }
        public EnumValue<ProviderType> Provider { get; set; }
    }
}
