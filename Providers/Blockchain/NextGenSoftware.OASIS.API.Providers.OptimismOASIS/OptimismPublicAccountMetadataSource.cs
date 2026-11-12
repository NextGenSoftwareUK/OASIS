using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class OptimismPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "OptimismAccountAddress";
    private static readonly Network Mainnet = new(PType.OptimismOASIS, "Optimism", "0xa",
        "0x7ca38a1916c42007829c55e69d3e9a73265554b586a499015373241b8a3fa48b", AddressMetadata, "/");

    public OptimismPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static OptimismPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new OptimismPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
