using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarSpells")]
    public class SpellModel : Spell
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }

        public string AvatarId{ set; get; }

        public SpellModel(){}
        public SpellModel(Spell source){

            this.Name = source.Name;
            this.Description = source.Description;
            this.FireDamage = source.FireDamage;
            this.WaterDamage = source.WaterDamage;
            this.IceDamage = source.IceDamage;
            this.WindDamage = source.WindDamage;
            this.LightningDamage = source.LightningDamage;
            this.PoisonDamage = source.PoisonDamage;
            this.HealingPower = source.HealingPower;
        }

        public Spell GetSpell(){
            Spell item=new Spell();

            item.Name = this.Name;
            item.Description = this.Description;
            item.FireDamage = this.FireDamage;
            item.WaterDamage = this.WaterDamage;
            item.IceDamage = this.IceDamage;
            item.WindDamage = this.WindDamage;
            item.LightningDamage = this.LightningDamage;
            item.PoisonDamage = this.PoisonDamage;
            item.HealingPower = this.HealingPower;

            return(item);
        }
    }
}