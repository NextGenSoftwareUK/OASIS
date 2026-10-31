using System;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // A Single normalization is a bounded but substantial native expression.
    // Bind it once, so predicates and result envelopes reuse the same value.
    private static BsonValue WithMetadataOperand(BsonValue raw, Core.Queries.CosmicQueryPath path,
        Func<BsonValue, BsonValue> body, string prefix)
    {
        var value = MetadataOperand(raw, path);
        var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
        return path.IsMetadata && type == typeof(float)
            ? SingleLet(prefix + "scalar", value, body("$$" + prefix + "scalar"))
            : body(value);
    }

    // Normalize once according to the declared query type. An invalid numeric
    // conversion is an object sentinel, never a nullable value or a string cast.
    private static BsonValue MetadataOperand(BsonValue value, Core.Queries.CosmicQueryPath path)
    {
        var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
        if (!path.IsMetadata) return value;
        if (type == typeof(DateTime)) return MetadataDate(value, path.Segments);
        if (type == typeof(float))
        {
            // Dispatch on the original BSON domain: Decimal128 and Int64 must
            // never pass through Double before the binary32 rounding decision.
            return Choose(Op("$eq", Type(value), Literal("double")), SingleFromNativeDouble(value),
                Choose(Op("$in", Type(value), Literal(new BsonArray { "int", "long" })), SingleFromNativeInteger(value),
                    Choose(Op("$eq", Type(value), Literal("decimal")), SingleFromNativeDecimal(value), value)));
        }
        if (type != typeof(double)) return value;

        var invalid = Literal(new BsonDocument());
        var numeric = Op("$in", Type(value), Literal(new BsonArray { "int", "long", "decimal", "double" }));
        var converted = new BsonDocument("$convert", new BsonDocument
        {
            { "input", value }, { "to", "double" }, { "onError", invalid }, { "onNull", invalid }
        });
        BsonValue number = "$$cqlfiniteDouble";
        var finite = Op("$and", Op("$eq", Type(number), Literal("double")),
            Op("$gte", number, Literal(-double.MaxValue)), Op("$lte", number, Literal(double.MaxValue)),
            Op("$or", Op("$ne", number, Literal(0d)), Op("$eq", value, Literal(0))));
        var normalized = new BsonDocument("$let", new BsonDocument
        {
            { "vars", new BsonDocument("cqlfiniteDouble", converted) },
            { "in", Choose(finite, Choose(Op("$eq", number, Literal(0d)), Literal(0d), number), invalid) }
        });
        return Choose(numeric, normalized, value);
    }
}
