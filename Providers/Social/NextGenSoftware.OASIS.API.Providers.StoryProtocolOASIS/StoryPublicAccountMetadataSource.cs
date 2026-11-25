using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.StoryProtocolOASIS;

/// <summary>Explicit anonymous Story mainnet (chain 1514) finalized balance representation.
/// Separate from the legacy API-key client; no wallet, IP registration or writes.</summary>
public sealed class StoryPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "StoryAccountAddress";
    private static readonly Network Mainnet = new(PType.StoryProtocolOASIS, "Story", "0x5ea",
        "0x29b83a77e1705524a60557a9494150635360ccd9d1f68278883d2d9e6d4cc6b2", AddressMetadata, "/");

    public StoryPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static StoryPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new StoryPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
