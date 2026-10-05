using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using Solnet.Rpc.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaHolonDto : SolanaBaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsActive { get; set; }

    public string PublicKey { get; set; }
    public AccountInfo AccountInfo { get; set; }
    public ulong Lamports { get; set; }
    public string Owner { get; set; }
    public bool Executable { get; set; }
    public ulong RentEpoch { get; set; }
    public List<string> Data { get; set; }
    public Dictionary<string, object> MetaData { get; set; }

    public Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.
    public IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.
    public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
    public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
    public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
    public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
    public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
    public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
    public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
    public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
    public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
    public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
    public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
    public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
    public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
    public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
    public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
    public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
    public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
    public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
    public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
    public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
    public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
    public IStar ParentStar { get; set; } //The Star this Holon belongs to.
    public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
    public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
    public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
    public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
    public Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
    public ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
    public Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 
    public ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 
    public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
    public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.

}