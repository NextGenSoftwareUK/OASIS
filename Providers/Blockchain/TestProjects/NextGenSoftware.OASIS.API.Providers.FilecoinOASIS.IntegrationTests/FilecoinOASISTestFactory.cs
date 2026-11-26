using System;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.IntegrationTests
{
    /// <summary>
    /// Builds a FilecoinOASIS (FEVM) provider from explicit FILECOINOASIS_* settings:
    /// HOSTURI (Lotus Ethereum RPC, e.g. http://127.0.0.1:11234/rpc/v1), CHAINPRIVATEKEY,
    /// CONTRACTADDRESS (deployed Web3CoreOASIS contract) and CHAINID (31415926 for the Lotus devnet).
    /// Returns null when HOSTURI is not configured so callers can report that plainly.
    /// </summary>
    internal static class FilecoinOASISTestFactory
    {
        public static FilecoinOASIS? CreateFromEnvironment()
        {
            var host = Environment.GetEnvironmentVariable("FILECOINOASIS_HOSTURI");
            if (string.IsNullOrWhiteSpace(host)) return null;
            return new FilecoinOASIS(host, Required("FILECOINOASIS_CHAINPRIVATEKEY"), Required("FILECOINOASIS_CONTRACTADDRESS"),
                BigInteger.Parse(Required("FILECOINOASIS_CHAINID")));
        }

        private static string Required(string name) =>
            Environment.GetEnvironmentVariable(name) is string value && !string.IsNullOrWhiteSpace(value)
                ? value : throw new InvalidOperationException(name + " is required when FILECOINOASIS_HOSTURI is set.");
    }
}
