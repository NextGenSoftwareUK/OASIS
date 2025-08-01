using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICoronalEjection
    {
        ICelestialBody CelestialBody { get; set; }
        ICelestialSpace CelestialSpace { get; set; }
        //IOAPPDNA OAPPDNA { get; set; }
        public IOAPP OAPP { get; set; }
        IEnumerable<IZome> Zomes { get; set; }
    }
}