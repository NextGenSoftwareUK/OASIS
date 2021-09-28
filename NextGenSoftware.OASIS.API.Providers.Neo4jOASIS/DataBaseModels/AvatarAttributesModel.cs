using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarAttributes", "AvatarAttributes")]
    public class AvatarAttributesModel
    {
        [NeoNodeId]
        public long? Id { get; set; }
        public string AvatarId{ set; get; }

        public int Strength { get; set; }
        public int Speed { get; set; }
        public int Dexterity { get; set; }
        public int Toughness { get; set; }
        public int Wisdom { get; set; }
        public int Intelligence { get; set; }
        public int Magic { get; set; }
        public int Vitality { get; set; }
        public int Endurance { get; set; }

        public AvatarAttributesModel(){}
        public AvatarAttributesModel(AvatarAttributes source){

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