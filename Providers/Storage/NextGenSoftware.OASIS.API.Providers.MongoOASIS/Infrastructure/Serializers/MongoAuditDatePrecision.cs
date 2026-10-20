using System;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

internal static class MongoAuditDatePrecision
{
    internal static long? ExactTicks(DateTime date)
    {
        var utc = date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : date;
        var native = new MongoDB.Bson.BsonDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
        return utc.Ticks % TimeSpan.TicksPerMillisecond != 0 || native.ToUniversalTime().Ticks != utc.Ticks ? utc.Ticks : null;
    }

    internal static DateTime Restore(DateTime projection, bool present, long? ticks, string field)
    {
        if (!ticks.HasValue) return projection;
        if (ticks.Value < DateTime.MinValue.Ticks || ticks.Value > DateTime.MaxValue.Ticks)
            throw new FormatException("Exact " + field + " timestamp ticks are outside the supported range.");
        var exact = new DateTime(ticks.Value, DateTimeKind.Utc);
        // Match the installed driver's native date projection, including negative Unix times.
        if (!present || new MongoDB.Bson.BsonDateTime(exact).MillisecondsSinceEpoch != new MongoDB.Bson.BsonDateTime(projection).MillisecondsSinceEpoch)
            throw new FormatException("Exact " + field + " timestamp disagrees with the persisted BSON date.");
        return exact;
    }
}
