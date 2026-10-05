
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ISpell
    {
        string Description { get; set; }
        int FireDamage { get; set; }
        int HealingPower { get; set; }
        int IceDamage { get; set; }
        int LightningDamage { get; set; }
        string Name { get; set; }
        int PoisonDamage { get; set; }
        int WaterDamage { get; set; }
        int WindDamage { get; set; }
    }
}