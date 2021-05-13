
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarAura : IAvatarAura
    {
        public int Level { get; set; }
        public int Value { get; set; }
        public int Progress { get; set; }
        public int Brightness { get; set; }
        public int Size { get; set; }
        public int ColourRed { get; set; }
        public int ColourGreen { get; set; }
        public int ColourBlue { get; set; }
    }
}