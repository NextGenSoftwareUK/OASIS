
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarStats
    {
        AvatarStat Energy { get; set; }
        AvatarStat HP { get; set; }
        AvatarStat Mana { get; set; }
        AvatarStat Stamina { get; set; }
    }
}