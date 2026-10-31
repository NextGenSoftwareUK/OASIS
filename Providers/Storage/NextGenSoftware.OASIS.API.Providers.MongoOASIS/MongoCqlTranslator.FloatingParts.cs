using System;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // The caller must first admit and normalize the original persisted value.
    // Extract binary digits directly: Decimal128 conversion loses binary64
    // information and scaling the entire value by 2^1074 can overflow.
    internal static BsonValue FloatingScaledParts(BsonValue admittedValue, bool single)
    {
        var count = single ? 10 : 70;
        var minimumExponent = single ? -149 : -1074;
        var parts = new BsonArray();
        for (var index = count - 1; index >= 0; index--)
        {
            var exponent = minimumExponent + 30 * index;
            BsonValue remainder = "$$cqlfloatingMagnitude";
            // The highest binary64 limb has no finite upper modulus. All
            // remaining moduli are exact finite powers of two. Remainders
            // and division by a power of two preserve the source IEEE bits.
            if (exponent + 30 <= 1023)
                remainder = Op("$mod", remainder, Literal(Math.Pow(2, exponent + 30)));
            var digit = new BsonDocument("$floor",
                Op("$divide", remainder, Literal(Math.Pow(2, exponent))));
            parts.Add(Op("$multiply", new BsonDocument("$toLong", digit), "$$cqlfloatingSign"));
        }

        // Bind once for every word. Signed Int64 digits can be natively summed
        // without loss for the shared Int32.MaxValue participation bound.
        return SingleLet("cqlfloatingInput", admittedValue,
            new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument
                    {
                        { "cqlfloatingMagnitude", new BsonDocument("$abs", "$$cqlfloatingInput") },
                        { "cqlfloatingSign", Choose(Op("$lt", "$$cqlfloatingInput", Literal(0d)), Literal(-1L), Literal(1L)) }
                    }
                },
                { "in", parts }
            }));
    }
}
