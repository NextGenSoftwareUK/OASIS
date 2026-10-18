using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaServiceIdentityTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var detail in new[] {false,true})
        foreach(var selector in new[] {"id","username","email"})
        foreach(var scenario in new[] {"valid","invalid","missing","ambiguous","corrupt"})
            yield return new object[] {detail,selector,scenario};
    }

    [DataTestMethod]
    [DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ServiceIdentityReadsUseAuthenticatedTypedHistoryWithoutBroadcast(bool detail,string selector,string scenario)
    {
        var owner=new Account();var id=Guid.NewGuid();var key=RandomNumberGenerator.GetBytes(32);
        var rpc=DispatchProxy.Create<IRpcClient,SolanaCollectionTests.HistoryProxy>();
        var proxy=(SolanaCollectionTests.HistoryProxy)(object)rpc;proxy.Writer=owner.PublicKey.Key;
        var repository=new SolanaRepository(owner,rpc,()=> (byte[])key.Clone());
        SolanaBaseDto Record(Guid identity,int version) => detail
            ? new SolanaAvatarDetailDto {Id=identity,Version=version,Username="stored user",Email="stored@example.invalid",XP=91}
            : new SolanaAvatarDto {Id=identity,AvatarId=identity,Version=version,UserName="stored user",Email="stored@example.invalid"};
        string Encode(SolanaBaseDto record) => Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes((string)typeof(SolanaRepository)
            .GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(record.GetType()).Invoke(repository,new object[] {record})!));
        proxy.Memos["new"]=Encode(Record(id,2));
        proxy.Memos["old"]=Encode(Record(scenario=="ambiguous" ? Guid.NewGuid():id,1));
        proxy.Memos["detail"]=Encode(detail ? new SolanaAvatarDto {Id=id,AvatarId=id,Version=9,UserName="stored user",Email="stored@example.invalid"}
            : new SolanaAvatarDetailDto {Id=id,Version=9,Username="stored user",Email="stored@example.invalid"});
        if(scenario=="corrupt") proxy.Memos["new"]=Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes("oasis-aes256gcm-gzip-v2:invalid"));
        var service=new SolanaService(owner,rpc,repository);
        var queryId=scenario=="invalid" ? Guid.Empty:scenario=="missing" ? Guid.NewGuid():id;
        var query=scenario=="invalid" ? " ":scenario=="missing" ? "missing":selector=="email" ? "STORED@example.invalid":"STORED USER";
        void Check<T>(OASISResult<T> result) where T:SolanaBaseDto
        {
            var ambiguous=scenario=="ambiguous" && selector!="id";
            if(scenario is "invalid" or "missing" or "corrupt" || ambiguous)
            {
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                if(scenario=="missing") Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
                if(ambiguous) Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
                if(scenario=="corrupt") Assert.IsNotNull(result.Exception);
            }
            else
            {
                Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
                Assert.AreEqual(id,result.Result.Id);Assert.AreEqual(2,result.Result.Version);
                Assert.AreEqual("new",result.Result.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS]);
                if(result.Result is SolanaAvatarDto avatar) Assert.AreEqual("stored@example.invalid",avatar.Email);
                if(result.Result is SolanaAvatarDetailDto avatarDetail) Assert.AreEqual(91L,avatarDetail.XP);
            }
        }
        if(detail) Check(selector switch {"id"=>await service.GetAvatarDetailByIdAsync(queryId),"email"=>await service.GetAvatarDetailByEmailAsync(query),_=>await service.GetAvatarDetailByUsernameAsync(query)});
        else Check(selector switch {"id"=>await service.GetAvatarByIdAsync(queryId),"email"=>await service.GetAvatarByEmailAsync(query),_=>await service.GetAvatarByUsernameAsync(query)});
        Assert.AreEqual(0,proxy.Writes);Assert.AreEqual(scenario=="invalid" ? 0:scenario=="corrupt" ? 1:2,proxy.Pages);
        CryptographicOperations.ZeroMemory(key);
    }
}
