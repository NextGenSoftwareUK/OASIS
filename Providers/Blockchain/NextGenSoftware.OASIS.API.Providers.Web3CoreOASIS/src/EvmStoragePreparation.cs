using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

internal static class EvmStoragePreparation
{
    internal static Func<JsonElement,CancellationToken,Task> Bind(UsageExecutionHandle execution,string provider,
        string correlation,BigInteger chain,string sender,string contract)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if(string.IsNullOrWhiteSpace(correlation)||correlation.Length>110)throw new ArgumentException("Bounded storage correlation required.",nameof(correlation));
        if(chain<=0||string.IsNullOrWhiteSpace(provider)||string.IsNullOrWhiteSpace(sender)||string.IsNullOrWhiteSpace(contract))throw new ArgumentException("Explicit native storage identity required.");
        var sequence=0;var chainText=chain.ToString(CultureInfo.InvariantCulture);
        return async(evidence,token)=>{
            token.ThrowIfCancellationRequested();
            var persist=execution.NativePreparationPersistence(provider,null,correlation+".storage."+Interlocked.Increment(ref sequence),chainText,sender,(original,cancellation)=>{
                cancellation.ThrowIfCancellationRequested();
                var encoded=original.GetProperty("signedTransactionBase64").GetString();var bytes=Convert.FromBase64String(encoded);
                if(Convert.ToBase64String(bytes)!=encoded||!string.Equals(original.GetProperty("contract").GetString(),contract,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Original EVM preparation contract or signed encoding differs.");
                Web3SignedPreparation.Verify(bytes,original.GetProperty("transactionHash").GetString(),chain,sender,contract,original.GetProperty("calldata").GetString().HexToByteArray());
                return Task.CompletedTask;
            });
            await persist(evidence,token);token.ThrowIfCancellationRequested();
        };
    }
}
