using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.FlyIOOASIS
{
    /// <summary>
    /// Fly.io has no key-value API of its own; its storage services are Tigris (see TigrisOASIS), LiteFS and
    /// Fly Managed Postgres. This provider stores OASIS data in Fly Managed Postgres, using the PostgreSQL
    /// provider's complete Npgsql implementation with the cluster connection string (fly mpg / dashboard).
    /// </summary>
    public class FlyIOOASIS : PostgreSQLOASIS.PostgreSQLOASIS
    {
        public FlyIOOASIS(string connectionString) : base(Require(connectionString))
        {
            ProviderName = "FlyIOOASIS";
            ProviderDescription = "Fly.io Managed Postgres provider (Npgsql, Postgres wire protocol).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FlyIOOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static string Require(string connectionString)
            => string.IsNullOrWhiteSpace(connectionString)
                ? throw new ArgumentException("A Fly Managed Postgres connection string is required.", nameof(connectionString))
                : connectionString;
    }
}
