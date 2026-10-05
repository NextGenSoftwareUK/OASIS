using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarAttributes")]
    public class AvatarAttributesModel : AvatarAttributes
    {
        [Required, Key]
        public string AvatarId{ set; get; }

        public AvatarAttributesModel(){}
        public AvatarAttributesModel(IAvatarAttributes source){

            this.Strength=source.Strength;
            this.Speed=source.Speed;
            this.Dexterity=source.Dexterity;
            this.Toughness=source.Toughness;
            this.Wisdom=source.Wisdom;
            this.Intelligence=source.Intelligence;
            this.Magic=source.Magic;
            this.Vitality=source.Vitality;
            this.Endurance=source.Endurance;
        }

        public AvatarAttributes GetAvatarAttributes(){

            AvatarAttributes item=new AvatarAttributes();

            item.Strength=this.Strength;
            item.Speed=this.Speed;
            item.Dexterity=this.Dexterity;
            item.Toughness=this.Toughness;
            item.Wisdom=this.Wisdom;
            item.Intelligence=this.Intelligence;
            item.Magic=this.Magic;
            item.Vitality=this.Vitality;
            item.Endurance=this.Endurance;

            return(item);
        }

    }

}