using System;
using System.Globalization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    private string CoinType => _contractAddress + "::token::TOKEN";

    private void ValidateCoinType(string address)
    {
        if (string.IsNullOrWhiteSpace(_contractAddress) || address != CoinType)
            throw new ArgumentException("Token address must be the configured published OASIS Coin type: " + CoinType);
    }

    private static string TokenUnits(decimal amount)
    {
        var units = checked(amount * 1000000000m);
        if (amount <= 0 || units != decimal.Truncate(units) || units > ulong.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(amount), "Positive exact u64 token units are required (9 decimal places).");
        return units.ToString("0", CultureInfo.InvariantCulture);
    }

    private async Task<string> TokenSigningKeyAsync(Guid avatarId = default, string key = null, string seed = null, string publicKey = null)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(seed))
            throw new ArgumentException("Specify one signing key or seed phrase, not both.");
        if (!string.IsNullOrWhiteSpace(seed))
            key = (await InvokeSdkAsync("restoreAccount", new { seedPhrase = seed })).GetProperty("privateKey").GetString();
        if (string.IsNullOrWhiteSpace(key) && avatarId != Guid.Empty)
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId,
                Core.Enums.ProviderType.SuiOASIS, showPrivateKeys: true);
            if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                throw new InvalidOperationException("Sui signer wallet could not be loaded: " + wallet.Message);
            key = wallet.Result.PrivateKey;
        }
        if (string.IsNullOrWhiteSpace(key)) key = _privateKey;
        var restored = await InvokeSdkAsync("restoreKey", new { privateKey = key });
        if (!string.IsNullOrWhiteSpace(publicKey) && publicKey != restored.GetProperty("publicKey").GetString())
            throw new ArgumentException("Supplied public key does not match the signing key.");
        return restored.GetProperty("privateKey").GetString();
    }

    private async Task<OASISResult<ITransactionResponse>> TokenCallAsync(string operation, Func<Task<object>> request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            var committed = await InvokeSdkAsync(operation, await request());
            var hash = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("Sui returned no committed token transaction digest.");
            result.Result = new TransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        => MintTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        => TokenCallAsync("tokenMint", async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.MetaData == null || !request.MetaData.TryGetValue("TokenAddress", out var tokenAddress)
                || !request.MetaData.TryGetValue("MintToWalletAddress", out var recipient))
                throw new ArgumentException("TokenAddress and MintToWalletAddress metadata are required.");
            ValidateCoinType(tokenAddress);
            if (!string.IsNullOrEmpty(request.MemoText)) throw new ArgumentException("Sui coin transactions have no memo field.");
            var amountUnits = TokenUnits(request.Amount);
            return new { privateKey = await TokenSigningKeyAsync(request.MintedByAvatarId), recipient, amountUnits };
        });

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        => SendTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        if (request?.FromTokenAddress == "0x2::sui::SUI")
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                var key = await TokenSigningKeyAsync(key: request.OwnerPrivateKey, seed: request.OwnerSeedPhrase, publicKey: request.OwnerPublicKey);
                return await SendSuiAsync(request.FromWalletAddress, request.ToWalletAddress, request.Amount, key, request.MemoText);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); return result; }
        }
        return await TokenCallAsync("tokenSend", async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateCoinType(request.FromTokenAddress);
            if (!string.IsNullOrEmpty(request.MemoText)) throw new ArgumentException("Sui coin transactions have no memo field.");
            var amountUnits = TokenUnits(request.Amount);
            return new { privateKey = await TokenSigningKeyAsync(key: request.OwnerPrivateKey, seed: request.OwnerSeedPhrase, publicKey: request.OwnerPublicKey),
                fromWalletAddress = request.FromWalletAddress, recipient = request.ToWalletAddress, amountUnits };
        });
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        => BurnTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        => TokenCallAsync("tokenBurn", async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateCoinType(request.TokenAddress);
            // The interface has no amount: burn every spendable Coin of this type owned by this signer.
            return new { privateKey = await TokenSigningKeyAsync(request.BurntByAvatarId, request.OwnerPrivateKey, request.OwnerSeedPhrase, request.OwnerPublicKey) };
        });

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        => LockTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        => TokenCallAsync("tokenLock", async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateCoinType(request.TokenAddress);
            return new { privateKey = await TokenSigningKeyAsync(request.LockedByAvatarId, request.FromWalletPrivateKey), fromWalletAddress = request.FromWalletAddress };
        });

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        => UnlockTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        => TokenCallAsync("tokenUnlock", async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateCoinType(request.TokenAddress);
            return new { privateKey = await TokenSigningKeyAsync(request.UnlockedByAvatarId) };
        });

    public async Task<OASISResult<decimal>> GetTokenBalanceAsync(string walletAddress, string tokenAddress, bool locked = false)
    {
        var result = new OASISResult<decimal>();
        try
        {
            ValidateCoinType(tokenAddress);
            var units = await InvokeSdkAsync(locked ? "tokenLockedBalance" : "tokenBalance", new { walletAddress });
            result.Result = decimal.Parse(units.GetString(), CultureInfo.InvariantCulture) / 1000000000m;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
