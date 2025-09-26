using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Drawing : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }
        public Phase Phase { get; set; }


        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }
        public File File { get; set; }

        public DrawingStatus DrawingStatus { get; set; }

        public string DrawingStatusDisplay
        {
            get
            {
                switch (DrawingStatus)
                {
                    case DrawingStatus.Approved:
                        return "Approved";

                    case DrawingStatus.Rejected:
                        return "Rejected";

                    default:
                        return "Approved";
                }
            }
        }

        //TODO: TEMP because need to rename correctly in DB and LOTS of records to do! Once renamed in DB, can remove these, because it is now 
        //correct in BaseEntity.
        [BsonRepresentation(BsonType.ObjectId)]
        public string ModifledByUserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ModifledDate { get; set; }
    }

    public enum DrawingStatus
    {
        Approved, //green = 0
        Rejected, // red = 1
        MinorChanges //yellow = 2
    }
}