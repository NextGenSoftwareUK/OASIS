using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities
{
    public class AvatarDetailEntity : HolonBaseEntity
    {
        public string UmaJson { get; set; }
        public string Image2D { get; set; }
        //public string Title { get; set; }
        //public string FirstName { get; set; }
        //public string LastName { get; set; }
        //public string FullName
        //{
        //    get
        //    {
        //        return string.Concat(Title, " ", FirstName, " ", LastName);
        //    }
        //}
        public string Email { get; set; }
        public string Username { get; set; }
        //public string Password { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public string Town { get; set; }
        //public string County { get; set; }
        public string Country { get; set; }
        public string Landline { get; set; }
        public string Mobile { get; set; }
        public DateTime DOB { get; set; }
        //public EnumValue<AvatarType> AvatarType { get; set; }
        public int Karma { get; set; }
        //public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();  //Unique private key used by each provider (part of private/public key pair).

       // public Dictionary<ProviderType, string> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, string>();

        //public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();  // This is only really needed when we need to store BOTH a id and username for a provider (ProviderUniqueStorageKey on Holon already stores either id/username etc).                                                                                                         // public Dictionary<ProviderType, string> ProviderId { get; set; } = new Dictionary<ProviderType, string>(); // The ProviderUniqueStorageKey property on the base Holon object can store ids, usernames, etc that uniqueliy identity that holon in the provider (although the Guid is globally unique we still need to map the Holons the unique id/username/etc for each provider).

        //public Dictionary<ProviderType, string> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, string>();

        //public ConsoleColor FavouriteColour { get; set; }
        //public ConsoleColor STARCLIColour { get; set; }
        public int XP { get; set; }
        //public List<AvatarGift> Gifts { get; set; } = new List<AvatarGift>();
        //public AvatarChakras Chakras { get; set; } = new AvatarChakras();
       // public AvatarAura Aura { get; set; } = new AvatarAura();
        //public AvatarStats Stats { get; set; } = new AvatarStats();
        //public List<GeneKey> GeneKeys { get; set; } = new List<GeneKey>();
        //public HumanDesign HumanDesign { get; set; } = new HumanDesign();
        //public AvatarSkills Skills { get; set; } = new AvatarSkills();
        //public AvatarAttributes Attributes { get; set; } = new AvatarAttributes();
        //public AvatarSuperPowers SuperPowers { get; set; } = new AvatarSuperPowers();
        //public List<Spell> Spells { get; set; } = new List<Spell>();
        //public List<Achievement> Achievements { get; set; } = new List<Achievement>();
        //public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        //public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        public int Level { get; set; }
        //public bool AcceptTerms { get; set; }
        //public string VerificationToken { get; set; }
        //public DateTime? Verified { get; set; }
        //public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        //public string ResetToken { get; set; }
        //public string JwtToken { get; set; }
        //public string RefreshToken { get; set; }
        //public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        //public DateTime? ResetTokenExpires { get; set; }
        //public DateTime? PasswordReset { get; set; }



        public Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.
        //[NotMapped]
        //public IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.
        public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
        //[NotMapped]
        //public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
        public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
        //[NotMapped]
        //public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
        public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
        //[NotMapped]
        //public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
        //[NotMapped]
        //public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
        //[NotMapped] 
        //public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
        public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
        //[NotMapped] 
        //public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
        public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
        //[NotMapped]
        //public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
        public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
        //[NotMapped] 
        //public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
        public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        //[NotMapped] 
        //public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
        //[NotMapped] 
        //public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
        public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
        //[NotMapped] 
        //public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        //public ICelestialBody ParentStar { get; set; } //The Star this Holon belongs to.
        //[NotMapped] 
        //public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //public ICelestialBody ParentPlanet { get; set; } //The Planet this Holon belongs to.
        //[NotMapped] 
        //public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        //public ICelestialBody ParentMoon { get; set; } //The Moon this Holon belongs to.
        //[NotMapped] 
        //public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        public Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        //[NotMapped] 
        //public ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        public Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 
        //[NotMapped] 
        //public ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 

        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        //[NotMapped] 
        //public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        //[NotMapped] 
        //public IHolon ParentHolon { get; set; }
        //[NotMapped] 
        //public IEnumerable<IHolon> Children { get; set; }
        //public ProviderType CreatedProviderType { get; set; }
        //[NotMapped] 
        //public ObservableCollection<INode> Nodes { get; set; }
    }
}
