using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("GeneKeyModel", "GeneKeyModel")]

    public class GeneKeyModel : GeneKey
    {
        [NeoNodeId]
        public long? Id { get; set; }
    

        public string AvatarId{ set; get; }

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