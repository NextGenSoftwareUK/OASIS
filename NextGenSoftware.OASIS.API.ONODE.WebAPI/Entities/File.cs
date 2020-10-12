
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class File : BaseEntityTitleDesc
    {
        public string URI { get; set; }
        public FileType Type { get; set; }

        //TODO: TEMP because need to rename correctly in DB and LOTS of records to do! Once renamed in DB, can remove these, because it is now 
        //correct in BaseEntity.
        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifledDate { get; set; }
    }

    public enum FileType
    {
        PDF, 
        WordDoc,
        Image,
        Video,
        Text
    }
}