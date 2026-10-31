using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SeiOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class SeiPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "SeiAccountAddress";
    private static readonly Network Mainnet = new(PType.SeiOASIS, "Sei", "0x531",
        "0xf9d3845df25b43b1c6926f3ceda6845c17f5624e12212fd8847d0ba01da1ab9e", AddressMetadata, "/", BlockHashWireFormat.MixedCase);

    public SeiPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static SeiPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new SeiPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
