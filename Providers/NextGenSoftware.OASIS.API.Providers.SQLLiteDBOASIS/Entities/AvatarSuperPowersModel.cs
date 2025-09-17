using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarSuperPowers")]
    public class AvatarSuperPowersModel : AvatarSuperPowers
    {
        [Required, Key]
        public string AvatarId{ set; get; }

        public AvatarSuperPowersModel(){}
        public AvatarSuperPowersModel(IAvatarSuperPowers source){

            this.Flight = source.Flight;
            this.Telekinesis = source.Telekinesis;
            this.Telepathy = source.Telepathy;
            this.Teleportation = source.Teleportation;
            this.RemoteViewing = source.RemoteViewing;
            this.AstralProjection = source.AstralProjection;
            this.SuperStrength = source.SuperStrength;
            this.SuperSpeed = source.SuperSpeed;
            this.Invulnerability = source.Invulnerability;
            this.HeatVision = source.HeatVision;
            this.XRayVision = source.XRayVision;
            this.FreezeBreath = source.FreezeBreath;
            this.BioLocatation = source.BioLocatation;
        }

        public AvatarSuperPowers GetAvatarSuperPowers(){

            AvatarSuperPowers item=new AvatarSuperPowers();

            item.Flight = this.Flight;
            item.Telekinesis = this.Telekinesis;
            item.Telepathy = this.Telepathy;
            item.Teleportation = this.Teleportation;
            item.RemoteViewing = this.RemoteViewing;
            item.AstralProjection = this.AstralProjection;
            item.SuperStrength = this.SuperStrength;
            item.SuperSpeed = this.SuperSpeed;
            item.Invulnerability = this.Invulnerability;
            item.HeatVision = this.HeatVision;
            item.XRayVision = this.XRayVision;
            item.FreezeBreath = this.FreezeBreath;
            item.BioLocatation = this.BioLocatation;

            return(item);
        }        
    }
}