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
        => (await TokenSignerAsync(avatarId, key, seed, publicKey)).PrivateKey;

    private async Task<(string PrivateKey, string Address)> TokenSignerAsync(Guid avatarId = default, string key = null, string seed = null, string publicKey = null)
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
        return (restored.GetProperty("privateKey").GetString(), restored.GetProperty("address").GetString());
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
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            // Capture the complete caller intent before any signer process or wallet await.
            var tokenAddress = request.FromTokenAddress;
            var sender = request.FromWalletAddress;
            var recipient = request.ToWalletAddress;
            var amount = request.Amount;
            var memo = request.MemoText;
            var privateKey = request.OwnerPrivateKey;
            var seed = request.OwnerSeedPhrase;
            var publicKey = request.OwnerPublicKey;
            var native = tokenAddress == "0x2::sui::SUI";
            if (!native) ValidateCoinType(tokenAddress);
            if (!string.IsNullOrEmpty(memo)) throw new ArgumentException("Sui coin transactions have no memo field.");
            var amountUnits = TokenUnits(amount);
            var key = await TokenSigningKeyAsync(key: privateKey, seed: seed, publicKey: publicKey);
            if (native)
            {
                return await SendSuiAsync(sender, recipient, amount, key, memo);
            }
            return await TokenCallAsync("tokenSend", () => Task.FromResult<object>(new {
                privateKey = key, fromWalletAddress = sender, recipient, amountUnits }));
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); return result; }
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        => BurnTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        => TokenCallAsync("tokenBurn", async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateCoinType(request.TokenAddress);
            var amountUnits = TokenUnits(request.Amount);
            return new { amountUnits, privateKey = await TokenSigningKeyAsync(request.BurntByAvatarId, request.OwnerPrivateKey, request.OwnerSeedPhrase, request.OwnerPublicKey) };
        });

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        => LockTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        => TokenEscrowAsync(true, request?.TokenAddress, request?.LockedByAvatarId ?? Guid.Empty, request?.LockId ?? Guid.Empty,
            request?.Amount ?? 0, request?.EscrowContractAddress, request?.FromWalletPrivateKey, request?.FromWalletAddress);

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        => UnlockTokenAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        => TokenEscrowAsync(false, request?.TokenAddress, request?.UnlockedByAvatarId ?? Guid.Empty, request?.LockId ?? Guid.Empty,
            0, request?.EscrowContractAddress);

    public async Task<OASISResult<decimal>> GetTokenBalanceAsync(string walletAddress, string tokenAddress, bool locked = false)
    {
        var result = new OASISResult<decimal>();
        try
        {
            ValidateCoinType(tokenAddress);
            var operation = locked ? (_tokenEscrow == null ? "tokenLockedBalance" : "tokenEscrowBalance") : "tokenBalance";
            var units = await InvokeSdkAsync(operation, new { walletAddress, escrowObjectId = locked ? _tokenEscrow?.EscrowObjectId : null });
            result.Result = TokenBalanceFromUnits(units.GetString());
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    internal static decimal TokenBalanceFromUnits(string units)
    {
        if (!ulong.TryParse(units, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || units != value.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("Sui token balance must contain canonical exact unsigned 64-bit units.");
        return value / 1000000000m;
    }
}
