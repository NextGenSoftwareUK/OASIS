using System;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// Resolves the single MongoDB deployment used by WEB4. Railway may override the URI without
    /// duplicating it in source; otherwise accounting reuses MongoDBOASIS from the loaded OASIS DNA.
    /// The accounting database remains an explicit, dedicated database on that deployment.
    /// </summary>
    public static class SubscriptionMongoConfiguration
    {
        public const string ConnectionSetting = "SUBSCRIPTION_MONGODB_CONNECTION_STRING";
        public const string DatabaseSetting = "SUBSCRIPTION_MONGODB_DATABASE";
        public const string DnaPathSetting = "OASIS_DNA_PATH";
        public const string InitialStateSetting = "SUBSCRIPTION_LEDGER_INITIAL_STATE";
        public const string EmptyInstallation = "empty-new-installation";
        public const string DefaultDatabase = "oasis_subscription_accounting";

        public static string ConnectionString(IConfiguration configuration) =>
            ConnectionString(configuration, ResolveDnaConnectionString);

        public static string ConnectionString(IConfiguration configuration, Func<IConfiguration, string> dnaResolver)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(dnaResolver);
            string explicitlyConfigured = configuration[ConnectionSetting];
            if (!string.IsNullOrWhiteSpace(explicitlyConfigured))
                return explicitlyConfigured;

            string dnaConnection = dnaResolver(configuration);
            return !string.IsNullOrWhiteSpace(dnaConnection)
                ? dnaConnection
                : throw new InvalidOperationException(
                    $"MongoDB is not configured. Set {ConnectionSetting}, or configure " +
                    "OASIS.StorageProviders.MongoDBOASIS.ConnectionString in OASIS_DNA.json.");
        }

        public static string DatabaseName(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            string database = configuration[DatabaseSetting];
            return !string.IsNullOrWhiteSpace(database) ? database : DefaultDatabase;
        }

        public static bool InitializeEmptyLedger(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            string value = configuration[InitialStateSetting];
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (!string.Equals(value, EmptyInstallation, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"{InitialStateSetting} must be exactly '{EmptyInstallation}' when initializing a new installation.");
            return true;
        }

        private static string ResolveDnaConnectionString(IConfiguration configuration)
        {
            string connection = OASISDNAManager.OASISDNA?.OASIS?.StorageProviders?.MongoDBOASIS?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(connection))
                return connection;

            string dnaPath = configuration[DnaPathSetting];
            var loaded = string.IsNullOrWhiteSpace(dnaPath)
                ? OASISDNAManager.LoadDNA()
                : OASISDNAManager.LoadDNA(dnaPath);
            if (loaded.IsError || loaded.Result == null)
                return null;

            return loaded.Result.OASIS?.StorageProviders?.MongoDBOASIS?.ConnectionString;
        }
    }
}
