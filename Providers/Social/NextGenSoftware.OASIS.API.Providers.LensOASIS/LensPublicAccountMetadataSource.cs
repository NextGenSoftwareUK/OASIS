using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.LensOASIS;

/// <summary>Explicit anonymous Lens Chain mainnet (chain 232) finalized balance representation.
/// Separate from the legacy Lens API client; no wallet, profile, post or writes.</summary>
public sealed class LensPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "LensAccountAddress";
    private static readonly Network Mainnet = new(PType.LensOASIS, "Lens", "0xe8",
        "0xe8e77626586f73b955364c7b4bbf0bb7f7685ebd40e852b164633a4acbd3244c", AddressMetadata, "/");

    public LensPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static LensPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new LensPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
