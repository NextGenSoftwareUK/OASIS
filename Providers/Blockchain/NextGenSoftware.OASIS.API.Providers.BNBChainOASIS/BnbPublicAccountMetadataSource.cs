using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class BnbPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "BNBChainAccountAddress";
    private static readonly Network Mainnet = new(PType.BNBChainOASIS, "BNBChain", "0x38",
        "0x0d21840abff46b96c84b2ac9e10e4f5cdaeb5693cb665db62a2f3b02d2d57b5b", AddressMetadata, "/");

    public BnbPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static BnbPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new BnbPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
