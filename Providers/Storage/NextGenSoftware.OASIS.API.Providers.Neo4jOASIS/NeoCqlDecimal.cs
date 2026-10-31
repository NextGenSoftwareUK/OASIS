using System;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// Schema3 preserves JSON number lexemes. Build the Core ordinal key inside
// Cypher using strings and small integer scale arithmetic, never binary64.
internal static class NeoCqlDecimal
{
    internal static (string Value, string Guard) Operand(string kind, string value, Func<object, string> bind)
    {
        var zeroes = bind(new string('0', 57));
        var maximum = bind(decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var grammar = bind(@"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?\z");
        // Singleton comprehensions provide lexical bindings without repeating
        // intermediate expressions or changing the caller's row cardinality.
        string Let(string name, string expression, string body)
            => "head([" + name + " IN [" + expression + "] | " + body + "])";
        string When(string guard, string body) => "CASE WHEN " + guard + " THEN " + body + " ELSE null END";

        var magnitude = "right(" + zeroes + "+cdCoefficient+substring(" + zeroes + ",0,28+cdExponent),57)";
        var complemented = "reduce(cdKey='',cdDigit IN [cdI IN range(0,56) | substring(cdMagnitude,cdI,1)] | "
            + "cdKey+substring('9876543210',toInteger(cdDigit),1))";
        var signed = Let("cdMagnitude", magnitude,
            "CASE WHEN left(cdRaw,1)='-' THEN '0'+" + complemented + " ELSE '1'+cdMagnitude END");

        // Remove insignificant zeroes before checking the96-bit coefficient.
        // A positive exponent moves digits into the coefficient; a negative
        // exponent requires scale<=28. Both checks precede padding/substrings.
        var exact = When("cdExponent>=-28 AND size(cdCoefficient)+CASE WHEN cdExponent>0 THEN cdExponent ELSE 0 END<=29",
            Let("cdBoundedCoefficient", "cdCoefficient+substring(" + zeroes + ",0,CASE WHEN cdExponent>0 THEN cdExponent ELSE 0 END)",
                When("size(cdBoundedCoefficient)<29 OR cdBoundedCoefficient<=" + maximum, signed)));
        var normalized = Let("cdCoefficient", "substring(cdDigits,head(cdNonzero),last(cdNonzero)-head(cdNonzero)+1)",
            Let("cdExponent", "cdPower-cdFraction+size(cdDigits)-last(cdNonzero)-1", exact));

        // Exponents outside Int64 cannot cancel a finite native string length.
        // Fence the parsed power before subtraction so scale arithmetic cannot
        // overflow even for otherwise well-formed hostile exponent lexemes.
        var exponent = Let("cdPower", "CASE WHEN size(cdParts)=1 THEN 0 ELSE toInteger(cdParts[1]) END",
            When("cdPower>=-size(cdDigits)-28 AND cdPower<=size(cdDigits)+29", normalized));
        var nonzero = Let("cdNonzero", "[cdI IN range(0,size(cdDigits)-1) WHERE substring(cdDigits,cdI,1)<>'0']",
            "CASE WHEN size(cdNonzero)=0 THEN '1'+" + zeroes + " ELSE " + exponent + " END");
        var mantissa = Let("cdDigits", "replace(cdMantissa,'.','')",
            Let("cdFraction", "CASE WHEN size(split(cdMantissa,'.'))=1 THEN 0 ELSE size(split(cdMantissa,'.')[1]) END", nonzero));
        var parts = Let("cdParts", "split(toLower(cdUnsigned),'e')", Let("cdMantissa", "cdParts[0]", mantissa));
        var raw = "CASE WHEN " + kind + "='integer' THEN toString(" + value + ") WHEN "
            + kind + "='number' THEN " + value + " ELSE null END";
        var key = Let("cdRaw", raw, When("cdRaw =~ " + grammar,
            Let("cdUnsigned", "CASE WHEN left(cdRaw,1)='-' THEN substring(cdRaw,1) ELSE cdRaw END", parts)));
        return (key, "(" + key + ") IS NOT NULL");
    }

    internal static string Literal(object value)
        => CosmicNumericScalarResults.DecimalOrderKey(Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture));
}
