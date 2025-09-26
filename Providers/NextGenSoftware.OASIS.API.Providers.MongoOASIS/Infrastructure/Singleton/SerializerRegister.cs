using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton
{
    public sealed class SerializerRegister
    {
        private bool _isRegisterGuidBsonSerializer = false;
        private static SerializerRegister _register;

        public static SerializerRegister GetInstance()
        {
            return _register ??= new SerializerRegister();
        }

        public SerializerRegister()
        {
            _isRegisterGuidBsonSerializer = false;
        }
        
        public void RegisterGuidBsonSerializer()
        {
            if (_isRegisterGuidBsonSerializer) return;
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            _isRegisterGuidBsonSerializer = true;
        }
    }
}