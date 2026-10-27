using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    internal enum AvatarTransferSelector { Id, Username, Email }

    internal virtual Task<OASISResult<IProviderWallet>> LoadTransferWalletAsync(AvatarTransferSelector selector, string identity, bool signing)
        => selector switch
        {
            AvatarTransferSelector.Id => WalletManager.GetAvatarDefaultWalletByIdAsync(Guid.Parse(identity), Core.Enums.ProviderType.SolanaOASIS, showPrivateKeys: signing),
            AvatarTransferSelector.Username => WalletManager.GetAvatarDefaultWalletByUsernameAsync(identity, showPrivateKeys: signing, providerType: Core.Enums.ProviderType.SolanaOASIS),
            AvatarTransferSelector.Email => WalletManager.GetAvatarDefaultWalletByEmailAsync(identity, Core.Enums.ProviderType.SolanaOASIS, showPrivateKeys: signing),
            _ => throw new ArgumentOutOfRangeException(nameof(selector))
        };

    private async Task<OASISResult<ITransactionResponse>> SendAvatarTransactionAsync(AvatarTransferSelector selector,
        string from, string to, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(from);
            ArgumentException.ThrowIfNullOrWhiteSpace(to);
            if (selector == AvatarTransferSelector.Id &&
                (!Guid.TryParse(from, out var fromId) || fromId == Guid.Empty || !Guid.TryParse(to, out var toId) || toId == Guid.Empty))
                throw new ArgumentException("Nonempty sender and recipient avatar IDs are required.");
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrWhiteSpace(token)) NativeSolAmount(amount);
            else _ = new PublicKey(token);

            var source = await LoadTransferWalletAsync(selector, from, signing: true);
            if (source == null) throw new InvalidOperationException("Sender wallet lookup returned no acknowledgement.");
            if (source.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,ITransactionResponse>(source);
            var sender = TransferWalletAddress(source.Result);
            if (string.IsNullOrWhiteSpace(source.Result.PrivateKey)) throw new InvalidOperationException("Sender wallet has no signing key.");
            // Validate sender ownership before resolving a recipient or dispatching any chain request.
            _ = ExplicitTokenSigner(source.Result.PrivateKey, sender);
            var key = source.Result.PrivateKey;
            var destination = await LoadTransferWalletAsync(selector, to, signing: false);
            if (destination == null) throw new InvalidOperationException("Recipient wallet lookup returned no acknowledgement.");
            if (destination.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,ITransactionResponse>(destination);
            var recipient = TransferWalletAddress(destination.Result);
            return await SendTokenAsync(new SendWeb3TokenRequest
            {
                FromWalletAddress = sender, ToWalletAddress = recipient, OwnerPublicKey = sender,
                OwnerPrivateKey = key, FromTokenAddress = token, Amount = amount
            });
        }
        catch (Exception cause) { OASISErrorHandling.HandleError(ref result, "Solana avatar transfer failed.", cause); }
        return result;
    }

    private static string TransferWalletAddress(IProviderWallet wallet)
    {
        if (wallet == null || wallet.ProviderType != Core.Enums.ProviderType.SolanaOASIS)
            throw new InvalidOperationException("A Solana provider wallet is required.");
        var address = string.IsNullOrWhiteSpace(wallet.WalletAddress) ? wallet.PublicKey : wallet.WalletAddress;
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        if (!string.IsNullOrWhiteSpace(wallet.PublicKey) && wallet.PublicKey != address)
            throw new ArgumentException("Solana wallet address and public key disagree.");
        _ = new PublicKey(address);
        return address;
    }
}
