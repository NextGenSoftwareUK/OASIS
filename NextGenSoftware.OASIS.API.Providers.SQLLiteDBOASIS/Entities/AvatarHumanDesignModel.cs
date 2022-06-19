using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarHumanDesign")]
    public class AvatarHumanDesignModel
    {
        [Required, Key]
        public string AvatarId{ set; get; }

        public string Type { get; set; }

        public AvatarHumanDesignModel(){}
        public AvatarHumanDesignModel(HumanDesign source){

            this.Type=source.Type;
        }

        public HumanDesign GetHumanDesign(){
            
            HumanDesign item=new HumanDesign();
            item.Type=this.Type;

            return(item);
        }
    }

}