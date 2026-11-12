using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class ArbitrumPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "ArbitrumAccountAddress";
    private static readonly Network Mainnet = new(PType.ArbitrumOASIS, "Arbitrum", "0xa4b1",
        "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442", AddressMetadata, "/rpc");

    public ArbitrumPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static ArbitrumPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new ArbitrumPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
