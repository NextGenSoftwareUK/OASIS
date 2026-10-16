using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private async Task<OASISResult<ITransactionResponse>> MintExistingErc20Async(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || request.MintedByAvatarId == Guid.Empty || request.Amount <= 0
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.TokenAddress)
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.ToWalletAddress)
                || string.Equals(request.ToWalletAddress, "0x" + new string('0', 40), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Mint requires a minter avatar, positive amount, existing token contract and nonzero recipient.");
            var keys = ResolveTokenSigningKeys(request.MintedByAvatarId);
            if (keys.IsError)
            {
                OASISErrorHandling.HandleError(ref result, keys.Message, keys.Exception);
                result.ErrorCode = keys.ErrorCode;
                return result;
            }
            var selected=await NextGenSoftware.OASIS.Providers.Shared.Evm.EvmMintAccountSelection.SelectAsync(
                keys.Result,ChainId,()=>ResolveDefaultTokenMintWalletAsync(request.MintedByAvatarId));
            if(selected.IsError)
            {
                OASISErrorHandling.HandleError(ref result,selected.Message,selected.Exception);result.ErrorCode=selected.ErrorCode;return result;
            }
            return await ExecuteErc20MutationCoreAsync(selected.Result.PrivateKey, request.TokenAddress, request.ToWalletAddress, request.Amount, TokenMutation.Mint);
        }
        catch (Exception cause)
        {
            OASISErrorHandling.HandleError(ref result, "Ethereum token mint preparation failed.", cause);
            if (cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }

    // Existing KeyManager authorization, configured quantum/Rijndael key handling and wallet storage remain authoritative.
    protected virtual OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId)
        => KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.EthereumOASIS);

    protected virtual async Task<OASISResult<string>> ResolveDefaultTokenMintWalletAsync(Guid avatarId)
    {
        var wallet=await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId,Core.Enums.ProviderType.EthereumOASIS);
        return new OASISResult<string>{Result=wallet.Result?.WalletAddress,IsError=wallet.IsError,
            Message=wallet.Message,Exception=wallet.Exception,ErrorCode=wallet.ErrorCode};
    }
}
