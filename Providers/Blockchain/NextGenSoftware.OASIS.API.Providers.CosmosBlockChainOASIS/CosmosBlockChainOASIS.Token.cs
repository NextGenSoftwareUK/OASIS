using System;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
    private const int AssetDecimals = 8;
    private static readonly BigInteger MaxAssetUnits = (BigInteger.One << 128) - 1;

    private static string AssetUnits(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Asset amount must be positive.");
        var bits = decimal.GetBits(amount);
        var units = new BigInteger((uint)bits[0]) + (new BigInteger((uint)bits[1]) << 32) + (new BigInteger((uint)bits[2]) << 64);
        var scale = (bits[3] >> 16) & 255;
        if (scale <= AssetDecimals) units *= BigInteger.Pow(10, AssetDecimals - scale);
        else
        {
            units = BigInteger.DivRem(units, BigInteger.Pow(10, scale - AssetDecimals), out var remainder);
            if (remainder != 0) throw new ArgumentException("Custom Cosmos assets support exactly eight decimal places.", nameof(amount));
        }
        if (units > MaxAssetUnits) throw new ArgumentOutOfRangeException(nameof(amount), "Asset amount exceeds Uint128.");
        return units.ToString(CultureInfo.InvariantCulture);
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        => SendTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress)
                || string.IsNullOrWhiteSpace(request.FromWalletAddress) || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                throw new ArgumentException("Token identity and sender/recipient wallet addresses are required.");
            // Capture caller-owned scalar intent before signer recovery can yield.
            var symbol=request.FromTokenAddress;var sender=request.FromWalletAddress;var recipient=request.ToWalletAddress;
            var amount=request.Amount;var memo=NativeWalletMemo.FromText(request.MemoText).Text;
            if(amount<=0)throw new ArgumentOutOfRangeException(nameof(request.Amount),"Transfer amount must be positive.");
            var assetUnits=symbol==_nativeDenom?null:AssetUnits(amount);
            var key = await SigningKeyAsync(Guid.Empty, request.OwnerPrivateKey, request.OwnerSeedPhrase, request.OwnerPublicKey);
            if (symbol == _nativeDenom)
                return await SendBankTransferAsync(sender, recipient, amount, memo, _nativeDenom, key);
            var hash = await ExecuteAssetAsync(new { token_transfer = new
            {
                symbol, recipient, amount = assetUnits
            } }, key, sender,memoText:memo);
            result.Result = new TransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        => MintTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Symbol))
                throw new ArgumentException("Token symbol is required.");
            var symbol = request.Symbol;
            var actor = request.MintedByAvatarId;
            var amount = AssetUnits(request.Amount);
            var memo = NativeWalletMemo.FromText(request.MemoText).Text;
            // Serialize caller-owned collections before signer recovery can yield.
            var metadata = JsonSerializer.Serialize(new { request.Title, request.Description, request.MetaData, request.Tags, request.MintedByAvatarId });
            var key = await SigningKeyAsync(actor);
            var issuer = await SignerAddressAsync(key);
            var hash = await ExecuteAssetAsync(new { token_mint = new
            {
                symbol, recipient = issuer, amount, metadata_json = metadata
            } }, key, issuer, memoText: memo);
            result.Result = new TransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        => BurnTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                throw new ArgumentException("Token identity is required.");
            var symbol = request.TokenAddress;
            var amount = AssetUnits(request.Amount);
            var key = await SigningKeyAsync(request.BurntByAvatarId, request.OwnerPrivateKey, request.OwnerSeedPhrase, request.OwnerPublicKey);
            var hash = await ExecuteAssetAsync(new { token_burn = new { symbol, amount } }, key);
            result.Result = new TransactionResponse { TransactionResult = hash };
            result.Message = "Requested amount burned and on-chain supply reduced.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        => LockTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        => ChangeTokenEscrowAsync(request?.TokenAddress, true, request?.LockedByAvatarId ?? Guid.Empty,
            request?.LockId ?? Guid.Empty, request?.Amount ?? 0, request?.EscrowContractAddress,
            request?.FromWalletPrivateKey, request?.FromWalletAddress);
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        => UnlockTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        => ChangeTokenEscrowAsync(request?.TokenAddress, false, request?.UnlockedByAvatarId ?? Guid.Empty,
            request?.LockId ?? Guid.Empty, 0, request?.EscrowContractAddress);

    private async Task<OASISResult<ITransactionResponse>> ChangeTokenLockAsync(string symbol, bool locked, Guid actor, string suppliedKey = null, string from = null)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Token identity is required.");
            var key = await SigningKeyAsync(actor, suppliedKey);
            var hash = await ExecuteAssetAsync(new { token_set_locked = new { symbol, locked } }, key, from);
            result.Result = new TransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<decimal>> GetCustomTokenBalanceAsync(string symbol, string address)
    {
        var result = new OASISResult<decimal>();
        try
        {
            var balance = await GetCustomTokenBalanceUnitsAsync(symbol, address);
            if (balance.IsError) throw balance.Exception ?? new InvalidOperationException(balance.Message);
            result.Result = CustomTokenBalanceFromUnits(balance.Result);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    /// <summary>Exact canonical Uint128 base units; no floating-point or decimal range conversion.</summary>
    public async Task<OASISResult<string>> GetCustomTokenBalanceUnitsAsync(string symbol, string address)
    {
        var result = new OASISResult<string>();
        try
        {
            if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Token identity and wallet address are required.");
            var balance = await QueryAssetAsync(new { token_balance = new { symbol, address } });
            var encoded = balance.GetString();
            _ = ParseCustomTokenBalanceUnits(encoded);
            result.Result = encoded;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    internal static BigInteger ParseCustomTokenBalanceUnits(string encoded)
    {
        if (!BigInteger.TryParse(encoded, NumberStyles.None, CultureInfo.InvariantCulture, out var units)
            || units.Sign < 0 || units >= (BigInteger.One << 128)
            || encoded != units.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("Cosmos token balance requires canonical unsigned Uint128 units.");
        return units;
    }

    internal static decimal CustomTokenBalanceFromUnits(string encoded)
    {
        var units = ParseCustomTokenBalanceUnits(encoded);
        var whole = BigInteger.DivRem(units, BigInteger.Pow(10, AssetDecimals), out var fraction);
        var value = (decimal)whole + (decimal)fraction / 100000000m;
        if (!units.IsZero && BigInteger.Parse(AssetUnits(value), CultureInfo.InvariantCulture) != units)
            throw new InvalidOperationException("Cosmos token balance cannot be represented exactly by the decimal interface.");
        return value;
    }

    public Task<OASISResult<JsonElement>> GetCustomTokenAsync(string symbol)
        => QueryContractAsync(_contractAddress, new { asset = new { message = new { token = new { symbol } } } });

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        => GetBalanceAsync(request).GetAwaiter().GetResult();
}
