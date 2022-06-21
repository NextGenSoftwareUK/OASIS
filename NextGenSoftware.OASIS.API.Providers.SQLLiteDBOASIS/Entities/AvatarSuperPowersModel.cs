using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarSuperPowers")]
    public class AvatarSuperPowersModel : AvatarSuperPowers
    {
        [Required, Key]
        public string AvatarId{ set; get; }

        public AvatarSuperPowersModel(){}
        public AvatarSuperPowersModel(AvatarSuperPowers source){

            this.Flight = source.Flight;
            this.Telekineseis = source.Telekineseis;
            this.Telepathy = source.Telepathy;
            this.Teleportation = source.Teleportation;
            this.RemoteViewing = source.RemoteViewing;
            this.AstralProjection = source.AstralProjection;
            this.SuperStrength = source.SuperStrength;
            this.SuperSpeed = source.SuperSpeed;
            this.Invulerability = source.Invulerability;
            this.HeatVision = source.HeatVision;
            this.XRayVision = source.XRayVision;
            this.FreezeBreath = source.FreezeBreath;
            this.BioLocatation = source.BioLocatation;
        }

        public AvatarSuperPowers GetAvatarSuperPowers(){

            AvatarSuperPowers item=new AvatarSuperPowers();

            item.Flight = this.Flight;
            item.Telekineseis = this.Telekineseis;
            item.Telepathy = this.Telepathy;
            item.Teleportation = this.Teleportation;
            item.RemoteViewing = this.RemoteViewing;
            item.AstralProjection = this.AstralProjection;
            item.SuperStrength = this.SuperStrength;
            item.SuperSpeed = this.SuperSpeed;
            item.Invulerability = this.Invulerability;
            item.HeatVision = this.HeatVision;
            item.XRayVision = this.XRayVision;
            item.FreezeBreath = this.FreezeBreath;
            item.BioLocatation = this.BioLocatation;

            return(item);
        }        
    }
}