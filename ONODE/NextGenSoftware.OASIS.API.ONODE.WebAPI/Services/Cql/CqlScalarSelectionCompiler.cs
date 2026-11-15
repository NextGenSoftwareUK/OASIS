using System;
using System.Linq.Expressions;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    public static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileDistinctCount(
        CosmicQuery<Holon> source, CqlScalarV1Selection selection)
    {
        if (source == null || selection == null)
            throw new ArgumentException("Distinct count requires a source and one exact selection.");

        const string alias = "Value";
        var column = ProjectionColumn(new CqlProjectionV1Column
        {
            Alias = alias, Field = selection.Field, MetadataPath = selection.MetadataPath,
            ValueType = selection.ValueType, Nullable = selection.Nullable
        });
        var query = RequiredComposition(source.SelectColumns(new[] { column }));
        var type = query.Columns[0].Path.ValueType;
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            query = RequiredComposition(query.Where(CompileSelectedFilter(type, alias, CqlV1Operator.NotEqual,
                JsonSerializer.SerializeToElement<object>(null))));
        return RequiredComposition(query.Distinct());
    }

    internal interface IScalarConsumer<TResult>
    {
        TResult Consume<TValue>(OASISResult<CosmicScalarQuery<Holon, TValue>> intent);
    }

    internal static TResult BindScalar<TValue, TResult>(CosmicQuery<Holon> source, CosmicScalarOperation operation,
        Expression<Func<Holon, TValue>> selector, IScalarConsumer<TResult> consumer)
    {
        // Transport enrollment is independent of a native adapter's SDK domains.
        // New numeric inputs require explicit v1 wire and live acceptance first.
        var input = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if ((operation == CosmicScalarOperation.Sum || operation == CosmicScalarOperation.Average)
            && input != typeof(int) && input != typeof(long) && input != typeof(ulong) && input != typeof(decimal)
            && input != typeof(float) && input != typeof(double))
            throw new ArgumentException("CQL v1 numeric inputs are Int32/Int64/UInt64/Decimal/Single/Double, optionally nullable.");
        if (operation == CosmicScalarOperation.Average)
        {
            // The bounded selection visitor already knows the exact CLR input
            // type. Bind the public exact numeric Average overloads;
            // no reflection or compiled selector delegate is involved.
            if (typeof(TValue) == typeof(int))
                return consumer.Consume(source.Average((Expression<Func<Holon, int>>)(object)selector));
            if (typeof(TValue) == typeof(long))
                return consumer.Consume(source.Average((Expression<Func<Holon, long>>)(object)selector));
            if (typeof(TValue) == typeof(int?))
                return consumer.Consume(source.Average((Expression<Func<Holon, int?>>)(object)selector));
            if (typeof(TValue) == typeof(long?))
                return consumer.Consume(source.Average((Expression<Func<Holon, long?>>)(object)selector));
            if (typeof(TValue) == typeof(ulong))
                return consumer.Consume(source.Average((Expression<Func<Holon, ulong>>)(object)selector));
            if (typeof(TValue) == typeof(ulong?))
                return consumer.Consume(source.Average((Expression<Func<Holon, ulong?>>)(object)selector));
            if (typeof(TValue) == typeof(decimal))
                return consumer.Consume(source.Average((Expression<Func<Holon, decimal>>)(object)selector));
            if (typeof(TValue) == typeof(decimal?))
                return consumer.Consume(source.Average((Expression<Func<Holon, decimal?>>)(object)selector));
            if (typeof(TValue) == typeof(float))
                return consumer.Consume(source.Average((Expression<Func<Holon, float>>)(object)selector));
            if (typeof(TValue) == typeof(float?))
                return consumer.Consume(source.Average((Expression<Func<Holon, float?>>)(object)selector));
            if (typeof(TValue) == typeof(double))
                return consumer.Consume(source.Average((Expression<Func<Holon, double>>)(object)selector));
            if (typeof(TValue) == typeof(double?))
                return consumer.Consume(source.Average((Expression<Func<Holon, double?>>)(object)selector));
            throw new ArgumentException("Average requires an Int32/Int64/UInt64/Decimal/Single/Double selection, optionally nullable.");
        }
        var intent = operation switch
        {
            CosmicScalarOperation.Min => source.Min(selector),
            CosmicScalarOperation.Max => source.Max(selector),
            CosmicScalarOperation.Sum => source.Sum(selector),
            _ => throw new ArgumentException("A selected scalar operation is required.")
        };
        return consumer.Consume(intent);
    }
}
