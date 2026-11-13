using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class AvalanchePublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "AvalancheAccountAddress";
    private static readonly Network Mainnet = new(PType.AvalancheOASIS, "Avalanche", "0xa86a",
        "0x31ced5b9beb7f8782b014660da0cb18cc409f121f408186886e1ca3e8eeca96b", AddressMetadata, "/ext/bc/C/rpc");

    public AvalanchePublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static AvalanchePublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new AvalanchePublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
