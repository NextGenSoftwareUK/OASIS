using System;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
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
            && input != typeof(int) && input != typeof(long) && input != typeof(ulong))
            throw new ArgumentException("CQL v1 numeric inputs are Int32/Int64/UInt64, optionally nullable.");
        if (operation == CosmicScalarOperation.Average)
        {
            // The bounded selection visitor already knows the exact CLR input
            // type. Bind the public exact integer Average overloads;
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
            throw new ArgumentException("Average requires an Int32/Int64/UInt64 selection, optionally nullable.");
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
