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
        if (operation == CosmicScalarOperation.Average)
        {
            // The bounded selection visitor already knows the exact CLR input
            // type. Bind only the four public integer Average overloads;
            // no reflection or compiled selector delegate is involved.
            if (typeof(TValue) == typeof(int))
                return consumer.Consume(source.Average((Expression<Func<Holon, int>>)(object)selector));
            if (typeof(TValue) == typeof(long))
                return consumer.Consume(source.Average((Expression<Func<Holon, long>>)(object)selector));
            if (typeof(TValue) == typeof(int?))
                return consumer.Consume(source.Average((Expression<Func<Holon, int?>>)(object)selector));
            if (typeof(TValue) == typeof(long?))
                return consumer.Consume(source.Average((Expression<Func<Holon, long?>>)(object)selector));
            throw new ArgumentException("Average requires an Int32/Int64 selection, optionally nullable.");
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
