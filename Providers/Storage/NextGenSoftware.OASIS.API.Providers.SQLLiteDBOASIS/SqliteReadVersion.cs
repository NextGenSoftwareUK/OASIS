using System;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class SqliteReadVersion
{
    public static void Validate(int version)
    {
        if(version<0) throw new ArgumentOutOfRangeException(nameof(version),"Version must be zero (latest) or a positive stored version.");
    }
}
