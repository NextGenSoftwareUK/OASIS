

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarStats : IAvatarStats
    {
        public IAvatarStat HP { get; set; }
        public IAvatarStat Mana { get; set; }
        public IAvatarStat Energy { get; set; }
        public IAvatarStat Staminia { get; set; }
    }
}