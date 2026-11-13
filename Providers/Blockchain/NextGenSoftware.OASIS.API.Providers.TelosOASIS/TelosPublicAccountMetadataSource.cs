using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS;

/// <summary>Explicit anonymous Telos EVM balance representation, separate from Antelope accounts and signed storage.</summary>
public sealed class TelosPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "TelosAccountAddress";
    private static readonly Network Mainnet = new(PType.TelosOASIS, "Telos", "0x28",
        "0x36fe7024b760365e3970b7b403e161811c1e626edd68460272fcdfa276272563", AddressMetadata, "/");

    public TelosPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static TelosPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new TelosPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
