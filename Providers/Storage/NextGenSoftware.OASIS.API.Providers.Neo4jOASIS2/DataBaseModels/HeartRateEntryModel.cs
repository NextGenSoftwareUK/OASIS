using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels
{

    [NeoNodeEntity("HeartRateEntry", "HeartRateEntry")]

    public class HeartRateEntryModel : HeartRateEntry{
        [NeoNodeId]
        public long? Id { get; set; }
    
        
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