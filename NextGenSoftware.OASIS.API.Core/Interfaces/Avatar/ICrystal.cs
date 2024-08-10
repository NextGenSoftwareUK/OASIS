using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ICrystal
    {
        EnumValue<CrystalName> Name { get; set; }
        string Description { get; set; }
        EnumValue<CrystalType> Type { get; set; }
        int AmplifyicationLevel { get; set; }
        int CleansingLevel { get; set; }
        int EnergisingLevel { get; set; }
        int GroundingLevel { get; set; }
        int ProtectionLevel { get; set; }
    }
}