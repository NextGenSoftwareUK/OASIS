using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Solnet.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaOriginValidationTests
{
    private static SolanaBaseDto Record(string kind) => kind switch {"avatar"=>new SolanaAvatarDto(),"detail"=>new SolanaAvatarDetailDto(),_=>new SolanaHolonDto()};
    private static void Corrupt(SolanaBaseDto record,bool provider)
    {if(provider) record.CreatedProviderType=(ProviderType)int.MaxValue;else record.CreatedOASISType=(OASISType)int.MaxValue;}
    public static IEnumerable<object[]> RecordCases()
    {foreach(var kind in new[] {"avatar","detail","holon"}) foreach(var provider in new[] {true,false}) yield return new object[] {kind,provider};}
    public static IEnumerable<object[]> WriteCases()
    {foreach(var row in RecordCases()) foreach(var update in new[] {true,false}) yield return row.Concat(new object[] {update}).ToArray();}
    public static IEnumerable<object[]> ApiCases()
    {foreach(var kind in new[] {"avatar","detail","holon","bulk"}) foreach(var synchronous in new[] {true,false}) foreach(var provider in new[] {true,false}) yield return new object[] {kind,synchronous,provider};}

    [DataTestMethod]
    [DynamicData(nameof(RecordCases),DynamicDataSourceType.Method)]
    public void MalformedStoredOriginIsRejected(string kind,bool provider)
    {
        var dto=Record(kind);dto.Id=Guid.NewGuid();Corrupt(dto,provider);
        var read=typeof(SolanaRepository).GetMethod("DeserializeStoredRecord",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(dto.GetType());
        var error=Assert.ThrowsException<TargetInvocationException>(()=>read.Invoke(null,new object[] {OasisJson.Serialize(dto)}));
        Assert.IsInstanceOfType(error.InnerException,typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DynamicData(nameof(WriteCases),DynamicDataSourceType.Method)]
    public async Task MalformedWriteFailsBeforeKeysOrRpc(string kind,bool provider,bool update)
    {
        var dto=Record(kind);dto.Id=Guid.NewGuid();Corrupt(dto,provider);
        var rpc=DispatchProxy.Create<IRpcClient,NoRpc>();var calls=(NoRpc)(object)rpc;var keyCalls=0;
        var repository=new SolanaRepository(null!,rpc,()=>{keyCalls++;throw new InvalidOperationException("Unexpected key access");});
        var method=typeof(SolanaRepository).GetMethod(update ? "UpdateAsync":"CreateAsync")!.MakeGenericMethod(dto.GetType());
        try {await (Task<string>)method.Invoke(repository,new object[] {dto})!;Assert.Fail("Malformed origin accepted");}
        catch(ArgumentException) { }
        Assert.AreEqual(0,keyCalls);Assert.AreEqual(0,calls.Calls);
    }

    [DataTestMethod]
    [DynamicData(nameof(ApiCases),DynamicDataSourceType.Method)]
    public async Task InvalidApiOriginHasTerminalPayloadCodeBeforeReads(string kind,bool synchronous,bool provider)
    {
        var account=new Solnet.Wallet.Account();var client=new SolanaOASIS("http://127.0.0.1:8899",account.PrivateKey.Key,account.PublicKey.Key);
        var rpc=DispatchProxy.Create<IRpcClient,NoRpc>();var calls=(NoRpc)(object)rpc;
        var repository=new SolanaRepository(account,rpc,()=>throw new InvalidOperationException("Unexpected key access"));
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(client,repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(client,true);
        IHolonBase source=kind switch {"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]="must-not-be-read";
        if(provider)source.CreatedProviderType=new EnumValue<ProviderType>((ProviderType)int.MaxValue);else source.CreatedOASISType=new EnumValue<OASISType>((OASISType)int.MaxValue);
        var code=kind switch {
            "avatar"=>synchronous ? client.SaveAvatar((IAvatar)source).ErrorCode:(await client.SaveAvatarAsync((IAvatar)source)).ErrorCode,
            "detail"=>synchronous ? client.SaveAvatarDetail((IAvatarDetail)source).ErrorCode:(await client.SaveAvatarDetailAsync((IAvatarDetail)source)).ErrorCode,
            "bulk"=>synchronous ? client.SaveHolons(new[] {(IHolon)source},saveChildren:false).ErrorCode:(await client.SaveHolonsAsync(new[] {(IHolon)source},saveChildren:false)).ErrorCode,
            _=>synchronous ? client.SaveHolon((IHolon)source,saveChildren:false).ErrorCode:(await client.SaveHolonAsync((IHolon)source,saveChildren:false)).ErrorCode};
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",code);Assert.AreEqual(0,calls.Calls);
        Assert.AreEqual("must-not-be-read",source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
    }

    public class NoRpc:DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {Calls++;throw new InvalidOperationException("Unexpected RPC: "+method?.Name);}
    }
}
