using System;
using System.Globalization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// Stored JSON retains UTC calendar text with0..7 fractional digits. Fixed-width
// text preserves100ns ticks without epoch milliseconds, Float or local time.
internal static class NeoCqlDate
{
    internal const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
    internal static string Literal(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value.ToString(Format, CultureInfo.InvariantCulture)
        : throw new ArgumentException("CQL DateTime values must be UTC.");

    internal static (string Value, string Guard) Operand(string kind, string value, Func<object, string> bind)
    {
        var text = "(CASE WHEN " + kind + "='string' THEN " + value + " ELSE null END)";
        string Part(int start, int count) => "toInteger(substring(" + text + "," + start + "," + count + "))";
        var year = Part(0, 4); var month = Part(5, 2); var day = Part(8, 2);
        var leap = "(" + year + "%4=0 AND (" + year + "%100<>0 OR " + year + "%400=0))";
        var days = "CASE " + month + " WHEN 2 THEN CASE WHEN " + leap
            + " THEN 29 ELSE 28 END WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END";
        const string pattern = @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?Z\z";
        var guard = "(" + kind + "='string' AND " + text + " =~ " + bind(pattern)
            + " AND " + year + ">=1 AND " + year + "<=9999 AND " + month + ">=1 AND " + month + "<=12"
            + " AND " + day + ">=1 AND " + day + "<=(" + days + ")"
            + " AND " + Part(11, 2) + ">=0 AND " + Part(11, 2) + "<=23"
            + " AND " + Part(14, 2) + ">=0 AND " + Part(14, 2) + "<=59"
            + " AND " + Part(17, 2) + ">=0 AND " + Part(17, 2) + "<=59)";
        // Guard this length independently: predicate boolean evaluation need not
        // short-circuit, and malformed short strings must not throw in substring.
        var fraction = "CASE WHEN size(" + text + ")>=22 AND size(" + text + ")<=28 THEN substring("
            + text + ",20,size(" + text + ")-21) ELSE '' END";
        return ("(substring(" + text + ",0,19)+'.'+substring((" + fraction + ")+'0000000',0,7)+'Z')", guard);
    }
}
