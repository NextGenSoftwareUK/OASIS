using System.Globalization;
using System.Text.Json;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS : IOASISNativeFeeReceiptProvider
{
    /// <summary>Finalized original-transaction fees only. Not rent, transferred value, a quote or spending authority.</summary>
    public async Task<OASISResult<JsonElement>> GetNativeFeeReceiptAsync(string originalTransactionId,CancellationToken cancellationToken=default)
    {
        var result=new OASISResult<JsonElement>();
        try
        {
            RequireNativeBase58(originalTransactionId,64);
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            timeout.Token.ThrowIfCancellationRequested();
            var chain=await _rpcClient.GetGenesisHashAsync().WaitAsync(timeout.Token);
            if(chain==null || !chain.WasSuccessful || chain.HttpStatusCode!=HttpStatusCode.OK)
                throw new InvalidOperationException("Solana native fee chain identity is unavailable.");
            RequireNativeBase58(chain.Result,32);
            var transaction=await _rpcClient.GetTransactionAsync(originalTransactionId,Commitment.Finalized).WaitAsync(timeout.Token);
            if(transaction==null || !transaction.WasSuccessful || transaction.HttpStatusCode!=HttpStatusCode.OK
                || string.IsNullOrWhiteSpace(transaction.RawRpcResponse))
                throw new InvalidOperationException("Solana original finalized fee receipt is unavailable.");
            // Raw presence/type checks are required: SDK value-type defaults cannot
            // distinguish a genuinely zero fee from missing or malformed metadata.
            using var document=JsonDocument.Parse(transaction.RawRpcResponse);
            if(document.RootElement.TryGetProperty("error",out _))
                throw new InvalidOperationException("Solana native fee RPC returned an error.");
            result.Result=MapSolanaNativeFeeReceipt(originalTransactionId,chain.Result,document.RootElement.GetProperty("result"));
            result.IsLoaded=true;
            result.Message="Observed finalized native transaction fee only; excludes rent/transfers and does not authorize spending or settlement.";
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private static void RequireNativeBase58(string value,int bytes)
    {
        if(string.IsNullOrWhiteSpace(value) || value.Length>88)
            throw new ArgumentException("Canonical native Solana identity is required.");
        var decoded=Encoders.Base58.DecodeData(value);
        if(decoded.Length!=bytes || Encoders.Base58.EncodeData(decoded)!=value)
            throw new ArgumentException("Canonical native Solana identity has an invalid byte length.");
    }

    internal static JsonElement MapSolanaNativeFeeReceipt(string signature,string genesis,JsonElement native)
    {
        RequireNativeBase58(signature,64);RequireNativeBase58(genesis,32);
        var slot=native.GetProperty("slot").GetUInt64();
        if(slot==0)throw new InvalidOperationException("A finalized native transaction slot is required.");
        var transaction=native.GetProperty("transaction");
        var signatures=transaction.GetProperty("signatures");
        if(signatures.ValueKind!=JsonValueKind.Array || signatures.GetArrayLength() is <1 or >256
            || signatures[0].GetString()!=signature)
            throw new InvalidOperationException("Solana fee receipt signature differs from its original reference.");
        foreach(var signed in signatures.EnumerateArray())RequireNativeBase58(signed.GetString(),64);
        var message=transaction.GetProperty("message");
        var keys=message.GetProperty("accountKeys");
        if(keys.ValueKind!=JsonValueKind.Array || keys.GetArrayLength() is <1 or >256)
            throw new InvalidOperationException("Solana native fee payer identity is missing.");
        var payer=keys[0].GetString();RequireNativeBase58(payer,32);
        var required=message.GetProperty("header").GetProperty("numRequiredSignatures").GetInt32();
        if(required!=signatures.GetArrayLength() || required>keys.GetArrayLength())
            throw new InvalidOperationException("Solana native fee payer is not bound to the signed message header.");
        var meta=native.GetProperty("meta");
        var fee=meta.GetProperty("fee").GetUInt64();
        var error=meta.GetProperty("err");
        if(error.ValueKind is not (JsonValueKind.Null or JsonValueKind.Object or JsonValueKind.String))
            throw new InvalidOperationException("Solana native execution result is malformed.");
        return System.Text.Json.JsonSerializer.SerializeToElement(new{
            transactionHash=signature,chainIdentifier=genesis,payer,currency="SOL",unit="lamport",
            netUnits=fee.ToString(CultureInfo.InvariantCulture),success=error.ValueKind==JsonValueKind.Null,
            slot=slot.ToString(CultureInfo.InvariantCulture),basis="finalized-transaction-meta-fee",
            coverage="transaction-fee-only",excludes=new[]{"rent","transferred-value"}
        });
    }
}
