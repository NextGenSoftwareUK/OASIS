using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarStats")]
    public class AvatarStatsModel {

        [Required, Key] 
        public string Id { get; set; }

        [ForeignKey("AvatarId")] 
        public AvatarDetailModel AvatarDetail { get; set; }
        public string AvatarId { get; set; }
        
        public int HP_Current{ set; get; }
        public int HP_Max{ set; get; }

        public int Mana_Current{ set; get; }
        public int Mana_Max{ set; get; }

        public int Energy_Current{ set; get; }
        public int Energy_Max{ set; get; }

        public int Staminia_Current{ set; get; }
        public int Staminia_Max{ set; get; }

        public AvatarStatsModel(){}
        public AvatarStatsModel(IAvatarStats source){

            this.HP_Current=source.HP.Current;
            this.HP_Max=source.HP.Max;

            this.Mana_Current=source.Mana.Current;
            this.Mana_Max=source.Mana.Max;

            this.Energy_Current=source.Energy.Current;
            this.Energy_Max=source.Energy.Max;

            this.Staminia_Current=source.Stamina.Current;
            this.Staminia_Max=source.Stamina.Max;
        }

        public AvatarStats GetAvatarStats(){

            AvatarStats item=new AvatarStats();

            item.HP.Current=this.HP_Current;
            item.HP.Max=this.HP_Max;

            item.Mana.Current=this.Mana_Current;
            item.Mana.Max=this.Mana_Max;

            item.Energy.Current=this.Energy_Current;
            item.Energy.Max=this.Energy_Max;

            item.Stamina.Current=this.Staminia_Current;
            item.Stamina.Max=this.Staminia_Max;

            return(item);
        }
    }

}