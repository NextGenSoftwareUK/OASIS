//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MongoDB.Bson.Serialization.Serializers;
//using MongoDB.Bson.Serialization;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Objects;

//namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serilizaers
//{
//    public class AvatarGiftSerializer : IBsonSerializer<IAvatarGift>
//    {
//        public Type ValueType => typeof(IAvatarGift);

//        public IAvatarGift Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
//        {
//            var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
//            return BsonSerializer.Deserialize<AvatarGift>(document);
//        }

//        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
//        {
//            var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
//            return BsonSerializer.Deserialize<AvatarGift>(document);
//        }

//        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IAvatarGift value)
//        {
//            BsonSerializer.Serialize(context.Writer, value as AvatarGift);
//        }
//    }

//}
