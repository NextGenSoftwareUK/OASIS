using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
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
                    case DrawingStatus.Ammend:
                        return "Ammend";

                    case DrawingStatus.Approved:
                        return "Approved";

                    case DrawingStatus.Declined:
                        return "Declined";

                    default:
                        return "Approved";
                }
            }
        }
    }

    public enum DrawingStatus
    {
        Approved,
        Declined,
        Ammend
    }
}