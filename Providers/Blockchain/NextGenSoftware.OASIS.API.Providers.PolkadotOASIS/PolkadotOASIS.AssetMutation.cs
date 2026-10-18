using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private Task<OASISResult<ITransactionResponse>> MintOwnedAssetAsync(IMintWeb3TokenRequest request)
        => ExecuteOwnedMutationAsync(async owner => {
            ArgumentNullException.ThrowIfNull(request);
            // Freeze the request before wallet I/O; authenticated API owns avatar defaulting/privileged overrides.
            var avatar = request.MintedByAvatarId;
            var amount = request.Amount;
            var assetId = AssetField(request.TokenAddress, request.MetaData, "TokenAddress");
            var recipient = AssetField(request.ToWalletAddress, request.MetaData, "MintToWalletAddress");
            var memo = request.MemoText;
            if (avatar == Guid.Empty) throw new ArgumentException("An authenticated minter avatar is required.");
            ValidateAssetFields(assetId, recipient, amount);
            if (request.ProviderType != null && request.ProviderType.Value != Core.Enums.ProviderType.Default
                && request.ProviderType.Value != Core.Enums.ProviderType.PolkadotOASIS)
                throw new ArgumentException("Mint provider conflicts with PolkadotOASIS.");
            if (request.MetaData?.TryGetValue("Amount", out var legacyAmount) == true
                && (!decimal.TryParse(legacyAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
                    || parsed != amount))
                throw new ArgumentException("Metadata Amount conflicts with the typed mint amount.");
            if (!string.IsNullOrEmpty(memo)) throw new ArgumentException("Asset mint memo support is not enabled.");
            owner.Token.ThrowIfCancellationRequested();
            var loaded = await ResolveAssetMintWalletAsync(avatar).ConfigureAwait(false);
            owner.Token.ThrowIfCancellationRequested();
            if (loaded.IsError || loaded.Result == null)
                throw loaded.Exception ?? new InvalidOperationException("The minter's unique default Polkadot wallet could not be loaded/decrypted.");
            var wallet = loaded.Result;
            if (wallet.AvatarId != avatar || wallet.ProviderType != Core.Enums.ProviderType.PolkadotOASIS)
                throw new InvalidOperationException("Default mint wallet does not belong to the requested avatar/provider.");
            // The SDK checks address/key agreement; the admitted owner independently binds the canonical signer before ACK.
            return await PrepareAssetMutationAsync("mint", assetId, wallet.WalletAddress, recipient, amount,
                wallet.PublicKey, wallet.PrivateKey, null, owner.Token).ConfigureAwait(false);
        }, true);

    private Task<OASISResult<ITransactionResponse>> BurnOwnedAssetAsync(IBurnWeb3TokenRequest request)
        => ExecuteOwnedMutationAsync(owner => {
            ArgumentNullException.ThrowIfNull(request);
            if (request.BurntByAvatarId == Guid.Empty) throw new ArgumentException("An authenticated burner avatar is required.");
            // Existing interface supplies no separate burn-target account: explicitly burn the signer's OWN holding.
            // Native pallet Assets additionally requires that signer to hold the admin role; never invent an empty target.
            var assetId = request.TokenAddress;
            var holder = request.OwnerPublicKey;
            var amount = request.Amount;
            var privateKey = request.OwnerPrivateKey;
            var mnemonic = request.OwnerSeedPhrase;
            ValidateAssetFields(assetId, holder, amount);
            return PrepareAssetMutationAsync("burn", assetId, holder, holder, amount,
                holder, privateKey, mnemonic, owner.Token);
        }, true);

    // Reuse existing default-wallet selection, access controls and detached credential decryption.
    // Never pick the first key or fall back to a configured service wallet.
    protected virtual Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId)
        => WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.PolkadotOASIS, showPrivateKeys: true);

    private async Task<OASISResult<JsonElement>> PrepareAssetMutationAsync(string action, string assetId,
        string sender, string recipient, decimal amount, string publicKey, string privateKey, string mnemonic, CancellationToken token)
    {
        var result = new OASISResult<JsonElement>();
        try {
            token.ThrowIfCancellationRequested();
            if (!_isActivated) throw new InvalidOperationException("Activate the verified network before preparing an asset mutation.");
            result.Result = await InvokePolkadotSdkAsync(new { operation = "prepareAssetOperation", endpoint = _rpcEndpoint,
                genesisHash = ReportedGenesisHash, action, assetId, sender, recipient,
                amount = amount.ToString(CultureInfo.InvariantCulture), publicKey, privateKey, mnemonic }, token).ConfigureAwait(false);
            result.Message = "Signed Assets operation prepared using its own metadata precision; not submitted.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }

    private static string AssetField(string typed, Dictionary<string, string> metadata, string alias)
    {
        string legacy = null;
        metadata?.TryGetValue(alias, out legacy);
        if (!string.IsNullOrWhiteSpace(typed) && !string.IsNullOrWhiteSpace(legacy)
            && !string.Equals(typed, legacy, StringComparison.Ordinal))
            throw new ArgumentException("Conflicting typed and metadata asset field: " + alias);
        return string.IsNullOrWhiteSpace(typed) ? legacy : typed;
    }

    private static void ValidateAssetFields(string assetId, string account, decimal amount)
    {
        if (string.IsNullOrEmpty(assetId) || assetId.Length > 39 || (assetId.Length > 1 && assetId[0] == '0')
            || !BigInteger.TryParse(assetId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            || id < 0 || id >= (BigInteger.One << 128))
            throw new ArgumentException("An existing canonical unsigned Assets pallet ID is required, not an EVM contract address.");
        if (amount <= 0 || string.IsNullOrWhiteSpace(account))
            throw new ArgumentException("A positive typed amount and explicit asset account are required.");
    }
}
