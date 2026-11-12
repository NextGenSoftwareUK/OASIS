using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MonadOASIS;

/// <summary>Explicit anonymous mainnet balance representation, separate from signed storage.</summary>
public sealed class MonadPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "MonadAccountAddress";
    private static readonly Network Mainnet = new(PType.MonadOASIS, "Monad", "0x8f",
        "0x0c47353304f22b1c15706367d739b850cda80b5c87bbc335014fef3d88deaac9", AddressMetadata, "/");

    public MonadPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static MonadPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new MonadPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
