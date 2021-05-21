using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities
{
    public class Holon // Equvilant to the base Holon object in OASIS.API.Core.
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        public bool ChangesSaved { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.

        public Guid HolonId { get; set; } //Unique id within the OASIS.
        public string Name { get; set; }
        public string Description { get; set; }
        //  public string ProviderKey { get; set; } //Unique key used by each provider (e.g. hashaddress in hc, etc).
        public HolonType HolonType { get; set; }
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        //public ICelestialBody ParentStar { get; set; } //The Star this Holon belongs to.
        public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //public ICelestialBody ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        //public ICelestialBody ParentMoon { get; set; } //The Moon this Holon belongs to.
        public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        //public Guid ParentCelestialBodyId { get; set; } //The CelestialBody (Planet or Moon (OAPP)) this Holon belongs to.
        //public ICelestialBody ParentCelestialBody { get; set; } //The CelestialBody (Planet or Moon (OAPP)) this Holon belongs to.
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public ProviderType CreatedProviderType { get; set; }
        public List<INode> Nodes { get; set; }


        //public string ProviderKey { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ProviderType, string> ProviderKey { get; set; } = new Dictionary<ProviderType, string>();

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifiedDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime DeletedDate { get; set; }

        public int Version { get; set; }
        public bool IsActive { get; set; }

        //  [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string ModifiedByAvatarId { get; set; }

       // [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByAvatarId { get; set; }

        /*
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedByUserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string DeletedByUserId { get; set; }
        */
    }
}