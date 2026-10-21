using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class Neo4jProviderFactory
{
    internal static Neo4jOASIS Create(Neo4jOASISSettings settings, string overrideConnectionString = null)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        var host = overrideConnectionString ?? settings.ConnectionString;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
            throw new ArgumentException("Neo4j requires an explicit connection string, username and password.", nameof(settings));
        return new Neo4jOASIS(host, settings.Username, settings.Password);
    }
}
