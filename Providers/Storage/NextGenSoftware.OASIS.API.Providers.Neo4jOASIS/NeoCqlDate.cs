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
        return CalendarOperand(kind + "='string'", text, bind);
    }

    private static (string Value, string Guard) CalendarOperand(string stringGuard, string text, Func<object, string> bind)
    {
        string Part(int start, int count) => "toInteger(substring(" + text + "," + start + "," + count + "))";
        var year = Part(0, 4); var month = Part(5, 2); var day = Part(8, 2);
        var leap = "(" + year + "%4=0 AND (" + year + "%100<>0 OR " + year + "%400=0))";
        var days = "CASE " + month + " WHEN 2 THEN CASE WHEN " + leap
            + " THEN 29 ELSE 28 END WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END";
        const string pattern = @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?Z\z";
        var guard = "(" + stringGuard + " AND " + text + " =~ " + bind(pattern)
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
    // A one-element comprehension binds the native temporal value once.
    // UTC text retains 100ns precision; a zoned native instant has an exact epoch.
    // Local datetimes have no UTC meaning and are not inferred from server locale.
    internal static string NativeTicks(string input, Func<object, string> bind)
        => NativeValue(input,bind,instant=>{
        // The decimal digits form integer ticks; no floating epoch conversion or
        // integer division rule can discard the last representable fraction.
        var fraction = "toInteger(substring(right('000000000'+toString(" + instant + ".nanosecond),9),0,7))";
        var ticks = "(" + instant + ".epochSeconds*10000000+621355968000000000+" + fraction + ")";
            return ticks;
        });

    private static string NativeValue(string input,Func<object,string> bind,Func<string,string> select)
    {
        const string source = "nativeUtcSource";
        const string instant = "nativeUtcInstant";
        var calendar = CalendarOperand("true", source, bind);
        var temporal = "CASE WHEN " + source + " IS :: ZONED DATETIME THEN " + source
            + " WHEN " + source + " IS :: STRING THEN CASE WHEN " + calendar.Guard
            + " THEN datetime(" + source + ") ELSE null END ELSE null END";
        var domain = instant + " IS NOT NULL AND " + instant + ".epochSeconds >= -62135596800"
            + " AND " + instant + ".epochSeconds <= 253402300799 AND " + instant + ".nanosecond % 100 = 0";
        var selected=select(instant);
        return "head([" + source + " IN [" + input + "] | head([" + instant + " IN [" + temporal
            + "] | CASE WHEN " + domain + " THEN " + selected + " ELSE null END])])";
    }

    internal static string NativePartCell(string input,Core.Queries.CosmicProjectionFunction function,Func<object,string> bind)
    {
        var selected=NativeValue(input,bind,instant=>{
            var utc="datetime({epochSeconds:"+instant+".epochSeconds,nanosecond:"+instant+".nanosecond,timezone:'UTC'})";
            return NeoCqlUtcDateParts.Value(function,utc);
        });
        return "head([nativeUtcOutputPart IN ["+selected+
            "] | {s:CASE WHEN "+input+" IS NULL THEN 0 WHEN nativeUtcOutputPart IS NOT NULL THEN 2 ELSE 3 END,"+
            "b:8,v:nativeUtcOutputPart}])";
    }

    internal static string NativeCell(string input, Func<object, string> bind)
        => "head([nativeUtcOutputTicks IN [" + NativeTicks(input, bind)
            + "] | {s:CASE WHEN " + input + " IS NULL THEN 0 WHEN nativeUtcOutputTicks IS NOT NULL THEN 2 ELSE 3 END,"
            + "b:8,v:nativeUtcOutputTicks}])";

    internal static string NativeComparison(string input, string operation, string parameter, Func<object, string> bind)
        => "head([nativeUtcBoundTicks IN [" + NativeTicks(input, bind)
            + "] | CASE WHEN nativeUtcBoundTicks IS NOT NULL THEN nativeUtcBoundTicks "
            + operation + " " + parameter + " ELSE false END])";

}
