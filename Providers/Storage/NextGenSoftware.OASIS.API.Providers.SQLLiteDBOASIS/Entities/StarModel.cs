using System;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("Star")]
    public class StarModel : StarModelBase{

        public String GalaxyId{ set; get; }
        public String GalaxyClusterId{ set; get; }
        public String UniverseId{ set; get; }

        public StarModel():base(){}
        public StarModel(IStar source):base(){
            if(source.Id == Guid.Empty){
                source.Id = Guid.NewGuid();
            }

            this.StarId = source.Id.ToString();
            this.HolonId = source.ParentHolonId.ToString();

            this.Luminosity = source.Luminosity;
            this.StarType = source.StarType;
            this.StarClassification = source.StarClassification;
            this.StarBinaryType = source.StarBinaryType;
        }

        // public IStar GetStar(){
        //     Star item=new Star();
        //
        //     item.Id = Guid.Parse(this.StarId);
        //     item.ParentHolonId = Guid.Parse(this.HolonId);
        //     
        //     item.Luminosity = this.Luminosity;
        //     item.StarType = this.StarType;
        //     item.StarClassification = this.StarClassification;
        //     item.StarBinaryType = this.StarBinaryType;
        //
        //     return(item);
        // }
    }
}