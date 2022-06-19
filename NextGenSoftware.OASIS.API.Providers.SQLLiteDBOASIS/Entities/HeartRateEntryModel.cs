using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("HeartRateEntry")]
    public class HeartRateEntryModel : HeartRateEntry{

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }
        
        public string AvatarId{ set; get; }

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