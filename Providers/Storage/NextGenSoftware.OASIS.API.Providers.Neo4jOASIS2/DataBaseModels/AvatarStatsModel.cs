using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarStats", "AvatarStats")]

    public class AvatarStatsModel {

        [NeoNodeId]
        public long? Id { get; set; }
        public string AvatarId{ set; get; }

        public int HP_Current{ set; get; }
        public int HP_Max{ set; get; }

        public int Mana_Current{ set; get; }
        public int Mana_Max{ set; get; }

        public int Energy_Current{ set; get; }
        public int Energy_Max{ set; get; }

        public int Staminia_Current{ set; get; }
        public int Staminia_Max{ set; get; }

        public AvatarStatsModel(){}
        public AvatarStatsModel(AvatarStats source){

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