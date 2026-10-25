using System;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal static class NeoCqlDouble
{
    internal static (string Value, string Guard) Operand(string kind, string value, Func<object, string> bind)
    {
        var grammar = bind(@"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?\z");
        var zero = bind(@"\A-?0(?:\.0+)?(?:[eE][+-]?[0-9]+)?\z");
        var maximum = bind(double.MaxValue);
        // Schema3 integer values are native Int64; number values are original
        // JSON lexemes. Guard their provenance before any numeric conversion.
        var admitted = "CASE WHEN " + kind + "='integer' THEN toFloat(" + value + ") WHEN "
            + kind + "='number' THEN CASE WHEN " + value + " =~ " + grammar
            + " THEN toFloatOrNull(" + value + ") ELSE null END ELSE null END";
        var valid = "cqldouble IS NOT NULL AND cqldouble >= -" + maximum + " AND cqldouble <= " + maximum
            + " AND (cqldouble <> 0.0 OR " + kind + "='integer' AND " + value + "=0 OR "
            + "CASE WHEN " + kind + "='number' THEN " + value + " =~ " + zero + " ELSE false END)";
        // Singleton comprehensions bind the conversion without altering rows.
        var normalized = "head([cqldouble IN [" + admitted + "] | CASE WHEN " + valid
            + " THEN CASE WHEN cqldouble=0.0 THEN 0.0 ELSE cqldouble END ELSE null END])";
        return (normalized, normalized + " IS NOT NULL");
    }
}
