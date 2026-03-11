using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton
{
    public sealed class SerializerRegister
    {
        private bool _isRegisterGuidBsonSerializer = false;
        private bool _isRegisterMetaDataSerializer = false;
        private bool _isRegisterSTARNETDNADiscriminator = false;
        private static SerializerRegister _register;

        public static SerializerRegister GetInstance()
        {
            return _register ??= new SerializerRegister();
        }

        public SerializerRegister()
        {
            _isRegisterGuidBsonSerializer = false;
            _isRegisterMetaDataSerializer = false;
            _isRegisterSTARNETDNADiscriminator = false;
        }
        
        public void RegisterGuidBsonSerializer()
        {
            if (_isRegisterGuidBsonSerializer) return;
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            _isRegisterGuidBsonSerializer = true;
        }

        public void RegisterMetaDataDictionarySerializer()
        {
            if (_isRegisterMetaDataSerializer) return;
            
            // Register custom serializer for Dictionary<string, object> to handle JsonElement deserialization
            BsonSerializer.RegisterSerializer(typeof(Dictionary<string, object>), new MetaDataDictionarySerializer());
            
            _isRegisterMetaDataSerializer = true;
        }

        /// <summary>
        /// Registers the STARNETDNA concrete type with the MongoDB driver so that ISTARNETDNA properties
        /// (e.g. on STARNETHolon) deserialize correctly when the stored discriminator is "STARNETDNA".
        /// Fixes: Unknown discriminator value 'STARNETDNA' when loading AvatarDetail with Inventory.
        /// </summary>
        public void RegisterSTARNETDNADiscriminator()
        {
            if (_isRegisterSTARNETDNADiscriminator) return;

            if (!BsonClassMap.IsClassMapRegistered(typeof(STARNETDNA)))
            {
                BsonClassMap.RegisterClassMap<STARNETDNA>(cm =>
                {
                    cm.SetDiscriminator("STARNETDNA");
                    cm.AutoMap();
                });
            }

            _isRegisterSTARNETDNADiscriminator = true;
        }
    }
}