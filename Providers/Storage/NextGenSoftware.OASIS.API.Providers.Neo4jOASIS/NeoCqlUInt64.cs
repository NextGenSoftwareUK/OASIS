using System;
using System.Globalization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// Schema3 already retains out-of-Int64 JSON numbers as exact text. Normalize
// unsigned integer lexemes to ordinal keys without invoking toFloat/toInteger.
internal static class NeoCqlUInt64
{
    internal static (string Value, string Guard) Operand(string kind, string value, Func<object, string> bind)
    {
        var text = "(CASE WHEN " + kind + "='integer' THEN toString(" + value + ") WHEN "
            + kind + "='number' THEN " + value + " ELSE null END)";
        var guard = text + " =~ " + bind(@"\A(?:0|[1-9][0-9]{0,19})\z")
            + " AND (size(" + text + ")<20 OR " + text + "<=" + bind("18446744073709551615") + ")";
        return ("right('00000000000000000000'+coalesce(" + text + ",''),20)", guard);
    }

    internal static string Literal(object value)
    {
        var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        if (number < 0m || number > ulong.MaxValue || decimal.Truncate(number) != number)
            throw new NotSupportedException("UInt64 literals require exact integral values in the declared range.");
        return ((ulong)number).ToString("D20", CultureInfo.InvariantCulture);
    }
}
