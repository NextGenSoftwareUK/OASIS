using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    /// <summary>Bind every physical contract mutation in this explicit scope to an admitted durable usage owner.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);token.ThrowIfCancellationRequested();
        if(string.IsNullOrWhiteSpace(clientRequestId) || clientRequestId.Length>110) throw new ArgumentException("Bounded storage correlation required.",nameof(clientRequestId));
        if(_web3CoreOASIS==null || !_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("Activated EVM provider with explicit chain identity required.");
        if(!string.Equals(expectedSender,_web3CoreOASIS.SigningAddress,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Admitted sender differs from configured EVM signing account.");
        var chain=_expectedChainId.Value.ToString(CultureInfo.InvariantCulture);var sequence=0;
        var sdk=_web3CoreOASIS;
        return sdk.WithNativePreparationAsync(operation,async evidence=>
        {
            token.ThrowIfCancellationRequested();
            var persist=execution.NativePreparationPersistence(ProviderType.Value.ToString(),null,
                clientRequestId+".storage."+Interlocked.Increment(ref sequence),chain,sdk.SigningAddress,(original,cancellation)=>
                {
                    cancellation.ThrowIfCancellationRequested();
                    var encoded=original.GetProperty("signedTransactionBase64").GetString();var bytes=Convert.FromBase64String(encoded);
                    if(Convert.ToBase64String(bytes)!=encoded || !string.Equals(original.GetProperty("contract").GetString(),_contractAddress,StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Original EVM preparation contract or signed encoding differs.");
                    Web3SignedPreparation.Verify(bytes,original.GetProperty("transactionHash").GetString(),_expectedChainId.Value,sdk.SigningAddress,
                        _contractAddress,original.GetProperty("calldata").GetString().HexToByteArray());
                    return Task.CompletedTask;
                });
            await persist(evidence,token);
        });
    }
}
