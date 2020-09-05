using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ORIAServices
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
    }

    public enum DrawingStatus
    {
        Approved, //green = 0
        Rejected, // red = 1
        MinorChanges //yellow = 2
    }
}