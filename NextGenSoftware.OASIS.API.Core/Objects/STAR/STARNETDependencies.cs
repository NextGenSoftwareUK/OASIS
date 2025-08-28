using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public class STARNETDependencies : ISTARNETDependencies
    {
        public List<STARNETDependency> Runtimes { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Libraries { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Templates { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> NFTs { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> GeoNFTs { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Quests { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Missions { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Chapters { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> InventoryItems { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> CelestialSpaces { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> CelestialBodies { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Zomes { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Holons { get; set; } = new List<STARNETDependency>();
    }
}