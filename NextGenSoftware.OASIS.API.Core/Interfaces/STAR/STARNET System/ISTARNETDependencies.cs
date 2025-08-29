using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public interface ISTARNETDependencies
    {
        List<STARNETDependency> CelestialBodies { get; set; }
        List<STARNETDependency> CelestialBodiesMetaDataDNA { get; set; }
        List<STARNETDependency> CelestialSpaces { get; set; }
        List<STARNETDependency> Chapters { get; set; }
        List<STARNETDependency> GeoHotSpots { get; set; }
        List<STARNETDependency> GeoNFTs { get; set; }
        List<STARNETDependency> HolonsMetaDataDNA { get; set; }
        List<STARNETDependency> Holons { get; set; }
        List<STARNETDependency> InventoryItems { get; set; }
        List<STARNETDependency> Libraries { get; set; }
        List<STARNETDependency> Missions { get; set; }
        List<STARNETDependency> NFTs { get; set; }
        List<STARNETDependency> OAPPs { get; set; }
        List<STARNETDependency> Quests { get; set; }
        List<STARNETDependency> Runtimes { get; set; }
        List<STARNETDependency> Templates { get; set; }
        List<STARNETDependency> ZomesMetaDataDNA { get; set; }
        List<STARNETDependency> Zomes { get; set; }
    }
}