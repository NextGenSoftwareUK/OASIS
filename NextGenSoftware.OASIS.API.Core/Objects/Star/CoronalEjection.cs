using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class CoronalEjection : ICoronalEjection
    {
        public IEnumerable<IZome> Zomes { get; set; } //Will only be set if GenesisType is ZomesAndHolonsOnly.
        public ICelestialBody CelestialBody { get; set; } //Will be null if GenesisType is ZomesAndHolonsOnly.
        public ICelestialSpace CelestialSpace { get; set; } //Will be null if GenesisType is ZomesAndHolonsOnly.
        //public IOAPPDNA OAPPDNA { get; set; }
        public IOAPP OAPP { get; set; }
    }
}