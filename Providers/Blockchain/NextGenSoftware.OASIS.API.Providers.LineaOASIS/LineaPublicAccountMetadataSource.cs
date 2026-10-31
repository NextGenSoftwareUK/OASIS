using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.LineaOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class LineaPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "LineaAccountAddress";
    private static readonly Network Mainnet = new(PType.LineaOASIS, "Linea", "0xe708",
        "0xb6762a65689107b2326364aefc18f94cda413209fab35c00d4af51eaa20ffbc6", AddressMetadata, "/");

    public LineaPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static LineaPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new LineaPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
