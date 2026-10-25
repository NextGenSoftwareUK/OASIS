using System;
using NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests
{
    /// <summary>
    /// Uses explicit connection configuration, without guessing arguments or hiding errors.
    /// </summary>
    internal static class OracleDBOASISTestFactory
    {
        public static OracleDBOASIS Create() => Create(
            Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));

        internal static OracleDBOASIS Create(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ORACLEDBOASIS_CONNECTIONSTRING must explicitly identify an authorized test database.");

            // Construction never activates the provider or provisions a schema.
            return new OracleDBOASIS(connectionString);
        }
    }
}
