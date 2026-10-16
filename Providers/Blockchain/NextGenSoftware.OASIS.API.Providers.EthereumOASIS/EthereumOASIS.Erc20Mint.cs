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
            if (keys.Result == null || keys.Result.Count == 0 || keys.Result.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Mint requires authorized Ethereum signing wallets.");
            var accounts=keys.Result.Select(key=>new Nethereum.Web3.Accounts.Account(key,ChainId))
                .GroupBy(account=>account.Address,StringComparer.OrdinalIgnoreCase).Select(group=>group.First()).ToList();
            var selected=accounts[0];
            if(accounts.Count>1)
            {
                var wallet=await ResolveDefaultTokenMintWalletAsync(request.MintedByAvatarId);
                if(wallet.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,wallet.Message,wallet.Exception);result.ErrorCode=wallet.ErrorCode;return result;
                }
                if(!Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(wallet.Result))
                    throw new InvalidOperationException("Multiple Ethereum wallets require a valid configured default wallet address.");
                selected=accounts.SingleOrDefault(account=>string.Equals(account.Address,wallet.Result,StringComparison.OrdinalIgnoreCase));
                if(selected==null) throw new UnauthorizedAccessException("The default mint wallet has no authorized signing key.");
            }
            return await ExecuteErc20MutationCoreAsync(selected.PrivateKey, request.TokenAddress, request.ToWalletAddress, request.Amount, TokenMutation.Mint);
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
