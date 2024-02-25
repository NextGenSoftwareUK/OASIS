
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class CoronalEjection
    {
        //public string Message { get; set; }
        //public bool ErrorOccured { get; set; }
        public List<IZome> Zomes { get; set; } //Will only be set if GenesisType is ZomesAndHolonsOnly.
        public ICelestialBody CelestialBody { get; set; } //Will be null if GenesisType is ZomesAndHolonsOnly.
        public ICelestialSpace CelestialSpace { get; set; } //Will be null if GenesisType is ZomesAndHolonsOnly.
    }
}
