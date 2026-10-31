using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronHistoryRangeTests
{
    private sealed class ReadOnlyProvider(TronNativeTransferClient client):TRONOASIS(nativeTransfers:client)
    {
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync()
            =>throw new InvalidOperationException("History must not activate storage or acquire a signing key.");
    }

    [Theory]
    [InlineData(false,"42","42","42")] [InlineData(true,"42","42","42")]
    [InlineData(false,"42","","42,43")] [InlineData(true,"42","","42,43")]
    [InlineData(false,"","42","41,42")] [InlineData(true,"","42","41,42")]
    [InlineData(false,"0","0","")] [InlineData(true,"0","0","")]
    [InlineData(false,"18446744073709551615","18446744073709551615","")] [InlineData(true,"18446744073709551615","18446744073709551615","")]
    [InlineData(false,"44","43","invalid")] [InlineData(true,"44","43","invalid")]
    public async Task PublicHistoryUsesVerifiedReceiptBlocksAndRejectsReversedRangeBeforeIo(bool synchronous,string from,string to,string expected)
    {
        var address=new TronNet.TronECKey(new string('1',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
        var recipient=new TronNet.TronECKey(new string('2',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
        var owner=ByteString.CopyFrom(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(address));
        var target=ByteString.CopyFrom(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(recipient));
        var transactions=new Dictionary<string,(Transaction Tx,TransactionInfo Receipt)>();
        foreach(var block in new[]{41L,42L,43L}){
            var tx=new Transaction{RawData=new Transaction.Types.raw{Timestamp=1700000000000+block,Data=ByteString.CopyFromUtf8("range-"+block)}};
            tx.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferContract,
                Parameter=Any.Pack(new TransferContract{OwnerAddress=owner,ToAddress=target,Amount=block})});
            var id=ByteString.CopyFrom(SHA256.HashData(tx.RawData.ToByteArray()));
            transactions.Add(Convert.ToHexString(id.Span), (tx,new TransactionInfo{Id=id,BlockNumber=block,BlockTimeStamp=1700000000000+block}));
        }
        var grpcReads=0;var receiptReads=0;var genesis=ByteString.CopyFrom(new byte[32]);
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>{grpcReads++;return Task.FromResult(new BlockExtention{Blockid=genesis});})
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>{grpcReads++;return Task.FromResult(new BlockExtention{Blockid=genesis});})
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionById",BytesMessage.Parser,Transaction.Parser),
                (BytesMessage request,ServerCallContext _)=>{grpcReads++;return Task.FromResult(transactions[Convert.ToHexString(request.Value.Span)].Tx);})
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),
                (BytesMessage request,ServerCallContext _)=>{grpcReads++;receiptReads++;return Task.FromResult(transactions[Convert.ToHexString(request.Value.Span)].Receipt);}).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),new string('0',64));
            var provider=new ReadOnlyProvider(runtime.NativeTransfers);
            var handler=new Index(transactions);using var http=new HttpClient(handler){BaseAddress=new Uri("https://controlled-index.invalid/")};
            typeof(TRONOASIS).GetField("_httpClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,http);
            var request=new Core.Objects.Wallet.Requests.GetWeb3TransactionsRequest{WalletAddress=address,
                FromBlock=from.Length==0?null:ulong.Parse(from),ToBlock=to.Length==0?null:ulong.Parse(to)};
            var result=synchronous?provider.GetTransactions(request):await provider.GetTransactionsAsync(request);
            if(expected=="invalid"){
                Assert.True(result.IsError);Assert.IsType<ArgumentOutOfRangeException>(result.Exception);Assert.Equal(0,grpcReads);Assert.Equal(0,handler.Reads);
            }else{
                Assert.False(result.IsError,result.Message);
                var selected=expected.Length==0?Array.Empty<long>():expected.Split(',').Select(long.Parse).ToArray();
                Assert.Equal(selected.Select(block=>transactions.Single(row=>row.Value.Receipt.BlockNumber==block).Key.ToLowerInvariant()).Order(),result.Result.Select(row=>row.NativeTransactionId).Order());
                Assert.Equal(3,receiptReads); // Bounds use verified SDK receipt heights, never untrusted index JSON.
                foreach(var row in result.Result){Assert.Equal("Success",row.NativeExecutionResult);Assert.Equal(address,row.FromWalletAddress);Assert.Equal(recipient,row.ToWalletAddress);}
            }
            Assert.False(provider.IsProviderActivated);
        }finally{await server.ShutdownAsync();}
    }

    private sealed class Index(Dictionary<string,(Transaction Tx,TransactionInfo Receipt)> transactions):HttpMessageHandler
    {
        public int Reads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Reads++;
            var json=request.Method==HttpMethod.Post?JsonSerializer.Serialize(new{blockID=new string('0',64)}):JsonSerializer.Serialize(new{success=true,
                data=transactions.Select(row=>new{txID=row.Key,block_timestamp=row.Value.Receipt.BlockTimeStamp,block_number=999999}),meta=new{}});
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)});
        }
    }
}
