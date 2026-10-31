using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class PolygonPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "PolygonAccountAddress";
    private static readonly Network Mainnet = new(PType.PolygonOASIS, "Polygon", "0x89",
        "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b", AddressMetadata);

    public PolygonPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static PolygonPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new PolygonPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
