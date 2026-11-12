using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ScrollOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class ScrollPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "ScrollAccountAddress";
    private static readonly Network Mainnet = new(PType.ScrollOASIS, "Scroll", "0x82750",
        "0xbbc05efd412b7cd47a2ed0e5ddfcf87af251e414ea4c801d78b6784513180a80", AddressMetadata, "/");

    public ScrollPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static ScrollPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new ScrollPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
