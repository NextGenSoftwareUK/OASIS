using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("Dimension")]
    public class DimensionModel : CelestialSpaceAbstract{

        [Required, Key]
        public String DimesionId{ set; get; }

        public DimensionLevel DimensionLevel { get; set; }

        public DimensionModel():base(){}
        public DimensionModel(IDimension source):base(){

            if(source.Id == Guid.Empty){
                source.Id = Guid.NewGuid();
            }

            this.DimesionId = source.Id.ToString();
            this.HolonId = source.ParentHolonId.ToString();

            this.DimensionLevel=source.DimensionLevel;

        }
        // public IDimension GetDimension(){
        //     Dimension item=new Dimension();
        //
        //     item.Id = Guid.Parse(this.DimesionId);
        //     item.ParentHolonId = Guid.Parse(this.HolonId);
        //     
        //     item.DimensionLevel = this.DimensionLevel;
        //
        //     return(item);
        // }
    }
}