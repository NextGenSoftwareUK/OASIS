using System;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class SqliteReadVersion
{
    internal static void ValidateExport(int version)
    {
        if(version!=-1)Validate(version);
    }
    public static void Validate(int version)
    {
        if(version<0) throw new ArgumentOutOfRangeException(nameof(version),"Version must be zero (latest) or a positive stored version.");
    }
}
