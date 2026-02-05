using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarSkills", "AvatarSkills")]

    public class AvatarSkillsModel
    {
        [NeoNodeId]
        public long? Id { get; set; }
        public string AvatarId{ set; get; }

        public int Fishing { get; set; }
        public int Farming { get; set; }
        public int Research { get; set; }
        public int Science { get; set; }
        public int Negotiating { get; set; }
        public int Translating { get; set; }
        public int MelleeCombat { get; set; }
        public int RangeCombat { get; set; }
        public int SpellCasting { get; set; }
        public int Meditation { get; set; }
        public int Yoga { get; set; }
        public int Mindfulness { get; set; }
        public int Engineering { get; set; }
        public int FireStarting { get; set; }
        public int Computers { get; set; }
        public int Languages { get; set; }

        public AvatarSkillsModel(){}
        public AvatarSkillsModel(AvatarSkills source){

            this.Fishing=source.Fishing;
            this.Farming=source.Farming;
            this.Research=source.Research;
            this.Science=source.Science;
            this.Negotiating=source.Negotiating;
            this.Translating=source.Translating;
            this.MelleeCombat=source.MeleeCombat;
            this.RangeCombat=source.RangedCombat;
            this.SpellCasting=source.SpellCasting;
            this.Meditation=source.Meditation;
            this.Yoga=source.Yoga;
            this.Mindfulness=source.Mindfulness;
            this.Engineering=source.Engineering;
            this.FireStarting=source.FireStarting;
            this.Computers=source.Computers;
            this.Languages=source.Languages;
        }

        public AvatarSkills GetAvatarSkills(){
            
            AvatarSkills item=new AvatarSkills();

            item.Fishing=this.Fishing;
            item.Farming=this.Farming;
            item.Research=this.Research;
            item.Science=this.Science;
            item.Negotiating=this.Negotiating;
            item.Translating=this.Translating;
            item.MeleeCombat=this.MelleeCombat;
            item.RangedCombat=this.RangeCombat;
            item.SpellCasting=this.SpellCasting;
            item.Meditation=this.Meditation;
            item.Yoga=this.Yoga;
            item.Mindfulness=this.Mindfulness;
            item.Engineering=this.Engineering;
            item.FireStarting=this.FireStarting;
            item.Computers=this.Computers;
            item.Languages=this.Languages;

            return(item);
        }
    }
}