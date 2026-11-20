using System;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// Normalize only proven UTC calendar text or native BSON dates to exact .NET
// ticks. A native millisecond date never rounds a 100ns comparison literal.
internal static class MongoNativeUtcDate
{
    private static BsonDocument Op(string name, params BsonValue[] values)
        => new(name, new BsonArray(values));
    private static BsonDocument Choose(BsonValue condition, BsonValue yes, BsonValue no)
        => Op("$cond", condition, yes, no);
    private static BsonDocument Type(BsonValue value) => new("$type", value);
    private static BsonDocument Let(BsonDocument variables, BsonValue body)
        => new("$let", new BsonDocument { { "vars", variables }, { "in", body } });
    private static BsonDocument Invalid => new() { { "valid", false }, { "ticks", BsonNull.Value } };
    private static BsonDocument Valid(BsonValue ticks) => new() { { "valid", true }, { "ticks", ticks } };
    private static BsonValue EpochTicks => new BsonInt64(DateTime.UnixEpoch.Ticks);
    private static BsonValue NativeTicks(BsonValue value)
        => Op("$add", Op("$multiply", new BsonDocument("$toLong", value),
            new BsonInt64(TimeSpan.TicksPerMillisecond)), EpochTicks);

    internal static BsonValue Packet(BsonValue input)
    {
        var nativeDomain = Op("$and",
            Op("$gte", "$$utcRaw", new BsonDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))),
            Op("$lte", "$$utcRaw", new BsonDateTime(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))));
        var native = Choose(nativeDomain, Valid(NativeTicks("$$utcRaw")), Invalid);
        return Let(new BsonDocument("utcRaw", input),
            Choose(Op("$eq", Type("$$utcRaw"), "date"), native,
                Choose(Op("$eq", Type("$$utcRaw"), "string"), TextPacket("$$utcRaw"), Invalid)));
    }

    private static BsonValue TextPacket(BsonValue input)
    {
        BsonValue Part(int start, int count) => new BsonDocument("$convert", new BsonDocument
        {
            { "input", Op("$substrCP", "$$utcText", start, count) },
            { "to", "int" }, { "onError", BsonNull.Value }, { "onNull", BsonNull.Value }
        });
        var parts = new BsonDocument
        {
            { "utcYear", Part(0,4) }, { "utcMonth", Part(5,2) }, { "utcDay", Part(8,2) },
            { "utcHour", Part(11,2) }, { "utcMinute", Part(14,2) }, { "utcSecond", Part(17,2) }
        };
        var leap = Op("$and", Op("$eq", Op("$mod", "$$utcYear", 4), 0),
            Op("$or", Op("$ne", Op("$mod", "$$utcYear", 100), 0),
                Op("$eq", Op("$mod", "$$utcYear", 400), 0)));
        var days = new BsonDocument("$switch", new BsonDocument
        {
            { "branches", new BsonArray
                {
                    new BsonDocument { { "case", Op("$eq", "$$utcMonth", 2) }, { "then", Choose(leap,29,28) } },
                    new BsonDocument { { "case", Op("$in", "$$utcMonth", new BsonArray {4,6,9,11}) }, { "then", 30 } }
                }
            }, { "default", 31 }
        });
        var format = new BsonDocument("$regexMatch", new BsonDocument
        {
            { "input", "$$utcText" },
            { "regex", @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?Z\z" }
        });
        var domain = Op("$and", format,
            Op("$gte","$$utcYear",1), Op("$lte","$$utcYear",9999),
            Op("$gte","$$utcMonth",1), Op("$lte","$$utcMonth",12),
            Op("$gte","$$utcDay",1), Op("$lte","$$utcDay",days),
            Op("$gte","$$utcHour",0), Op("$lte","$$utcHour",23),
            Op("$gte","$$utcMinute",0), Op("$lte","$$utcMinute",59),
            Op("$gte","$$utcSecond",0), Op("$lte","$$utcSecond",59));
        // The guarded constructor receives whole seconds only. Fractional ticks
        // are parsed independently and added as Int64, never through BSON dates.
        var second = new BsonDocument("$dateFromParts", new BsonDocument
        {
            { "year", "$$utcYear" }, { "month", "$$utcMonth" }, { "day", "$$utcDay" },
            { "hour", "$$utcHour" }, { "minute", "$$utcMinute" }, { "second", "$$utcSecond" },
            { "timezone", "UTC" }
        });
        var fractionLength = Op("$max", 0, Op("$subtract", "$$utcLength", 21));
        var fraction = Op("$substrCP", "$$utcText", 20, fractionLength);
        var padded = Op("$substrCP", Op("$concat", fraction, "0000000"), 0, 7);
        var ticks = Op("$add", NativeTicks(second), new BsonDocument("$toLong", padded));
        var parsed = Let(parts, Choose(domain, Valid(ticks), Invalid));
        return Let(new BsonDocument { { "utcText", input }, { "utcLength", new BsonDocument("$strLenCP", input) } },
            Choose(Op("$and", Op("$gte","$$utcLength",20), Op("$lte","$$utcLength",28)), parsed, Invalid));
    }

    internal static BsonValue Cell(BsonValue input)
    {
        var state = new BsonDocument("$switch", new BsonDocument
        {
            { "branches", new BsonArray
                {
                    new BsonDocument { { "case", Op("$eq",Type("$$projectionUtcRaw"),"missing") }, { "then",0 } },
                    new BsonDocument { { "case", Op("$eq",Type("$$projectionUtcRaw"),"null") }, { "then",1 } },
                    new BsonDocument { { "case","$$projectionUtcPacket.valid" }, { "then",2 } }
                }
            }, { "default",3 }
        });
        return Let(new BsonDocument("projectionUtcRaw",input),
            Let(new BsonDocument("projectionUtcPacket",Packet("$$projectionUtcRaw")),
                new BsonDocument { { "s",state }, { "b",8 }, { "v","$$projectionUtcPacket.ticks" } }));
    }

    internal static BsonValue Comparison(BsonValue input, string operation, DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Native UTC date comparisons require UTC literals.");
        return Let(new BsonDocument("comparisonUtcPacket",Packet(input)),
            Choose("$$comparisonUtcPacket.valid",
                Op(operation,"$$comparisonUtcPacket.ticks",new BsonInt64(value.Ticks)),BsonBoolean.False));
    }
}
