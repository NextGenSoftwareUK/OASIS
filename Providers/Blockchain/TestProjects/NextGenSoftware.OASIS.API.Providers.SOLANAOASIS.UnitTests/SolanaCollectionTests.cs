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
public class SolanaCollectionTests
{
    [DataTestMethod]
    [DataRow("valid")]
    [DataRow("deleted")]
    [DataRow("conflict")]
    [DataRow("duplicate")]
    [DataRow("missing-transaction")]
    [DataRow("corrupt")]
    public async Task FinalizedHistoryReturnsOnlyAuthenticLatestTypedRecords(string scenario)
    {
        var account=new Account();
        var rpc=DispatchProxy.Create<IRpcClient,HistoryProxy>();
        var proxy=(HistoryProxy)(object)rpc;
        proxy.Scenario=scenario; proxy.Writer=account.PublicKey.Key;
        var key=RandomNumberGenerator.GetBytes(32);
        var repository=new SolanaRepository(account,rpc,()=> (byte[])key.Clone());
        var id=Guid.NewGuid();
        string Memo<T>(T dto) => Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes((string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T)).Invoke(repository,new object[] {dto!})!));
        proxy.Memos["new"]=Memo(new SolanaAvatarDto {Id=id,Version=2,UserName="actual stored avatar",IsDeleted=scenario=="deleted"});
        proxy.Memos["old"]=Memo(new SolanaAvatarDto {Id=id,Version=scenario=="conflict" ? 2:1,UserName="old"});
        proxy.Memos["detail"]=Memo(new SolanaAvatarDetailDto {Id=id,Version=9,Username="not an avatar",XP=57});
        if(scenario=="corrupt") proxy.Memos["new"]=Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes("oasis-aes256gcm-gzip-v2:corrupt"));
        if(scenario is "conflict" or "duplicate" or "missing-transaction" or "corrupt")
        {
            if(scenario=="corrupt") await Assert.ThrowsExceptionAsync<FormatException>(()=>repository.GetAllAsync<SolanaAvatarDto>());
            else await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>repository.GetAllAsync<SolanaAvatarDto>());
        }
        else
        {
            var result=await repository.GetAllAsync<SolanaAvatarDto>();
            Assert.AreEqual(scenario=="deleted" ? 0:1,result.Count);
            if(result.Count==1) {Assert.AreEqual(id,result[0].Record.Id); Assert.AreEqual(2,result[0].Record.Version); Assert.AreEqual("actual stored avatar",result[0].Record.UserName); Assert.AreEqual("new",result[0].Signature);}
            Assert.AreEqual(2,proxy.Pages); Assert.AreEqual("detail",proxy.LastBefore);
        }
        Assert.AreEqual(0,proxy.Writes);
    }

    public class HistoryProxy:DispatchProxy
    {
        public string Scenario="",Writer="",LastBefore="";
        public int Pages,Writes;
        public Dictionary<string,string> Memos=new();
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            var resultType=method!.ReturnType.GetGenericArguments()[0];
            var valueType=resultType.GetGenericArguments()[0];
            string json;
            if(method.Name=="GetSignaturesForAddressAsync")
            {
                Pages++; LastBefore=args![2]?.ToString() ?? "";
                json=Pages==1 ? JsonConvert.SerializeObject((Scenario=="duplicate" ? new[] {"new","new"}:new[] {"new","old","detail"}).Select(signature=>new {signature,slot=5,confirmationStatus="finalized"})) : "[]";
            }
            else if(method.Name=="GetTransactionAsync")
            {
                var signature=(string)args![0]!;
                json=Scenario=="missing-transaction" ? "null" : JsonConvert.SerializeObject(new {Meta=new {},Transaction=new {Signatures=new[] {signature},Message=new {
                    AccountKeys=new[] {Writer,"MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr"},
                    Instructions=new[] {new {ProgramIdIndex=1,Data=Memos[signature]}}}}});
            }
            else {Writes++; throw new InvalidOperationException("Unexpected RPC "+method.Name);}
            var value=JsonConvert.DeserializeObject(json,valueType);
            var response=Activator.CreateInstance(resultType,new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),value);
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] {response});
        }
    }
}
