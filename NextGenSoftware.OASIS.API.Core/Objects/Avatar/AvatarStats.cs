

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarStats : IAvatarStats
    {
        public IAvatarStat HP { get; set; } = new AvatarStat();
        public IAvatarStat Mana { get; set; } = new AvatarStat();
        public IAvatarStat Energy { get; set; } = new AvatarStat();
        public IAvatarStat Staminia { get; set; } = new AvatarStat();
    }
}