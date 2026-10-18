using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaBlockhashTests
{
    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(false,true)]
    [DataRow(true,false)]
    [DataRow(true,true)]
    public async Task InvalidIdentityOrVersionFailsBeforeKeyResolutionAndRpc(bool update,bool negativeVersion)
    {
        var keyCalls=0;
        var repository=new SolanaRepository(null!,null!,()=> {keyCalls++;throw new InvalidOperationException("Unexpected key access");});
        var dto=new SolanaAvatarDto {Id=negativeVersion ? Guid.NewGuid():Guid.Empty,Version=negativeVersion ? -1:0};
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>update ? repository.UpdateAsync(dto):repository.CreateAsync(dto));
        Assert.AreEqual(0,keyCalls); Assert.AreEqual(negativeVersion ? -1:0,dto.Version);
    }

    [DataTestMethod]
    [DataRow(false,"missing")]
    [DataRow(true,"missing")]
    [DataRow(false,"http")]
    [DataRow(true,"http")]
    [DataRow(false,"malformed")]
    [DataRow(true,"malformed")]
    public async Task BlockhashFailureNeverSubmitsOrChangesCallerVersion(bool update,string scenario)
    {
        var rpc=DispatchProxy.Create<IRpcClient,BlockhashProxy>();
        var proxy=(BlockhashProxy)(object)rpc; proxy.Scenario=scenario;
        var key=RandomNumberGenerator.GetBytes(32);
        var repository=new SolanaRepository(null!,rpc,()=> (byte[])key.Clone());
        var dto=new SolanaAvatarDto {Id=Guid.NewGuid(),Version=7,PreviousVersionId=Guid.NewGuid(),Email="owned@example.invalid"};
        var previous=dto.PreviousVersionId;
        var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>update ? repository.UpdateAsync(dto):repository.CreateAsync(dto));
        Assert.IsFalse(NativeMutationReconciliation.IsRequired(error));
        Assert.AreEqual(7,dto.Version); Assert.AreEqual(previous,dto.PreviousVersionId);
        Assert.AreEqual(1,proxy.Calls);
    }

    public class BlockhashProxy : DispatchProxy
    {
        public string Scenario="";
        public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Calls++;
            if(method!.Name != "GetLatestBlockHashAsync") throw new InvalidOperationException("Unexpected RPC before valid blockhash: "+method.Name);
            var resultType=method.ReturnType.GetGenericArguments()[0];
            var valueType=resultType.GetGenericArguments()[0];
            var value=Scenario=="missing" ? null : JsonConvert.DeserializeObject("{\"value\":{\"blockhash\":\"bad\",\"lastValidBlockHeight\":100}}",valueType);
            var response=Activator.CreateInstance(resultType,new System.Net.Http.HttpResponseMessage(Scenario=="http" ? System.Net.HttpStatusCode.BadGateway:System.Net.HttpStatusCode.OK),value);
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] {response});
        }
    }
}
