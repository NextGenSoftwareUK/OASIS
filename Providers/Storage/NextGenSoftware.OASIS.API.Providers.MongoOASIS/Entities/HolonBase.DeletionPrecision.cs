using System;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities
{
    public partial class HolonBase
    {
        private DateTime _deletedDate;
        private DateTime _createdDate, _modifiedDate;
        private bool _readCreatedDate, _readModifiedDate;
        private long? _readCreatedTicks, _readModifiedTicks;
        private bool _readingBson;
        private bool _readDeletionDate;
        private long? _readDeletionTicks;

        // Keep the existing BSON date field/query shape; retain only precision it cannot represent.
        [BsonIgnoreIfNull]
        public long? DeletedDateExactUtcTicks
        {
            get => MongoAuditDatePrecision.ExactTicks(_deletedDate);
            set
            {
                if (!_readingBson) throw new InvalidOperationException("Exact deletion ticks are a storage representation; assign DeletedDate instead.");
                _readDeletionTicks = value;
            }
        }

        [BsonIgnoreIfNull]
        public long? CreatedDateExactUtcTicks
        {
            get => MongoAuditDatePrecision.ExactTicks(_createdDate);
            set
            {
                if (!_readingBson) throw new InvalidOperationException("Exact created ticks are a storage representation; assign CreatedDate instead.");
                _readCreatedTicks = value;
            }
        }

        [BsonIgnoreIfNull]
        public long? ModifiedDateExactUtcTicks
        {
            get => MongoAuditDatePrecision.ExactTicks(_modifiedDate);
            set
            {
                if (!_readingBson) throw new InvalidOperationException("Exact modified ticks are a storage representation; assign ModifiedDate instead.");
                _readModifiedTicks = value;
            }
        }

        private MongoDB.Bson.BsonArray _readMetadataPrecision;
        [BsonIgnoreIfNull]
        public MongoDB.Bson.BsonArray MetaDataExactUtcTicks
        {
            get => MongoMetadataPrecision.Capture(MetaData);
            set
            {
                if (!_readingBson) throw new InvalidOperationException("Metadata precision is a storage representation; assign MetaData instead.");
                _readMetadataPrecision = value;
            }
        }

        void ISupportInitialize.BeginInit()
        {
            _readingBson = true;
            _readMetadataPrecision = null;
            _readCreatedDate = _readModifiedDate = false;
            _readCreatedTicks = _readModifiedTicks = null;
            _readDeletionDate = false;
            _readDeletionTicks = null;
        }

        void ISupportInitialize.EndInit()
        {
            try
            {
                MongoMetadataPrecision.Restore(MetaData, _readMetadataPrecision);
                _createdDate = MongoAuditDatePrecision.Restore(_createdDate, _readCreatedDate, _readCreatedTicks, "creation");
                _modifiedDate = MongoAuditDatePrecision.Restore(_modifiedDate, _readModifiedDate, _readModifiedTicks, "modification");
                _deletedDate = MongoAuditDatePrecision.Restore(_deletedDate, _readDeletionDate, _readDeletionTicks, "deletion");
            }
            finally { _readMetadataPrecision = null; _readingBson = false; _readDeletionTicks = _readCreatedTicks = _readModifiedTicks = null; }
        }
    }
}
