using System;
using System.Collections.Generic;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RootstockOASIS;

/// <summary>Explicit anonymous Rootstock mainnet balance representation, separate from signed storage.
/// Rootstock nodes reject the finalized tag and EIP-1898 requireCanonical, and Rootstock documents
/// finality as confirmation depth, so reads pin the block 100 confirmations below the head and
/// fence its hash at that height after the read.</summary>
public sealed class RootstockPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "RootstockAccountAddress";
    public const int Confirmations = 100;
    private static readonly Network Mainnet = new(PType.RootstockOASIS, "Rootstock", "0x1e",
        "0xf88529d4ab262c0f4d042e9d8d3f2472848eaafe1a9b7213f57617eb40a9f9e0", AddressMetadata, "/")
        { ReadPolicy = ReadBlockPolicy.ConfirmationDepth, Confirmations = Confirmations, SupportsRequireCanonical = false };

    public RootstockPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static RootstockPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new RootstockPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
