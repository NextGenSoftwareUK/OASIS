using Google.Protobuf;
using Grpc.Core;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronReadRuntimeTests
{
    private sealed class NativeReadsOnly(TronNativeTransferClient client) : TRONOASIS(nativeTransfers: client)
    {
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync()
            => throw new InvalidOperationException("Read-only native calls must not activate encrypted storage.");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeReadUsesSdkWithoutStorageOrSigningCredentials(bool status)
    {
        var genesis = ByteString.CopyFrom(new byte[32]); var reads = 0;
        var service = ServerServiceDefinition.CreateBuilder()
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _) => Task.FromResult(new BlockExtention { Blockid = genesis }))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _) => Task.FromResult(new BlockExtention { Blockid = genesis }))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),
                (Account request,ServerCallContext _) => { reads++; return Task.FromResult(new Account { Address = request.Address, Balance = 1 }); })
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),
                (BytesMessage _,ServerCallContext _) => { reads++; return Task.FromResult(new TransactionInfo()); })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetTransactionById",BytesMessage.Parser,Transaction.Parser),
                (BytesMessage _,ServerCallContext _) => { reads++; return Task.FromResult(new Transaction()); }).Build();
        var server = new Server { Services = { service }, Ports = { new ServerPort("127.0.0.1",0,ServerCredentials.Insecure) } }; server.Start();
        try {
            var port = server.Ports.Single().BoundPort;
            await using var runtime = new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),new string('0',64));
            var provider = new NativeReadsOnly(runtime.NativeTransfers);
            if (status) {
                var result = await provider.GetTransactionStatusAsync(new string('a',64));
                Assert.False(result.IsError,result.Message);
                Assert.Equal(Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound,result.Result);
                Assert.Equal(2,reads);
            } else {
                var address = new TronNet.TronECKey(new string('1',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
                var result = await provider.GetAccountBalanceAsync(address);
                Assert.False(result.IsError,result.Message); Assert.Equal(0.000001m,result.Result); Assert.Equal(1,reads);
                var request = new Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest { WalletAddress = address };
                var shared = await provider.GetBalanceAsync(request);
                Assert.False(shared.IsError,shared.Message); Assert.Equal(0.000001d,shared.Result);
                var synchronous = provider.GetBalance(request);
                Assert.False(synchronous.IsError,synchronous.Message); Assert.Equal(0.000001d,synchronous.Result);
                Assert.Equal(3,reads);
            }
            Assert.False(provider.IsProviderActivated);
        } finally { await server.ShutdownAsync(); }
    }
}
