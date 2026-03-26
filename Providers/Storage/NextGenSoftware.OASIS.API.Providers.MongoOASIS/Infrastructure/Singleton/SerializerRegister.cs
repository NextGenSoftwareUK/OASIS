using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton
{
    public sealed class SerializerRegister
    {
        private bool _isRegisterGuidBsonSerializer = false;
        private bool _isRegisterMetaDataSerializer = false;
        private bool _isRegisterEnumValueBsonSerializers = false;
        private static SerializerRegister _register;

        public static SerializerRegister GetInstance()
        {
            return _register ??= new SerializerRegister();
        }

        public SerializerRegister()
        {
            _isRegisterGuidBsonSerializer = false;
            _isRegisterMetaDataSerializer = false;
            _isRegisterEnumValueBsonSerializers = false;
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
        /// Registers EnumValue{T} BSON serializers so documents with both "Value" and "Name"
        /// (e.g. CreatedProviderType: { Value: 1, Name: "MongoDBOASIS" }) deserialize correctly.
        /// Without this, the driver throws "Element 'Name' does not match any field or property".
        /// </summary>
        public void RegisterEnumValueBsonSerializers()
        {
            if (_isRegisterEnumValueBsonSerializers) return;
            BsonSerializer.RegisterSerializer(typeof(NextGenSoftware.Utilities.EnumValue<ProviderType>), new EnumValueBsonSerializer<ProviderType>());
            BsonSerializer.RegisterSerializer(typeof(NextGenSoftware.Utilities.EnumValue<OASISType>), new EnumValueBsonSerializer<OASISType>());
            BsonSerializer.RegisterSerializer(typeof(NextGenSoftware.Utilities.EnumValue<AvatarType>), new EnumValueBsonSerializer<AvatarType>());
            _isRegisterEnumValueBsonSerializers = true;
        }
    }
}