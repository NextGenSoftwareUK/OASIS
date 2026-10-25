using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // Input must already satisfy the exact persisted Decimal domain. Scaling
    // changes only the exponent of its at-most29-digit Decimal128 coefficient.
    // The seven signed base1e9 words never pass through binary floating point.
    internal static BsonValue DecimalScaledParts(BsonValue admittedValue)
    {
        var basis = Literal(new BsonDecimal128(new Decimal128(CosmicDecimalScalarResults.LimbBase)));
        var scale = Literal(new BsonDecimal128(new Decimal128(10000000000000000000000000000m)));
        var digit = new BsonDocument("$let", new BsonDocument
        {
            { "vars", new BsonDocument("decimalDigit", Op("$mod", "$$value.remaining", basis)) },
            { "in", new BsonDocument
                {
                    { "remaining", Op("$divide", Op("$subtract", "$$value.remaining", "$$decimalDigit"), basis) },
                    { "digits", Op("$concatArrays", new BsonArray { "$$decimalDigit" }, "$$value.digits") }
                }
            }
        });
        var packet = new BsonDocument("$reduce", new BsonDocument
        {
            { "input", Op("$range", Literal(0), Literal(CosmicDecimalScalarResults.LimbCount)) },
            { "initialValue", new BsonDocument
                {
                    { "remaining", Op("$multiply", admittedValue, scale) },
                    { "digits", new BsonArray() }
                }
            },
            { "in", digit }
        });
        return new BsonDocument("$let", new BsonDocument
        {
            { "vars", new BsonDocument("decimalPacket", packet) },
            { "in", "$$decimalPacket.digits" }
        });
    }
}
