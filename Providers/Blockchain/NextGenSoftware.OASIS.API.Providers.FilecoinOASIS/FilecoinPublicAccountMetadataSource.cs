using System;
using System.Collections.Generic;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS;

/// <summary>Anonymous FEVM account balances, separate from storage deals and signed writes.
/// Native genesis CID binds Filecoin identity; /rpc/v1 finalized is an RPC observation.</summary>
public sealed class FilecoinPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "FilecoinAccountAddress";
    private static readonly Network Mainnet = new(PType.FilecoinOASIS, "Filecoin", "0x13a",
        null, AddressMetadata, "/rpc/v1")
    {
        GenesisIdentity = GenesisIdentityKind.FilecoinTipsetCid,
        NativeGenesisCid = "bafy2bzacecnamqgqmifpluoeldx7zzglxcljo6oja4vrmtj7432rphldpdmm2"
    };

    public FilecoinPublicAccountMetadataSource(HttpClient client, Uri rpc, string chainId,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : base(client, rpc, chainId, addresses, sourceOwner, ownsClient, Mainnet) { }

    public static FilecoinPublicAccountMetadataSource FromJson(string json)
        => CreateFromJson(json, (client, rpc, chain, addresses, owner, ownsClient) =>
            new FilecoinPublicAccountMetadataSource(client, rpc, chain, addresses, owner, ownsClient));
}
