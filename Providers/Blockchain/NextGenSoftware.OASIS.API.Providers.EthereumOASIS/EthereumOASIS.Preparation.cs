using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS:IPreparedNativeStorageProvider
{
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);token.ThrowIfCancellationRequested();
        if(!IsProviderActivated||Web3Client==null||ChainId<=0)throw new InvalidOperationException("Activated Ethereum provider with explicit chain identity required.");
        var sender=Web3Client.TransactionManager.Account.Address;
        if(!string.Equals(expectedSender,sender,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Admitted sender differs from configured Ethereum signing account.");
        var persist=EvmStoragePreparation.Bind(execution,ProviderType.Value.ToString(),clientRequestId,ChainId,sender,ContractAddress);
        return _storagePreparation.RunAsync(operation,persist,token);
    }
}
