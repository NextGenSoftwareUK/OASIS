using Google.Protobuf;
using Grpc.Core;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronFeeScheduleTests
{
    [Theory]
    [InlineData("valid")][InlineData("advanced")][InlineData("reorg")][InlineData("wrong-genesis")]
    [InlineData("duplicate")][InlineData("empty")][InlineData("maintenance")][InlineData("bad-height")][InlineData("bad-hash")]
    [InlineData("cancelled")]
    [InlineData("missing-header")][InlineData("fork")][InlineData("oversized")][InlineData("control-key")][InlineData("blank-key")]
    public async Task OfficialSdkReadsScheduleWithoutSigningAndNeverClaimsAdmission(string mode)
    {
        var calls=0;var heads=0;var time=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        BlockExtention Head(long height){
            var raw=new BlockHeader.Types.raw{Number=height,Timestamp=time+height+(mode=="fork"&&heads>1?1:0)};
            var hash=System.Security.Cryptography.SHA256.HashData(raw.ToByteArray());
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(hash.AsSpan(0,8),height);
            if(mode=="bad-height")hash[7]++;
            if(mode=="bad-hash")hash[31]^=1;
            return new BlockExtention{Blockid=ByteString.CopyFrom(hash),BlockHeader=mode=="missing-header"?null:new BlockHeader{RawData=raw}};
        }
        var parameters=new ChainParameters();
        if(mode!="empty")parameters.ChainParameter.Add(new ChainParameters.Types.ChainParameter{Key="getTransactionFee",Value=1000});
        parameters.ChainParameter.Add(new ChainParameters.Types.ChainParameter{Key=mode=="duplicate"?"getTransactionFee":"getMemoFee",Value=0});
        if(mode=="empty")parameters.ChainParameter.Clear();
        if(mode=="control-key")parameters.ChainParameter[0].Key="fee\n";
        if(mode=="blank-key")parameters.ChainParameter[0].Key=" ";
        if(mode=="oversized")for(var i=0;i<257;i++)parameters.ChainParameter.Add(new ChainParameters.Types.ChainParameter{Key="parameter"+i,Value=i});
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>{calls++;return Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(mode=="wrong-genesis"?Enumerable.Repeat((byte)1,32).ToArray():new byte[32])});})
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>{calls++;return Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(new byte[32])});})
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetNowBlock2",EmptyMessage.Parser,BlockExtention.Parser),
                (EmptyMessage _,ServerCallContext _)=>{calls++;heads++;return Task.FromResult(Head(heads==1?42:mode=="advanced"?43:mode=="reorg"?41:42));})
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetChainParameters",EmptyMessage.Parser,ChainParameters.Parser),
                (EmptyMessage _,ServerCallContext _)=>{calls++;return Task.FromResult(parameters);})
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetNextMaintenanceTime",EmptyMessage.Parser,NumberMessage.Parser),
                (EmptyMessage _,ServerCallContext _)=>{calls++;return Task.FromResult(new NumberMessage{Num=mode=="maintenance"?time:time+3600000});}).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",ServerPort.PickUnused,ServerCredentials.Insecure)}};server.Start();
        try{
            var endpoint=new Uri("http://127.0.0.1:"+server.Ports.Single().BoundPort);
            await using var runtime=new TronSdkRuntime(endpoint,endpoint,new string('0',64));
            using var cancelled=new CancellationTokenSource();if(mode=="cancelled")cancelled.Cancel();
            if(mode is "valid" or "advanced"){
                var snapshot=await runtime.NativeTransfers.ReadFeeScheduleAsync(cancelled.Token);
                Assert.Equal(6,calls);Assert.Equal(42,snapshot.BeforeHeight);Assert.Equal(mode=="advanced"?43:42,snapshot.AfterHeight);
                Assert.Equal(mode=="valid",snapshot.HeadUnchanged);Assert.False(snapshot.IsFeeAdmissionProof);
                Assert.Equal(1000,snapshot.Parameters["getTransactionFee"]);Assert.Equal(0,snapshot.Parameters["getMemoFee"]);
                Assert.False(snapshot.Parameters.ContainsKey("getCreateAccountFee")); // Never manufacture an omitted fee.
                Assert.Throws<NotSupportedException>(()=>((IDictionary<string,long>)snapshot.Parameters).Add("fake",1));
            }else{
                if(mode=="cancelled"){
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>runtime.NativeTransfers.ReadFeeScheduleAsync(cancelled.Token));Assert.Equal(0,calls);
                }else if(mode=="wrong-genesis"){
                    await Assert.ThrowsAsync<InvalidOperationException>(()=>runtime.NativeTransfers.ReadFeeScheduleAsync(cancelled.Token));Assert.Equal(1,calls);
                }else{
                    await Assert.ThrowsAsync<InvalidDataException>(()=>runtime.NativeTransfers.ReadFeeScheduleAsync(cancelled.Token));Assert.Equal(6,calls);
                }
            }
        }finally{await server.ShutdownAsync();}
    }
}
