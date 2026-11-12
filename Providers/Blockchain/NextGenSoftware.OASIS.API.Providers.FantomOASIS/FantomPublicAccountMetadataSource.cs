using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS;

/// <summary>Explicit Opera canonical-at-read balances; not finalized or Sonic state. No signer/storage authority.</summary>
public sealed class FantomPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "FantomAccountAddress";
    private static readonly Network Mainnet = new(PType.FantomOASIS, "Fantom", "0xfa",
        "0x0000000000000000c20dbfb2ec18ae20037c716f3ba2d9e1da768a9deca17cb4", AddressMetadata, "/") { ReadPolicy = ReadBlockPolicy.LatestCanonicalAtRead };

    public FantomPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static FantomPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new FantomPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
