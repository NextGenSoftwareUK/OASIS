using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NileOASIS
{
    /// <summary>
    /// Nile is serverless Postgres reached over the standard Postgres wire protocol, so it uses the PostgreSQL provider's
    /// complete Npgsql implementation against the Nile database connection string (from the Nile console).
    /// OASIS tables are created as shared (non-tenant) tables in that database.
    /// </summary>
    public class NileOASIS : PostgreSQLOASIS.PostgreSQLOASIS
    {
        public NileOASIS(string connectionString) : base(RequireNile(connectionString))
        {
            ProviderName = "NileOASIS";
            ProviderDescription = "Nile serverless Postgres provider (Npgsql, Postgres wire protocol).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NileOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static string RequireNile(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A Nile database connection string is required.", nameof(connectionString));
            return connectionString;
        }
    }
}
