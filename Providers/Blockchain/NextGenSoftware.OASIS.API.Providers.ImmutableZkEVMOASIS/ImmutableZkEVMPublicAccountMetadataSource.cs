using System;
using System.Collections.Generic;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.ImmutableZkEVMOASIS;

public sealed class ImmutableZkEVMPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "ImmutableZkEVMAccountAddress";
    private static readonly Network Mainnet = new(PType.ImmutableZkEVMOASIS, "ImmutableZkEVM", "0x343b",
        "0xf2029323f7006d5b4204a4d0eb299b500b143c8529e89f7b2332d83b80a3cf15", AddressMetadata, "/")
        { ReadPolicy = ReadBlockPolicy.LatestCanonicalAtRead };

    public ImmutableZkEVMPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static ImmutableZkEVMPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new ImmutableZkEVMPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
