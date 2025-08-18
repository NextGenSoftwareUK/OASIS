
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarSkills : IAvatarSkills
    {
        public int Fishing { get; set; }
        public int Farming { get; set; }
        public int Research { get; set; }
        public int Science { get; set; }
        public int Negotiating { get; set; }
        public int Translating { get; set; }
        public int MeleeCombat { get; set; }
        public int RangedCombat { get; set; }
        public int SpellCasting { get; set; }
        public int Meditation { get; set; }
        public int Yoga { get; set; }
        public int Mindfulness { get; set; }
        public int Engineering { get; set; }
        public int FireStarting { get; set; }
        public int Computers { get; set; }
        public int Languages { get; set; }
    }
}