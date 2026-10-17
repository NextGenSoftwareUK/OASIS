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
    [InlineData("balance")] [InlineData("status")]
    [InlineData("nft")] [InlineData("collection")]
    [InlineData("history")] [InlineData("inventory")]
    public async Task NativeReadUsesSdkWithoutStorageOrSigningCredentials(string mode)
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
                (BytesMessage _,ServerCallContext _) => { reads++; return Task.FromResult(new Transaction()); })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"TriggerConstantContract",TriggerSmartContract.Parser,TransactionExtention.Parser),
                (TriggerSmartContract request,ServerCallContext _) => {
                    reads++; var selector = Convert.ToHexString(request.Data.ToByteArray()[..4]); byte[] encoded;
                    if (selector == "6352211E") { encoded = new byte[32]; encoded[31] = 1; }
                    else if (selector == "70A08231") encoded = new byte[32];
                    else {
                        Assert.Equal("C87B56DD",selector);
                        encoded = ((byte[])typeof(TronStorageBackend).GetMethod("Encode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!
                            .Invoke(null,new object[] {"result(string)",new[] {"string"},new object[] {"https://example.invalid/nft/1"}})!)[4..];
                    }
                    var result = new TransactionExtention { Result = new Return { Result = true } };
                    result.ConstantResult.Add(ByteString.CopyFrom(encoded)); return Task.FromResult(result);
                }).Build();
        var server = new Server { Services = { service }, Ports = { new ServerPort("127.0.0.1",0,ServerCredentials.Insecure) } }; server.Start();
        try {
            var port = server.Ports.Single().BoundPort;
            await using var runtime = new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),new string('0',64));
            var provider = new NativeReadsOnly(runtime.NativeTransfers);
            using var http = new HttpClient(new EmptyHistory()) { BaseAddress = new Uri("https://controlled-indexer.invalid/") };
            typeof(TRONOASIS).GetField("_httpClient",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(provider,http);
            var address = new TronNet.TronECKey(new string('1',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
            if (mode == "status") {
                var result = await provider.GetTransactionStatusAsync(new string('a',64));
                Assert.False(result.IsError,result.Message);
                Assert.Equal(Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound,result.Result);
                Assert.Equal(2,reads);
            } else if (mode == "balance") {
                var result = await provider.GetAccountBalanceAsync(address);
                Assert.False(result.IsError,result.Message); Assert.Equal(0.000001m,result.Result); Assert.Equal(1,reads);
                var request = new Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest { WalletAddress = address };
                var shared = await provider.GetBalanceAsync(request);
                Assert.False(shared.IsError,shared.Message); Assert.Equal(0.000001d,shared.Result);
                var synchronous = provider.GetBalance(request);
                Assert.False(synchronous.IsError,synchronous.Message); Assert.Equal(0.000001d,synchronous.Result);
                Assert.Equal(3,reads);
            } else if (mode == "nft") {
                var result = await provider.LoadOnChainNFTDataAsync(address + "/1");
                Assert.False(result.IsError,result.Message); Assert.Equal("https://example.invalid/nft/1",result.Result.JSONMetaDataURL);
                var sync = provider.LoadOnChainNFTData(address + "/1");
                Assert.False(sync.IsError,sync.Message); Assert.Equal(4,reads);
            } else if (mode == "collection") {
                var result = await provider.LoadNFTsForCollectionOwnerAsync(address,address);
                Assert.False(result.IsError,result.Message); Assert.Empty(result.Result); Assert.Equal(2,reads);
            } else if (mode == "history") {
                var result = await provider.GetTransactionsAsync(new Core.Objects.Wallet.Requests.GetWeb3TransactionsRequest { WalletAddress = address });
                Assert.False(result.IsError,result.Message); Assert.Empty(result.Result);
            } else {
                var result = await provider.LoadAllNFTsForMintAddressAsync(address);
                Assert.False(result.IsError,result.Message); Assert.Empty(result.Result);
            }
            Assert.False(provider.IsProviderActivated);
        } finally { await server.ShutdownAsync(); }
    }

    private sealed class EmptyHistory : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var path = request.RequestUri!.AbsolutePath;
            Assert.True(path == "/wallet/getblockbynum" || path.StartsWith("/v1/accounts/",StringComparison.Ordinal));
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(
                path == "/wallet/getblockbynum" ? "{\"blockID\":\"" + new string('0',64) + "\"}"
                    : "{\"success\":true,\"data\":[],\"meta\":{}}") });
        }
    }
}
