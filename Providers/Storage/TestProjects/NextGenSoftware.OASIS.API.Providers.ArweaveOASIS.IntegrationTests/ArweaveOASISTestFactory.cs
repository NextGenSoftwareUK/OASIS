using System;
using NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.IntegrationTests;

internal static class ArweaveOASISTestFactory
{
    public static ArweaveOASIS Create(string gateway = null)
    {
        var wallet = Required("OASIS_ARWEAVE_WALLET_JSON");
        var bridge = Required("OASIS_ARWEAVE_SDK_BRIDGE");
        return new ArweaveOASIS(
            wallet,
            gateway ?? Required("OASIS_ARWEAVE_GATEWAY"),
            bridge,
            Environment.GetEnvironmentVariable("OASIS_ARWEAVE_NODE") ?? "node",
            mineAfterPost: true);
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required real-runtime setting {name} is missing.");
}
