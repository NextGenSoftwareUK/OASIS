using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("AvatarAura")]
    public class AvatarAuraModel
    {
        [Required, Key]
        public string AvatarId{ set; get; }

        public int Level { get; set; }
        public int Value { get; set; }
        public int Progress { get; set; }
        public int Brightness { get; set; }
        public int Size { get; set; }
        public int ColourRed { get; set; }
        public int ColourGreen { get; set; }
        public int ColourBlue { get; set; }

        public AvatarAuraModel(){}
        public AvatarAuraModel(AvatarAura source){

            this.Level=source.Level;
            this.Value=source.Value;
            this.Progress=source.Progress;
            this.Brightness=source.Brightness;
            this.Size=source.Size;
            this.ColourRed=source.ColourRed;
            this.ColourGreen=source.ColourGreen;
            this.ColourBlue=source.ColourBlue;
        }

        public AvatarAura GetAvatarAura(){

            AvatarAura item=new AvatarAura();

            item.Level=this.Level;
            item.Value=this.Value;
            item.Progress=this.Progress;
            item.Brightness=this.Brightness;
            item.Size=this.Size;
            item.ColourRed=this.ColourRed;
            item.ColourGreen=this.ColourGreen;
            item.ColourBlue=this.ColourBlue;

            return(item);
        }
    }

}