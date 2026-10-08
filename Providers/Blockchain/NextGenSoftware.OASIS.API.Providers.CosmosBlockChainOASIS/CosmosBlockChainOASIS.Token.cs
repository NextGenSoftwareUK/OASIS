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
            var key = await SigningKeyAsync(Guid.Empty, request.OwnerPrivateKey, request.OwnerSeedPhrase);
            if (request.FromTokenAddress == _nativeDenom)
                return await SendBankTransferAsync(request.FromWalletAddress, request.ToWalletAddress, request.Amount, request.MemoText, _nativeDenom, key);
            var hash = await ExecuteAssetAsync(new { token_transfer = new
            {
                symbol = request.FromTokenAddress, recipient = request.ToWalletAddress, amount = AssetUnits(request.Amount)
            } }, key, request.FromWalletAddress);
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
            var amount = AssetUnits(request.Amount);
            var key = await SigningKeyAsync(Guid.Empty);
            var issuer = await SignerAddressAsync(key);
            var hash = await ExecuteAssetAsync(new { token_mint = new
            {
                symbol = request.Symbol, recipient = issuer, amount,
                metadata_json = JsonSerializer.Serialize(new { request.Title, request.Description, request.MetaData, request.Tags, request.MintedByAvatarId })
            } }, key, issuer);
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
            var key = await SigningKeyAsync(request.BurntByAvatarId, request.OwnerPrivateKey, request.OwnerSeedPhrase);
            var hash = await ExecuteAssetAsync(new { token_burn = new { symbol = request.TokenAddress } }, key, request.OwnerPublicKey);
            result.Result = new TransactionResponse { TransactionResult = hash };
            result.Message = "Sender's complete balance burned and on-chain supply reduced.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        => LockTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        => ChangeTokenLockAsync(request?.TokenAddress, true, request?.LockedByAvatarId ?? Guid.Empty, request?.FromWalletPrivateKey, request?.FromWalletAddress);
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        => UnlockTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        => ChangeTokenLockAsync(request?.TokenAddress, false, request?.UnlockedByAvatarId ?? Guid.Empty);

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
            if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Token identity and wallet address are required.");
            var balance = await QueryAssetAsync(new { token_balance = new { symbol, address } });
            var units = BigInteger.Parse(balance.GetString(), CultureInfo.InvariantCulture);
            // Divide before converting to decimal: Uint128 base units can exceed decimal's integer range.
            var scale = BigInteger.Pow(10, AssetDecimals);
            var whole = BigInteger.DivRem(units, scale, out var fraction);
            result.Result = (decimal)whole + (decimal)fraction / 100000000m;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public Task<OASISResult<JsonElement>> GetCustomTokenAsync(string symbol)
        => QueryContractAsync(_contractAddress, new { asset = new { message = new { token = new { symbol } } } });

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        => GetBalanceAsync(request).GetAwaiter().GetResult();
}
