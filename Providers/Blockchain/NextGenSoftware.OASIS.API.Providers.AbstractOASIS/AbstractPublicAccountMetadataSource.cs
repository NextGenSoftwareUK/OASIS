using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AbstractOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class AbstractPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "AbstractAccountAddress";
    private static readonly Network Mainnet = new(PType.AbstractOASIS, "Abstract", "0xab5",
        "0xe8e77626586f73b955364c7b4bbf0bb7f7685ebd40e852b164633a4acbd3244c", AddressMetadata, "/");

    public AbstractPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static AbstractPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new AbstractPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
