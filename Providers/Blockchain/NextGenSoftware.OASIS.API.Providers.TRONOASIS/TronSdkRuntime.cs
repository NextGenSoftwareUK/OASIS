using System;
using System.Threading.Tasks;
using System.Threading;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using TronNet;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Owns one reusable SDK service container and two reusable node channels.</summary>
public sealed class TronSdkRuntime : IAsyncDisposable
{
    private readonly Channel _fullNode;
    private readonly Channel _solidityNode;
    private readonly ServiceProvider _services;

    public TronNativeTransferClient NativeTransfers { get; }

    public TronSdkRuntime(Uri fullNode, Uri solidityNode, string genesisBlockId, string apiKey = null, long contractFeeLimitSun = 100000000)
    {
        if (contractFeeLimitSun <= 0) throw new ArgumentOutOfRangeException(nameof(contractFeeLimitSun));
        ValidateEndpoint(fullNode);
        ValidateEndpoint(solidityNode);
        _fullNode = CreateChannel(fullNode);
        try
        {
            _solidityNode = CreateChannel(solidityNode);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTronNet(options =>
            {
                // Current TRON networks share the 0x41 address encoding. Network identity
                // is checked using the configured genesis block, not this SDK enum.
                options.Network = TronNetwork.MainNet;
                options.ApiKey = apiKey ?? string.Empty;
            });
            services.AddSingleton<IGrpcChannelClient>(new SharedChannels(_fullNode, _solidityNode));
            _services = services.BuildServiceProvider();
            NativeTransfers = new TronNativeTransferClient(_services.GetRequiredService<IWalletClient>(),
                _services.GetRequiredService<ITransactionClient>(), genesisBlockId, contractFeeLimitSun);
        }
        catch
        {
            _services?.Dispose();
            _fullNode.ShutdownAsync().GetAwaiter().GetResult();
            _solidityNode?.ShutdownAsync().GetAwaiter().GetResult();
            throw;
        }
    }

    /// <summary>Execution-owned client view; reuses the installed services/channels without ambient callback mutation.</summary>
    public TronNativeTransferClient CreatePreparedNativeTransfers(Func<string,string,string,CancellationToken,Task> persistPreparedSubmission)
    {
        ArgumentNullException.ThrowIfNull(persistPreparedSubmission);
        return NativeTransfers.WithPreparation(persistPreparedSubmission);
    }

    /// <summary>Caller owns native admission, trusted sender/permissions and one unique attempt correlation.</summary>
    public TronNativeTransferClient CreatePreparedNativeTransfers(UsageExecutionHandle execution,string clientRequestId,byte[] expectedSender)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);
        if(expectedSender==null||expectedSender.Length!=21||expectedSender[0]!=0x41)
            throw new ArgumentException("Trusted 21-byte TRON sender required.",nameof(expectedSender));
        var sender=(byte[])expectedSender.Clone();
        return CreatePreparedNativeTransfers(async(id,genesis,bytes,token)=>{
            if(genesis!=NativeTransfers.ExpectedGenesisBlockId)throw new InvalidOperationException("Prepared TRON genesis identity mismatch.");
            TronPreparationVerifier.VerifyOriginal(bytes,id,sender,NativeTransfers.MaximumContractFeeLimitSun);
            await execution.BeginPreparedNativeAttemptAsync("TRONOASIS",null,clientRequestId,id,new UsageNativePreparation{
                ChainId=genesis,Sender=Convert.ToHexString(sender).ToLowerInvariant(),SignedTransactionBase64=bytes
            },token);
        });
    }

    /// <summary>Refreshes actual solidified account permission authority before each prepared evidence write.</summary>
    public TronNativeTransferClient CreatePreparedNativeTransfersWithAccountAuthority(UsageExecutionHandle execution,string clientRequestId,string expectedOwner)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedOwner);
        return CreatePreparedNativeTransfers(async(id,genesis,bytes,token)=>{
            if(genesis!=NativeTransfers.ExpectedGenesisBlockId)throw new InvalidOperationException("Prepared TRON genesis identity mismatch.");
            var account=await NativeTransfers.LoadPermissionAuthorityAsync(expectedOwner,token);
            TronPreparationVerifier.VerifyWithAccountAuthority(bytes,id,account.Address.ToByteArray(),NativeTransfers.MaximumContractFeeLimitSun,account);
            await execution.BeginPreparedNativeAttemptAsync("TRONOASIS",null,clientRequestId,id,new UsageNativePreparation{
                ChainId=genesis,Sender=Convert.ToHexString(account.Address.ToByteArray()).ToLowerInvariant(),SignedTransactionBase64=bytes
            },token);
        });
    }

    private static void ValidateEndpoint(Uri endpoint)
    {
        if (endpoint == null || !endpoint.IsAbsoluteUri || endpoint.UserInfo.Length != 0
            || endpoint.AbsolutePath != "/" || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0
            || (endpoint.Scheme != "https" && !(endpoint.Scheme == "http" && endpoint.IsLoopback)))
            throw new ArgumentException("TRON gRPC endpoint must be HTTPS, or HTTP on loopback, without credentials, path or query.");
    }

    private static Channel CreateChannel(Uri endpoint) => new Channel(endpoint.Host, endpoint.Port,
        endpoint.Scheme == "https" ? new SslCredentials() : ChannelCredentials.Insecure);

    public async ValueTask DisposeAsync()
    {
        if(System.Threading.Interlocked.Exchange(ref _disposed,1)!=0)return;
        try { await _services.DisposeAsync(); }
        finally { await Task.WhenAll(_fullNode.ShutdownAsync(), _solidityNode.ShutdownAsync()); }
    }
    private int _disposed;

    private sealed class SharedChannels : IGrpcChannelClient
    {
        private readonly Channel _full;
        private readonly Channel _solidity;
        public SharedChannels(Channel full, Channel solidity) { _full = full; _solidity = solidity; }
        public Channel GetProtocol() => _full;
        public Channel GetSolidityProtocol() => _solidity;
    }
}
