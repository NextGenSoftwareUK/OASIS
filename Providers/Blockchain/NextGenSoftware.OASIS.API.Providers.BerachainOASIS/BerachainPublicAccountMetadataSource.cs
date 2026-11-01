using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BerachainOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class BerachainPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "BerachainAccountAddress";
    private static readonly Network Mainnet = new(PType.BerachainOASIS, "Berachain", "0x138de",
        "0xd57819422128da1c44339fc7956662378c17e2213e669b427ac91cd11dfcfb38", AddressMetadata, "/");

    public BerachainPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static BerachainPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new BerachainPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
