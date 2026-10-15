using System;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities
{
    public partial class HolonBase
    {
        private DateTime _deletedDate;
        private bool _readingBson;
        private bool _readDeletionDate;
        private long? _readDeletionTicks;

        // Keep the existing BSON date field/query shape; retain only precision it cannot represent.
        [BsonIgnoreIfNull]
        public long? DeletedDateExactUtcTicks
        {
            get => _deletedDate.Ticks % TimeSpan.TicksPerMillisecond == 0 ? null : _deletedDate.Ticks;
            set
            {
                if (!_readingBson) throw new InvalidOperationException("Exact deletion ticks are a storage representation; assign DeletedDate instead.");
                _readDeletionTicks = value;
            }
        }

        void ISupportInitialize.BeginInit()
        {
            _readingBson = true;
            _readDeletionDate = false;
            _readDeletionTicks = null;
        }

        void ISupportInitialize.EndInit()
        {
            try
            {
                if (_readDeletionTicks.HasValue)
                {
                    var exact = new DateTime(_readDeletionTicks.Value, DateTimeKind.Utc);
                    if (!_readDeletionDate || exact.Ticks - exact.Ticks % TimeSpan.TicksPerMillisecond != _deletedDate.Ticks)
                        throw new FormatException("Exact deletion timestamp disagrees with the persisted BSON date.");
                    _deletedDate = exact;
                }
            }
            finally { _readingBson = false; _readDeletionTicks = null; }
        }
    }
}
