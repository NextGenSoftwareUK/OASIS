using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS
{
    private async Task<OASISResult<ITransactionResponse>> SendNamedAvatarTransferAsync(
        string sender, string recipient, decimal amount, string token, bool byEmail)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(recipient)
                || string.Equals(sender, recipient, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Distinct sender and recipient avatar identities are required.");
            if (token != "HBAR" && !System.Text.RegularExpressions.Regex.IsMatch(token ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                throw new ArgumentException("An explicit HBAR or numeric HTS token ID is required.");
            var from = byEmail
                ? await WalletManager.GetAvatarDefaultWalletByEmailAsync(sender, Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true)
                : await WalletManager.GetAvatarDefaultWalletByUsernameAsync(sender, showPrivateKeys: true, providerType: Core.Enums.ProviderType.HashgraphOASIS);
            var to = byEmail
                ? await WalletManager.GetAvatarDefaultWalletByEmailAsync(recipient, Core.Enums.ProviderType.HashgraphOASIS)
                : await WalletManager.GetAvatarDefaultWalletByUsernameAsync(recipient, providerType: Core.Enums.ProviderType.HashgraphOASIS);
            if (from.IsError || to.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet, ITransactionResponse>(from.IsError ? from : to);
            if (from.Result == null || to.Result == null)
                throw new InvalidOperationException("Could not resolve the avatars' Hedera default wallets.");
            return await SendTokenAsync(new SendWeb3TokenRequest {
                FromWalletAddress = from.Result.WalletAddress, ToWalletAddress = to.Result.WalletAddress,
                Amount = amount, FromTokenAddress = token, OwnerPrivateKey = from.Result.PrivateKey,
                OwnerPublicKey = from.Result.PublicKey, MemoText = "OASIS avatar transfer"
            });
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, "Hedera avatar transfer failed: " + ex.Message, ex); }
        return result;
    }
}
