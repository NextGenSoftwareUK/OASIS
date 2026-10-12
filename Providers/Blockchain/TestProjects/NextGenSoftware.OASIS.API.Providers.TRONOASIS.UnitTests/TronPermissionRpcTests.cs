using Google.Protobuf;
using Grpc.Core;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronPermissionRpcTests
{
    private static Method<TRequest,TResponse> Rpc<TRequest,TResponse>(string service,string name,MessageParser<TRequest> request,MessageParser<TResponse> response)
        where TRequest:class,IMessage<TRequest> where TResponse:class,IMessage<TResponse>
        =>new(MethodType.Unary,service,name,Marshallers.Create<TRequest>(value=>value.ToByteArray(),bytes=>request.ParseFrom(bytes)),Marshallers.Create<TResponse>(value=>value.ToByteArray(),bytes=>response.ParseFrom(bytes)));
    private sealed class SolidNode(ByteString genesis,bool foreignAccount)
    {
        public int AccountQueries;
        public Task<BlockExtention> GetBlockByNum2(NumberMessage request,ServerCallContext context)
            =>Task.FromResult(new BlockExtention{Blockid=genesis});
        public Task<Account> GetAccount(Account request,ServerCallContext context)
        {
            AccountQueries++;
            var address=request.Address.ToByteArray();if(foreignAccount)address[1]^=1;
            return Task.FromResult(new Account{Address=ByteString.CopyFrom(address),OwnerPermission=new Permission{Id=0,Threshold=1}});
        }
    }
    [Theory]
    [InlineData("valid")] [InlineData("foreign-account")] [InlineData("wrong-genesis")]
    public async Task LoaderUsesActualGrpcAndRejectsMismatchedAuthority(string mode)
    {
        var expected=new string('a',64);var served=ByteString.CopyFrom(Convert.FromHexString(mode=="wrong-genesis"?new string('b',64):expected));
        var solid=new SolidNode(served,mode=="foreign-account");
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),solid.GetBlockByNum2)
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),solid.GetBlockByNum2)
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),solid.GetAccount).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};
        server.Start();
        try{
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),expected);
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var address=new TronNet.TronECKey(new string('0',63)+"1",TronNet.TronNetwork.MainNet).GetPublicAddress();
            if(mode=="valid"){
                var account=await runtime.NativeTransfers.LoadPermissionAuthorityAsync(address,deadline.Token);
                Assert.Equal(21,account.Address.Length);Assert.NotNull(account.OwnerPermission);Assert.Equal(1,solid.AccountQueries);
            }else{
                await Assert.ThrowsAsync<InvalidOperationException>(()=>runtime.NativeTransfers.LoadPermissionAuthorityAsync(address,deadline.Token));
                Assert.Equal(mode=="wrong-genesis"?0:1,solid.AccountQueries);
            }
        }finally{await server.ShutdownAsync();}
    }
}
