using System.ComponentModel.DataAnnotations;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarHumanDesign", "AvatarHumanDesign")]

    public class AvatarHumanDesignModel
    {
        [NeoNodeId]
        public long? Id { get; set; }
        public string AvatarId { set; get; }

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