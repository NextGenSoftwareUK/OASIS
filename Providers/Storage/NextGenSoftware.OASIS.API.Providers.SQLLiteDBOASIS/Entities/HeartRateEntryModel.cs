using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{

    [Table("HeartRateEntry")]
    public class HeartRateEntryModel : HeartRateEntry
    {

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { set; get; }
        public int SequenceIndex { get; set; }

        [ForeignKey("AvatarId")] 
        public AvatarDetailModel AvatarDetail { set; get; }
        public string AvatarId { get; set; }

        public HeartRateEntryModel(){}
        public HeartRateEntryModel(IHeartRateEntry source){

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
