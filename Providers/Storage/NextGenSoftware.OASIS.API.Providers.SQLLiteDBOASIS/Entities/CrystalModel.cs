using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("Crystal")]
    public class CrystalModel{

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get;}

        public long AvatarChakraId { set; get; }

        public CrystalName Name { get; set; }
        public CrystalType Type { get; set; }
        public string Description { get; set; }
        public int ProtectionLevel { get; set; }
        public int EnergisingLevel { get; set; }
        public int GroundingLevel { get; set; }
        public int CleansingLevel { get; set; }
        public int AmplifyicationLevel { get; set; }
        
        public CrystalModel(){}
        public CrystalModel(Crystal source){

            this.Type=source.Type.Value;
            this.Name=source.Name.Value;

            this.Description=source.Description;
            this.ProtectionLevel=source.ProtectionLevel;
            this.EnergisingLevel=source.EnergisingLevel;
            this.GroundingLevel=source.GroundingLevel;
            this.CleansingLevel=source.CleansingLevel;
        }

        public Crystal GetCrystal(){
            Crystal item=new Crystal();

            item.Type=new EnumValue<CrystalType>(this.Type);
            item.Name=new EnumValue<CrystalName>(this.Name);

            item.Description=this.Description;
            item.ProtectionLevel=this.ProtectionLevel;
            item.EnergisingLevel=this.EnergisingLevel;
            item.GroundingLevel=this.GroundingLevel;
            item.CleansingLevel=this.CleansingLevel;

            return(item);
        }

    }
}