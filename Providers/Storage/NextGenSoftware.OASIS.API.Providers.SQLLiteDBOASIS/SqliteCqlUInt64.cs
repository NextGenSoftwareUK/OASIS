using System;
using System.Globalization;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

// json_each.value is REAL above Int64.MaxValue. Read the original token
// through json_each's own full path; never reconstruct it from that REAL.
internal static class SqliteCqlUInt64
{
    private static string Raw(string alias) => "(" + alias + ".json -> " + alias + ".fullkey)";

    internal static string Column(string alias)
        => "substr('00000000000000000000'||" + Raw(alias) + ",-20) COLLATE BINARY";

    internal static string Guard(string alias)
    {
        var raw = Raw(alias);
        return alias + ".type='integer' AND length(" + raw + ") BETWEEN 1 AND 20"
            + " AND " + raw + " NOT GLOB '*[^0-9]*'"
            + " AND (length(" + raw + ")<20 OR " + raw + " COLLATE BINARY<='18446744073709551615')";
    }

    internal static string Literal(object value)
    {
        var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        if (number < 0m || number > ulong.MaxValue || decimal.Truncate(number) != number)
            throw new NotSupportedException("UInt64 metadata literals require an exact UInt64 value; mixed fractional/out-of-range comparisons are not enrolled.");
        return ((ulong)number).ToString("D20", CultureInfo.InvariantCulture);
    }
}
