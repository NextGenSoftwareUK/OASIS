using System;
using System.Data.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.DuckDBOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MotherDuckOASIS;

/// <summary>Cloud configuration over the canonical DuckDB SQL/record adapter.
/// Hosted SDK/extension/catalog/authentication acceptance must be verified separately.</summary>
public class MotherDuckOASIS : DuckDBOASIS.DuckDBOASIS
{
    public MotherDuckOASIS(string database = "md:oasis", string? token = null)
        : base(CreateConnectionString(database, token))
    {
        ProviderName = "MotherDuckOASIS";
        ProviderDescription = "MotherDuck cloud using shared DuckDB SQL and OASIS record policy";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MotherDuckOASIS);
    }

    internal static string CreateConnectionString(string database, string? token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        if (!database.StartsWith("md:", StringComparison.Ordinal) || database.Contains('#'))
            throw new ArgumentException("A canonical MotherDuck md: database URI is required.", nameof(database));
        var source = database;
        if (!string.IsNullOrEmpty(token))
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("A token cannot contain only whitespace.", nameof(token));
            var query = database.IndexOf('?');
            if (query >= 0)
                foreach (var part in database[(query + 1)..].Split('&'))
                    if (Uri.UnescapeDataString(part.Split('=', 2)[0]).Equals("motherduck_token", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Configure the MotherDuck token in only one place.", nameof(database));
            source += (query >= 0 ? "&" : "?") + "motherduck_token=" + Uri.EscapeDataString(token);
        }
        var builder = new DbConnectionStringBuilder { ["Data Source"] = source };
        return builder.ConnectionString;
    }
}
