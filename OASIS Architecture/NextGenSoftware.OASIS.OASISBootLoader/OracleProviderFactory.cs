using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class OracleProviderFactory
{
    internal static OracleDBOASIS Create(OracleDBOASISSettings settings, string overrideConnectionString = null)
    {
        var connectionString = overrideConnectionString ?? settings?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("OracleDBOASIS requires an explicit connection string.", nameof(settings));

        return new OracleDBOASIS(connectionString) { IsVersionControlEnabled=settings?.IsVersionControlEnabled??false };
    }
}
