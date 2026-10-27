using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal static int SelectedNumericWidth(Type declared)
    {
        var input = Nullable.GetUnderlyingType(declared) ?? declared;
        return input == typeof(decimal) ? CosmicDecimalScalarResults.LimbCount
            : input == typeof(float) ? CosmicFloatingAggregateResults.SingleLimbCount
            : input == typeof(double) ? CosmicFloatingAggregateResults.DoubleLimbCount : 2;
    }

    private BsonDocument[] SelectedIntegerReduction(List<BsonDocument> selected, CosmicProjectedNumericIntent intent)
    {
        var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
        if (input != typeof(int) && input != typeof(long) && input != typeof(ulong) && input != typeof(decimal) && input != typeof(float) && input != typeof(double))
            throw new NotSupportedException("Selected integer reduction requires an admitted exact numeric domain.");

        // The final selected rank already includes every ordering and window.
        var kernel = new List<BsonDocument>
        {
            new("$project", new BsonDocument
            {
                { "_id", 0 }, { "rank", 1 },
                { "cell", new BsonDocument("$arrayElemAt", new BsonArray { "$values", intent.ColumnIndex }) }
            })
        };
        AppendNumericKernel(kernel, intent.InputType, intent.UsesInt64DoubleAverage, new BsonDocument("rank", 1));

        var empty = new BsonDocument
        {
            { "inputs", 0L }, { "invalid", 0L }, { "present", 0L },
            { "high", new BsonDecimal128(Decimal128.Zero) }, { "low", new BsonDecimal128(Decimal128.Zero) }
        };
        if (input == typeof(decimal) || input == typeof(float) || input == typeof(double))
        {
            empty.Remove("high"); empty.Remove("low");
            var width = SelectedNumericWidth(input);
            empty.Add("parts", new BsonArray(Enumerable.Range(0, width).Select(_ => input == typeof(decimal)
                ? (BsonValue)new BsonDecimal128(Decimal128.Zero) : new BsonInt64(0))));
        }
        if (intent.UsesInt64DoubleAverage) empty.Add("prefixOverflow", false);
        var pipeline = selected.Select(stage => (BsonDocument)stage.DeepClone()).ToList();
        pipeline.Add(new("$facet", new BsonDocument("numeric", new BsonArray(kernel))));
        pipeline.Add(new("$project", new BsonDocument
        {
            { "_id", 0 }, { "packet", new BsonDocument("$ifNull", new BsonArray
                { new BsonDocument("$arrayElemAt", new BsonArray { "$numeric", 0 }), Literal(empty) }) }
        }));
        ValidatePipeline(pipeline);
        return pipeline.ToArray();
    }
}
