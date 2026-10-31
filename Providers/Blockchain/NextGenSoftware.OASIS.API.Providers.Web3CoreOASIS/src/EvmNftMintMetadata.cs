using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// One source-linked public NFT envelope for shared Web3 and standalone Ethereum.
// Request authority, recipient identity and routing controls are not NFT metadata.
internal static class EvmNftMintMetadata
{
    internal static string Serialize(IMintWeb3NFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return JsonConvert.SerializeObject(new {
            request.JSONMetaDataURL,
            JSONMetaData = request.StoreNFTMetaDataOnChain == true ? request.JSONMetaData : null });
    }
}
