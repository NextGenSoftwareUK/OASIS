using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaTombstoneWireTests
{
    [DataTestMethod]
    [DataRow("missing")][DataRow("writer")][DataRow("payload")]
    [DataRow("cycle")][DataRow("version")][DataRow("bounds")]
    public async Task CorruptCommittedChainCannotReturnAnExactOrCollectionRecord(string corruption)
    {
        var owner=new Account();var key=RandomNumberGenerator.GetBytes(32);
        try{
            var rpc=DispatchProxy.Create<IRpcClient,TombstoneProxy>();var proxy=(TombstoneProxy)(object)rpc;
            proxy.Writer=owner.PublicKey.Key;proxy.IncludePacketsInHistory=true;
            var repository=new SolanaRepository(owner,rpc,()=>key.ToArray());
            var root=await repository.CreateAsync(new SolanaAvatarDetailDto{Id=Guid.NewGuid(),Version=6,XP=997});
            var tail=proxy.Packets.Keys.Reverse().Skip(1).First();
            proxy.RewriteMemo=(reference,memo)=>{
                if(reference==root && corruption is "cycle" or "version" or "bounds"){
                    var fields=memo.Split('|');
                    if(corruption=="cycle")fields[5]=root;
                    if(corruption=="version")fields[0]="oasis-solana-commit-v9";
                    if(corruption=="bounds")fields[2]="257";
                    return string.Join('|',fields);
                }
                return reference==tail && corruption=="payload" ? memo[..^1]+(memo[^1]=='a'?"b":"a") : memo;
            };
            if(corruption=="missing")proxy.MissingReference=tail;
            if(corruption=="writer")proxy.UnexpectedWriterReference=tail;
            if(corruption is "missing" or "writer"){
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>repository.GetAsync<SolanaAvatarDetailDto>(root));
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>repository.GetAllAsync<SolanaAvatarDetailDto>());
            }else{
                await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>repository.GetAsync<SolanaAvatarDetailDto>(root));
                await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>repository.GetAllAsync<SolanaAvatarDetailDto>());
            }
            Assert.AreEqual(proxy.Packets.Count,proxy.Sends,"Read failures must not resubmit transactions.");
        }finally{CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod]
    [DataRow(1)][DataRow(2)][DataRow(-1)]
    public async Task InterruptedLargeWriteRetainsExactEvidenceAndNeverPublishesPartialRecord(int failingSend)
    {
        var owner=new Account();var key=RandomNumberGenerator.GetBytes(32);
        try{
            var rpc=DispatchProxy.Create<IRpcClient,TombstoneProxy>();var proxy=(TombstoneProxy)(object)rpc;
            proxy.Writer=owner.PublicKey.Key;proxy.IncludePacketsInHistory=true;
            var repository=new SolanaRepository(owner,rpc,()=>key.ToArray());
            var source=new SolanaAvatarDetailDto{Id=Guid.NewGuid(),Version=6,XP=997};
            var ciphertext=(string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.NonPublic|BindingFlags.Instance)!
                .MakeGenericMethod(source.GetType()).Invoke(repository,new object[]{source})!;
            var chunkCount=new SolanaMemoFrames(ciphertext).Count;
            proxy.FailSend=failingSend==-1?chunkCount+1:failingSend;
            Exception failure;
            if(proxy.FailSend==1)failure=await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(()=>repository.CreateAsync(source));
            else failure=await Assert.ThrowsExceptionAsync<SolanaPartialMutationException>(()=>repository.CreateAsync(source));
            Assert.AreEqual(proxy.FailSend,proxy.Sends);
            Assert.AreEqual(proxy.FailSend-1,proxy.StatusReads);
            if(failure is SolanaPartialMutationException partial){
                CollectionAssert.AreEqual(proxy.Packets.Keys.Take(proxy.FailSend-1).ToArray(),partial.CommittedSignatures.ToArray());
                failure=partial.InnerException!;
            }
            Assert.IsInstanceOfType<SolanaTransactionReconciliationException>(failure);
            StringAssert.Contains(failure.ToString(),proxy.Signature);
            // Simulate a lost response before native acknowledgement. The last attempted
            // packet is NOT evidence of commitment and must not appear in history here.
            proxy.Packets.Remove(proxy.Signature);
            var records=await repository.GetAllAsync<SolanaAvatarDetailDto>();
            Assert.AreEqual(0,records.Count);
            Assert.AreEqual(6,source.Version);Assert.AreEqual(997,source.XP);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }

    [TestMethod]
    public async Task LargeCommittedRecordIsReconstructedByCollectionAndExactSignatureReaders()
    {
        var owner=new Account();var key=RandomNumberGenerator.GetBytes(32);
        try{
            var rpc=DispatchProxy.Create<IRpcClient,TombstoneProxy>();var proxy=(TombstoneProxy)(object)rpc;
            proxy.Writer=owner.PublicKey.Key;proxy.IncludePacketsInHistory=true;
            var repository=new SolanaRepository(owner,rpc,()=>key.ToArray());
            var source=new SolanaAvatarDetailDto{Id=Guid.NewGuid(),Version=6,XP=997};
            var signature=await repository.CreateAsync(source);
            Assert.IsTrue(proxy.Sends>2);
            var selected=await repository.GetAllAsync<SolanaAvatarDetailDto>();
            Assert.AreEqual(1,selected.Count);Assert.AreEqual(signature,selected[0].Signature);
            Assert.AreEqual(source.Id,selected[0].Record.Id);Assert.AreEqual(source.XP,selected[0].Record.XP);
            var reopened=await repository.GetAsync<SolanaAvatarDetailDto>(signature);
            Assert.AreEqual(source.Id,reopened.Id);Assert.AreEqual(source.XP,reopened.XP);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod]
    [DataRow("avatar")] [DataRow("detail")] [DataRow("holon")]
    public async Task SignedEncryptedTombstonePreservesRecordAndStampsUtcAudit(string kind)
    {
        var owner=new Account();var key=RandomNumberGenerator.GetBytes(32);
        var rpc=DispatchProxy.Create<IRpcClient,TombstoneProxy>();var proxy=(TombstoneProxy)(object)rpc;
        proxy.Writer=owner.PublicKey.Key;
        var repository=new SolanaRepository(owner,rpc,()=> (byte[])key.Clone());
        SolanaBaseDto source=kind switch {"avatar"=>new SolanaAvatarDto {Email="private@example.invalid"},"detail"=>new SolanaAvatarDetailDto {XP=93},_=>new SolanaHolonDto {Name="private holon"}};
        source.Id=Guid.NewGuid();source.Version=6;
        const decimal precise=0.1234567890123456789012345678m;
        const string literalDate="2026-10-20T10:11:12.1234567+02:00";
        source.GetType().GetProperty("MetaData")!.SetValue(source,new Dictionary<string,object>{["precise"]=precise,["literal_date"]=literalDate});
        proxy.Memo=(string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.NonPublic|BindingFlags.Instance)!
            .MakeGenericMethod(source.GetType()).Invoke(repository,new object[] {source})!;
        var before=DateTime.UtcNow;
        var signature=await repository.CreateTombstoneAsync("original");
        var after=DateTime.UtcNow;
        Assert.AreEqual(proxy.Signature,signature);Assert.AreEqual(proxy.Sends,proxy.StatusReads);
        foreach(var packet in proxy.Packets.Values){
            Assert.IsTrue(packet.Length<=SolanaMemoFrames.TransactionBytesLimit);
            Assert.IsTrue(owner.Verify(packet.AsSpan(65).ToArray(),packet.AsSpan(1,64).ToArray()));
        }
        var tx=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!);
        Assert.AreEqual(2,tx.Instructions.Count);
        var envelope=tx.Instructions[1].Data;
        if(kind=="detail"){
            var commit=SolanaMemoFrames.ParseCommit(Encoding.UTF8.GetString(envelope));
            Assert.AreEqual(commit.Count+1,proxy.Sends);
            var rows=proxy.Packets.Where(row=>row.Key!=signature).Select(row=>(row.Key,
                Encoding.UTF8.GetString(Solnet.Rpc.Models.Transaction.Deserialize(row.Value).Instructions[1].Data))).ToList();
            envelope=Encoding.ASCII.GetBytes(SolanaMemoFrames.Join(commit,rows));
        }else Assert.AreEqual(1,proxy.Sends);
        StringAssert.StartsWith(Encoding.UTF8.GetString(envelope),"oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(Encoding.UTF8.GetString(envelope).Contains("private"));
        var payload=(string)typeof(SolanaRepository).GetMethod("DecodeEntity",BindingFlags.NonPublic|BindingFlags.Instance)!
            .Invoke(repository,new object[] {envelope})!;
        var readTask=(Task<string>)typeof(SolanaRepository).GetMethod("ReadStoredRecordAsync",BindingFlags.NonPublic|BindingFlags.Instance)!
            .Invoke(repository,new object[]{signature})!;
        Assert.AreEqual(payload,await readTask);
        using(var exact=System.Text.Json.JsonDocument.Parse(payload)){
            var metadata=exact.RootElement.GetProperty("MetaData");
            Assert.AreEqual(precise,metadata.GetProperty("precise").GetDecimal());
            Assert.AreEqual(literalDate,metadata.GetProperty("literal_date").GetString());
        }
        var reopened=JsonConvert.DeserializeObject(payload,source.GetType()) as SolanaBaseDto;
        Assert.IsNotNull(reopened);Assert.AreEqual(source.Id,reopened.Id);Assert.AreEqual(7,reopened.Version);
        Assert.AreEqual(source.Id,reopened.PreviousVersionId);Assert.IsTrue(reopened.IsDeleted);
        Assert.AreEqual(DateTimeKind.Utc,reopened.DeletedDate.Kind);
        Assert.IsTrue(reopened.DeletedDate>=before && reopened.DeletedDate<=after);
        Assert.AreEqual(source.RecordKind,reopened.RecordKind);
        if(source is SolanaAvatarDto avatar) Assert.AreEqual(avatar.Email,((SolanaAvatarDto)reopened).Email);
        if(source is SolanaAvatarDetailDto detail) Assert.AreEqual(detail.XP,((SolanaAvatarDetailDto)reopened).XP);
        if(source is SolanaHolonDto holon) Assert.AreEqual(holon.Name,((SolanaHolonDto)reopened).Name);
        Assert.AreEqual(6,source.Version);Assert.IsFalse(source.IsDeleted);Assert.AreEqual(DateTime.MinValue,source.DeletedDate);
        CryptographicOperations.ZeroMemory(key);
    }

    [TestMethod]
    public async Task SignedEncryptedUpdateRetainsExactMetadata()
    {
        var owner=new Account();var key=RandomNumberGenerator.GetBytes(32);
        try{
            var rpc=DispatchProxy.Create<IRpcClient,TombstoneProxy>();var proxy=(TombstoneProxy)(object)rpc;proxy.Writer=owner.PublicKey.Key;
            var repository=new SolanaRepository(owner,rpc,()=> (byte[])key.Clone());
            const decimal precise=0.1234567890123456789012345678m;
            const string literalDate="2026-10-20T10:11:12.1234567+02:00";
            var source=new SolanaHolonDto{Id=Guid.NewGuid(),Version=6,MetaData=new(){["precise"]=precise,["literal_date"]=literalDate}};
            var signature=await repository.UpdateAsync(source);Assert.AreEqual(proxy.Signature,signature);
            Assert.AreEqual(1,proxy.Sends);Assert.IsTrue(owner.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
            var transaction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed);
            var payload=(string)typeof(SolanaRepository).GetMethod("DecodeEntity",BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(repository,new object[]{transaction.Instructions[1].Data})!;
            using var exact=System.Text.Json.JsonDocument.Parse(payload);
            Assert.AreEqual(precise,exact.RootElement.GetProperty("MetaData").GetProperty("precise").GetDecimal());
            Assert.AreEqual(literalDate,exact.RootElement.GetProperty("MetaData").GetProperty("literal_date").GetString());
            Assert.AreEqual(7,exact.RootElement.GetProperty("Version").GetInt32());Assert.AreEqual(7,source.Version);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod]
    [DataRow("avatar",false)] [DataRow("avatar",true)]
    [DataRow("detail",false)] [DataRow("detail",true)]
    [DataRow("holon",false)] [DataRow("holon",true)]
    public async Task KnownStaleOrAlreadyDeletedSignaturesCannotSubmitAnotherTombstone(string kind,bool deleted)
    {
        var owner=new Account();var key=RandomNumberGenerator.GetBytes(32);
        var rpc=DispatchProxy.Create<IRpcClient,TombstoneProxy>();var proxy=(TombstoneProxy)(object)rpc;proxy.Writer=owner.PublicKey.Key;
        var repository=new SolanaRepository(owner,rpc,()=> (byte[])key.Clone());
        SolanaBaseDto source=kind switch {"avatar"=>new SolanaAvatarDto(),"detail"=>new SolanaAvatarDetailDto(),_=>new SolanaHolonDto()};
        source.Id=Guid.NewGuid();source.Version=6;
        var encoder=typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(source.GetType());
        proxy.Memo=(string)encoder.Invoke(repository,new object[] {source})!;
        source.Version=7;source.IsDeleted=deleted;
        proxy.NewMemo=(string)encoder.Invoke(repository,new object[] {source})!;
        if(deleted) await Assert.ThrowsExceptionAsync<KeyNotFoundException>(()=>repository.CreateTombstoneAsync("original"));
        else await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>repository.CreateTombstoneAsync("original"));
        Assert.AreEqual(0,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);Assert.IsNull(proxy.Signed);Assert.AreEqual(2,proxy.Pages);
        CryptographicOperations.ZeroMemory(key);
    }

    public class TombstoneProxy:DispatchProxy
    {
        public string Writer="",Memo="",NewMemo="",Signature="";public byte[]? Signed;public int Sends,StatusReads,Pages;
        public Dictionary<string,byte[]> Packets=new(StringComparer.Ordinal);
        public int FailSend;public bool IncludePacketsInHistory;
        public string? MissingReference,UnexpectedWriterReference;
        public Func<string,string,string>? RewriteMemo;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            var resultType=method!.ReturnType.GetGenericArguments()[0];var valueType=resultType.GetGenericArguments()[0];
            string json;
            switch(method.Name)
            {
                case "GetTransactionAsync":
                    var requested=(string)args![0]!;
                    if(requested==MissingReference){json="null";break;}
                    Assert.IsTrue(requested is "original" or "newer" || Packets.ContainsKey(requested));
                    var requestedMemo=Packets.TryGetValue(requested,out var packet)
                        ? Encoding.UTF8.GetString(Solnet.Rpc.Models.Transaction.Deserialize(packet).Instructions[1].Data)
                        : requested=="newer" ? NewMemo:Memo;
                    requestedMemo=RewriteMemo?.Invoke(requested,requestedMemo)??requestedMemo;
                    json=JsonConvert.SerializeObject(new {Meta=new {},Transaction=new {Signatures=new[] {requested},Message=new {
                        AccountKeys=new[] {requested==UnexpectedWriterReference ? "wrong-writer":Writer,"MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr"},
                        Instructions=new[] {new {ProgramIdIndex=1,Data=Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes(requestedMemo))}}}}});break;
                case "GetSignaturesForAddressAsync":
                    Pages++;json=Pages==1 ? JsonConvert.SerializeObject((IncludePacketsInHistory ? Packets.Keys.Reverse().ToArray() : NewMemo.Length>0 ? new[] {"newer","original"}:new[] {"original"})
                        .Select(signature=>new {signature,slot=5,confirmationStatus="finalized"})) : "[]";break;
                case "GetLatestBlockHashAsync":
                    json=JsonConvert.SerializeObject(new {Value=new {Blockhash=Encoders.Base58.EncodeData(new byte[32])}});break;
                case "SendTransactionAsync":
                    Sends++;Signed=(byte[])args![0]!;Signature=Encoders.Base58.EncodeData(Signed.AsSpan(1,64).ToArray());
                    Packets.Add(Signature,Signed.ToArray());
                    if(Sends==FailSend)throw new IOException("Controlled lost submission response; outcome must be reconciled.");
                    json=JsonConvert.SerializeObject(Signature);break;
                case "GetSignatureStatusesAsync":
                    StatusReads++;json=JsonConvert.SerializeObject(new {Value=new[] {new {confirmationStatus="finalized"}}});break;
                default:throw new InvalidOperationException("Unexpected RPC "+method.Name);
            }
            var response=Activator.CreateInstance(resultType,new HttpResponseMessage(System.Net.HttpStatusCode.OK),JsonConvert.DeserializeObject(json,valueType));
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] {response});
        }
    }
}
