

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarStats : IAvatarStats
    {
        public AvatarStat HP { get; set; } = new AvatarStat();
        public AvatarStat Mana { get; set; } = new AvatarStat();
        public AvatarStat Energy { get; set; } = new AvatarStat();
        public AvatarStat Staminia { get; set; } = new AvatarStat();
    }
}