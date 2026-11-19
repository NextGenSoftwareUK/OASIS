using System;
using NextGenSoftware.OASIS.API.Providers.NeonOASIS;

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS.TestHarness
{
    internal static class NeonOASISTestFactory
    {
        public static NeonOASIS Create()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEONOASIS_CONNECTIONSTRING");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Set NEONOASIS_CONNECTIONSTRING to the disposable Neon test database.");

            var poolerSetting = Environment.GetEnvironmentVariable("NEONOASIS_USEPOOLER");
            var usePooler = true;
            if (poolerSetting != null && !bool.TryParse(poolerSetting, out usePooler))
                throw new InvalidOperationException("NEONOASIS_USEPOOLER must be true or false.");

            return new NeonOASIS(connectionString, usePooler);
        }
    }
}
