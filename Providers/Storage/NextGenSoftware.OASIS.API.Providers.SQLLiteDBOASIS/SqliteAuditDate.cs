using System;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class SqliteAuditDate
{
    internal static DateTime Restore(DateTime value,int kind)
    {
        if(kind<0 || kind>2)throw new InvalidDataException("Stored audit timestamp kind is invalid.");
        return DateTime.SpecifyKind(value,(DateTimeKind)kind);
    }
}
