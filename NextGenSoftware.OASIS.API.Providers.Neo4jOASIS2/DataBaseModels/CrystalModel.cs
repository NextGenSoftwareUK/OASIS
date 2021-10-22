using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("CrystalModel", "CrystalModel")]

    public class CrystalModel{
        [NeoNodeId]
        public long? Id { get; set; }
    
        public long AvatarChakraId { set; get; }

        public String Name { get; set; }
        public String Type { get; set; }
        public string Description { get; set; }
        public int ProtectionLevel { get; set; }
        public int EnergisingLevel { get; set; }
        public int GroundingLevel { get; set; }
        public int CleansingLevel { get; set; }
        public int AmplifyicationLevel { get; set; }
        
        public CrystalModel(){}
        public CrystalModel(Crystal source){

            this.Type=source.Type.Name;
            this.Name=source.Name.Name;

            this.Description=source.Description;
            this.ProtectionLevel=source.ProtectionLevel;
            this.EnergisingLevel=source.EnergisingLevel;
            this.GroundingLevel=source.GroundingLevel;
            this.CleansingLevel=source.CleansingLevel;
        }

        public Crystal GetCrystal(){
            Crystal item=new Crystal();

            CrystalType crystalType=Enum.Parse<CrystalType>(this.Type);
            item.Type=new EnumValue<CrystalType>(crystalType);

            CrystalName crystalName=Enum.Parse<CrystalName>(this.Name);
            item.Name=new EnumValue<CrystalName>(crystalName);

            item.Description=this.Description;
            item.ProtectionLevel=this.ProtectionLevel;
            item.EnergisingLevel=this.EnergisingLevel;
            item.GroundingLevel=this.GroundingLevel;
            item.CleansingLevel=this.CleansingLevel;

            return(item);
        }

    }
}