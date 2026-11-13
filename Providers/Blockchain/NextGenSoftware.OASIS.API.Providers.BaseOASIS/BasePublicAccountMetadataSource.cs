using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class BasePublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "BaseAccountAddress";
    private static readonly Network Mainnet = new(PType.BaseOASIS, "Base", "0x2105",
        "0xf712aa9241cc24369b143cf6dce85f0902a9731e70d66818a3a5845b296c73dd", AddressMetadata, "/");

    public BasePublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static BasePublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new BasePublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
