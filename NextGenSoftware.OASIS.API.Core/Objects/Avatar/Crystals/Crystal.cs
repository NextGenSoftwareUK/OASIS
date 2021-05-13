using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public abstract class Crystal : ICrystal
    {
        public EnumValue<CrystalName> Name { get; set; }
        public EnumValue<CrystalType> Type { get; set; }
        public string Description { get; set; }
        public int ProtectionLevel { get; set; }
        public int EnergisingLevel { get; set; }
        public int GroundingLevel { get; set; }
        public int CleansingLevel { get; set; }
        public int AmplifyicationLevel { get; set; }
        

        //TODO: Lots more to be added soon... ;-)
    }
}