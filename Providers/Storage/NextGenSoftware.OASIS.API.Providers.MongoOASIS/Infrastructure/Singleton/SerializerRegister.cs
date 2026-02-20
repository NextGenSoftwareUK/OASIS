using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton
{
    public sealed class SerializerRegister
    {
        private bool _isRegisterGuidBsonSerializer = false;
        private bool _isRegisterMetaDataSerializer = false;
        private static SerializerRegister _register;

        public static SerializerRegister GetInstance()
        {
            return _register ??= new SerializerRegister();
        }

        public SerializerRegister()
        {
            _isRegisterGuidBsonSerializer = false;
            _isRegisterMetaDataSerializer = false;
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
    }
}