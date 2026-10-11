using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        => SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        => SendNamedAvatarTransferAsync(fromAvatarUsername, toAvatarUsername, amount, "0x2::sui::SUI", false);

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        => SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        => SendNamedAvatarTransferAsync(fromAvatarUsername, toAvatarUsername, amount, token, false);

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        => SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        => SendNamedAvatarTransferAsync(fromAvatarEmail, toAvatarEmail, amount, "0x2::sui::SUI", true);

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        => SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        => SendNamedAvatarTransferAsync(fromAvatarEmail, toAvatarEmail, amount, token, true);

    private async Task<OASISResult<ITransactionResponse>> SendNamedAvatarTransferAsync(string sender, string recipient, decimal amount, string token, bool byEmail)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            TokenUnits(amount);
            if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(recipient)
                || string.Equals(sender, recipient, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Distinct sender and recipient avatar identities are required.");
            if (token != "0x2::sui::SUI") ValidateCoinType(token);
            var from = byEmail
                ? await WalletManager.GetAvatarDefaultWalletByEmailAsync(sender, Core.Enums.ProviderType.SuiOASIS, showPrivateKeys: true)
                : await WalletManager.GetAvatarDefaultWalletByUsernameAsync(sender, showPrivateKeys: true, providerType: Core.Enums.ProviderType.SuiOASIS);
            if (from.IsError || from.Result == null || string.IsNullOrWhiteSpace(from.Result.PrivateKey))
                throw new InvalidOperationException("Could not resolve the sender avatar's Sui signing wallet.");
            var to = byEmail
                ? await WalletManager.GetAvatarDefaultWalletByEmailAsync(recipient, Core.Enums.ProviderType.SuiOASIS)
                : await WalletManager.GetAvatarDefaultWalletByUsernameAsync(recipient, providerType: Core.Enums.ProviderType.SuiOASIS);
            if (to.IsError || to.Result == null)
                throw new InvalidOperationException("Could not resolve the recipient avatar's Sui wallet.");
            return await SendTokenAsync(new SendWeb3TokenRequest
            {
                FromWalletAddress = from.Result.WalletAddress, ToWalletAddress = to.Result.WalletAddress,
                Amount = amount, FromTokenAddress = token, OwnerPrivateKey = from.Result.PrivateKey,
                OwnerPublicKey = from.Result.PublicKey
            });
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, "Sui avatar transfer failed: " + ex.Message, ex); }
        return result;
    }
}
