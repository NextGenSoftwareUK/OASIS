

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarStat : IAvatarStat
    {
        public int Current { get; set; }
        public int Max { get; set; }
    }
}