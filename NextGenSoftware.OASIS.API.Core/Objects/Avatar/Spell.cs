

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Spell : ISpell
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int FireDamage { get; set; }
        public int WaterDamage { get; set; }
        public int IceDamage { get; set; }
        public int WindDamage { get; set; }
        public int LightningDamage { get; set; }
        public int PoisonDamage { get; set; }
        public int HealingPower { get; set; }
    }
}