
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarStats
    {
        IAvatarStat Energy { get; set; }
        IAvatarStat HP { get; set; }
        IAvatarStat Mana { get; set; }
        IAvatarStat Staminia { get; set; }
    }
}