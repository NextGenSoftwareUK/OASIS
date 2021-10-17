using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("AvatarSkills")]
    public class AvatarSkillsModel : AvatarSkills
    {
        [Required, Key]
        public string AvatarId{ set; get; }

        public AvatarSkillsModel(){}
        public AvatarSkillsModel(AvatarSkills source){

            this.Fishing=source.Fishing;
            this.Farming=source.Farming;
            this.Research=source.Research;
            this.Science=source.Science;
            this.Negotiating=source.Negotiating;
            this.Translating=source.Translating;
            this.MelleeCombat=source.MelleeCombat;
            this.RangeCombat=source.RangeCombat;
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
            item.MelleeCombat=this.MelleeCombat;
            item.RangeCombat=this.RangeCombat;
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