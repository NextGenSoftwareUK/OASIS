using System;
using System.Collections.Generic;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.RoninOASIS;

public sealed class RoninPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "RoninAccountAddress";
    private static readonly Network Mainnet = new(PType.RoninOASIS, "Ronin", "0x7e4",
        "0x456ac0552c86e356a3f7517d8cde08f3a56309f18c77512760024ff8244af978", AddressMetadata, "/rpc/");

    public RoninPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static RoninPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new RoninPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
