using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("GeneKeys")]
    public class GeneKeyModel : GeneKey
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }

        [ForeignKey("AvatarId")] 
        public AvatarDetailModel AvatarDetail { get; set; }
        public string AvatarId { get; set; }

        public GeneKeyModel(){}
        public GeneKeyModel(GeneKey source){

            this.Name=source.Name;
            this.Description=source.Description;
            this.Shadow=source.Shadow;
            this.Gift=source.Gift;
            this.Sidhi=source.Sidhi;
        }

        public GeneKey GetGeneKey(){
            GeneKey item=new GeneKey();

            item.Name=this.Name;
            item.Description=this.Description;
            item.Shadow=this.Shadow;
            item.Gift=this.Gift;
            item.Sidhi=this.Sidhi;

            return(item);
        }
    }
}