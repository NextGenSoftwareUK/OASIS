using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("Nebula")]
    public class NebulaModel : CelestialSpaceAbstract{

        [Required, Key]
        public String NebulaId{ set; get; }
        
        public String GalaxyId{ set; get; }
        public String UniverseId{ set; get; }

        public NebulaModel():base(){}
        public NebulaModel(INebula source):base(){

            if(source.Id == Guid.Empty){
                source.Id = Guid.NewGuid();
            }
        
            this.NebulaId = source.Id.ToString();
            this.HolonId = source.ParentHolonId.ToString();
        }

        // public INebula GetNebula(){
        //     Nebula item=new Nebula();
        //
        //     item.Id = Guid.Parse(this.NebulaId);
        //     item.ParentHolonId = Guid.Parse(this.HolonId);
        //
        //     return(item);
        // }
    }
}