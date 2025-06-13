using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledCelestialSpace : InstalledSTARHolon, IInstalledCelestialSpace
    {
        public InstalledCelestialSpace() : base("CelestialSpaceDNAJSON")
        {
            this.HolonType = HolonType.InstalledCelestialSpace;
        }
    }
}