
using System;

namespace NextGenSoftware.OASIS.API.Core
{
    public class KarmaAkashicRecord
    {
        public Guid AvatarId { get; set; }
        public DateTime Date { get; set; }
        public KarmaTypePositive KarmaTypePositive { get; set; }
        public KarmaTypeNegative KarmaTypeNegative { get; set; }
        public int Karma { get; set; } //Calculated from the KarmaType.
        public KarmaSourceType KarmaSource { get; set; } //App, dApp, hApp, Website or Game.
        public ProviderType Provider { get; set; }
        public string ProviderName { get; set; }
        public string KarmaSourceTitle { get; set; } //Name of the app/website/game etc.
        public string KarmaSourceDesc { get; set; }
        public KarmaEarntOrLost KarmaEarntOrLost { get; set; }
    }
}
