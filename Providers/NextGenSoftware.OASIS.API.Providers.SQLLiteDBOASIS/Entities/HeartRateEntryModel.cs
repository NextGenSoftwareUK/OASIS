using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{

    [Table("HeartRateEntry")]
    public class HeartRateEntryModel : HeartRateEntry
    {

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { set; get; }

        [ForeignKey("AvatarId")] 
        public AvatarDetailModel AvatarDetail { set; get; }
        public string AvatarId { get; set; }

        public HeartRateEntryModel(){}
        public HeartRateEntryModel(HeartRateEntry source){

            this.HeartRateValue=source.HeartRateValue;
            this.TimeStamp=source.TimeStamp;
        }

        public HeartRateEntry GetHeartRateEntry(){

            HeartRateEntry item=new HeartRateEntry();

            item.HeartRateValue=this.HeartRateValue;
            item.TimeStamp=this.TimeStamp;

            return(item);
        }
    }
}