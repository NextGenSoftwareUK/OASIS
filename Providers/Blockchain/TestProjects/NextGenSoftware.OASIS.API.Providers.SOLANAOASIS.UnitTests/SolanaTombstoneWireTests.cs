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
        Assert.AreEqual(proxy.Signature,signature);Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(1,proxy.StatusReads);
        Assert.IsTrue(owner.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
        var tx=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!);
        Assert.AreEqual(2,tx.Instructions.Count);
        var envelope=tx.Instructions[1].Data;
        StringAssert.StartsWith(Encoding.UTF8.GetString(envelope),"oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(Encoding.UTF8.GetString(envelope).Contains("private"));
        var payload=(string)typeof(SolanaRepository).GetMethod("DecodeEntity",BindingFlags.NonPublic|BindingFlags.Instance)!
            .Invoke(repository,new object[] {envelope})!;
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
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            var resultType=method!.ReturnType.GetGenericArguments()[0];var valueType=resultType.GetGenericArguments()[0];
            string json;
            switch(method.Name)
            {
                case "GetTransactionAsync":
                    var requested=(string)args![0]!;
                    Assert.IsTrue(requested is "original" or "newer");
                    json=JsonConvert.SerializeObject(new {Meta=new {},Transaction=new {Signatures=new[] {requested},Message=new {
                        AccountKeys=new[] {Writer,"MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr"},
                        Instructions=new[] {new {ProgramIdIndex=1,Data=Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes(requested=="newer" ? NewMemo:Memo))}}}}});break;
                case "GetSignaturesForAddressAsync":
                    Pages++;json=Pages==1 ? JsonConvert.SerializeObject((NewMemo.Length>0 ? new[] {"newer","original"}:new[] {"original"})
                        .Select(signature=>new {signature,slot=5,confirmationStatus="finalized"})) : "[]";break;
                case "GetLatestBlockHashAsync":
                    json=JsonConvert.SerializeObject(new {Value=new {Blockhash=Encoders.Base58.EncodeData(new byte[32])}});break;
                case "SendTransactionAsync":
                    Sends++;Signed=(byte[])args![0]!;Signature=Encoders.Base58.EncodeData(Signed.AsSpan(1,64).ToArray());
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
