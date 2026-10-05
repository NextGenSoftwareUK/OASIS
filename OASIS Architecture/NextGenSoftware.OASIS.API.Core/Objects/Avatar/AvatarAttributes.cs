
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarAttributes : IAvatarAttributes
    {
        public int Strength { get; set; }
        public int Speed { get; set; }
        public int Dexterity { get; set; }
        public int Toughness { get; set; }
        public int Wisdom { get; set; }
        public int Intelligence { get; set; }
        public int Magic { get; set; }
        public int Vitality { get; set; }
        public int Endurance { get; set; }
    }
}