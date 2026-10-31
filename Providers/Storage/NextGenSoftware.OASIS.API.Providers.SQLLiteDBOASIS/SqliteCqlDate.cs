using System;
using System.Globalization;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

// Stored JSON uses ISO UTC text with zero to seven fractional digits. Compare
// fixed-width calendar text so no Julian-day/double conversion can round ticks.
internal static class SqliteCqlDate
{
    internal static string Literal(DateTime value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);

    internal static string Column(string alias)
    {
        var value = alias + ".value";
        return "(substr(" + value + ",1,19) || '.' || CASE WHEN length(" + value + ") = 20 THEN '0000000' ELSE substr(substr(" + value + ",21,length(" + value + ")-21) || '0000000',1,7) END || 'Z') COLLATE BINARY";
    }

    internal static string Guard(string alias)
    {
        var value = alias + ".value";
        string Part(int offset, int count) => "CAST(substr(" + value + "," + offset + "," + count + ") AS INTEGER)";
        var year = Part(1, 4); var month = Part(6, 2); var day = Part(9, 2);
        var leap = "(" + year + " % 4 = 0 AND (" + year + " % 100 <> 0 OR " + year + " % 400 = 0))";
        var days = "CASE " + month + " WHEN 2 THEN 28 + " + leap + " WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END";
        const string digit = "[0-9]";
        var two = digit + digit; var prefix = two + two + "-" + two + "-" + two + "T" + two + ":" + two + ":" + two;
        return alias + ".type = 'text' AND length(CAST(" + value + " AS BLOB)) = length(" + value + ")"
            + " AND substr(" + value + ",1,19) GLOB '" + prefix + "' AND substr(" + value + ",-1) = 'Z'"
            + " AND (length(" + value + ") = 20 OR (length(" + value + ") BETWEEN 22 AND 28 AND substr(" + value + ",20,1) = '.' AND substr(" + value + ",21,length(" + value + ")-21) NOT GLOB '*[^0-9]*'))"
            + " AND " + year + " BETWEEN 1 AND 9999 AND " + month + " BETWEEN 1 AND 12 AND " + day + " BETWEEN 1 AND (" + days + ")"
            + " AND " + Part(12, 2) + " BETWEEN 0 AND 23 AND " + Part(15, 2) + " BETWEEN 0 AND 59 AND " + Part(18, 2) + " BETWEEN 0 AND 59";
    }
}
