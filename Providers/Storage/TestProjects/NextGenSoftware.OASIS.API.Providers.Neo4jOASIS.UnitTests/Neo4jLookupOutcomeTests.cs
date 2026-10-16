using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jLookupOutcomeTests
{
    [DataTestMethod]
    [DataRow(false,-1)] [DataRow(true,-1)] [DataRow(false,1)] [DataRow(true,1)]
    public async Task AvatarReadVersionsAreNotSilentlyIgnored(bool synchronous,int version)
    {
        var provider=new Provider("bolt://127.0.0.1:17687","neo4j","owned",()=>ReadProbe.Make<IDriver>(0));
        provider.IsProviderActivated=true;var id=Guid.NewGuid();
        void Check<T>(OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception,version<0 ? typeof(ArgumentOutOfRangeException):typeof(NotSupportedException));
        }
        Check(synchronous ? provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version));
        Check(synchronous ? provider.LoadAvatarByProviderKey(id.ToString(),version):await provider.LoadAvatarByProviderKeyAsync(id.ToString(),version));
        Check(synchronous ? provider.LoadAvatarByUsername("owned",version):await provider.LoadAvatarByUsernameAsync("owned",version));
        Check(synchronous ? provider.LoadAvatarByEmail("owned@example.invalid",version):await provider.LoadAvatarByEmailAsync("owned@example.invalid",version));
        Check(synchronous ? provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version));
        Check(synchronous ? provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version));
        Check(synchronous ? provider.LoadAvatarDetailByUsername("owned",version):await provider.LoadAvatarDetailByUsernameAsync("owned",version));
        Check(synchronous ? provider.LoadAvatarDetailByEmail("owned@example.invalid",version):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid",version));
        Check(synchronous ? provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version));
    }

    public class ReadProbe : DispatchProxy
    {
        public int Remaining;
        public static T Make<T>(int rows) where T:class {var value=Create<T,ReadProbe>();((ReadProbe)(object)value).Remaining=rows;return value;}
        protected override object? Invoke(MethodInfo? method,object?[]? args) => method!.Name switch {
            "AsyncSession"=>Make<IAsyncSession>(Remaining),
            "RunAsync"=>Task.FromResult(Make<IResultCursor>(Remaining)),
            "FetchAsync"=>Task.FromResult(Remaining-->0),
            "get_Current"=>Make<IRecord>(0),
            "Dispose"=>null,
            _=>throw new InvalidOperationException("Unexpected read probe call: "+method.Name)
        };
    }

    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,2)] [DataRow(true,2)]
    public async Task LookupCountDistinguishesAbsenceFromAmbiguity(bool synchronous,int rows)
    {
        var provider=new Provider("bolt://127.0.0.1:17687","neo4j","owned",()=>ReadProbe.Make<IDriver>(rows));
        provider.IsProviderActivated=true;var id=Guid.NewGuid();
        void Check<T>(OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
            Assert.AreEqual(rows==0 ? OASISResultErrorCodes.RecordNotFound:OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
        }
        Check(synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id));
        Check(synchronous ? provider.LoadAvatarByProviderKey(id.ToString()):await provider.LoadAvatarByProviderKeyAsync(id.ToString()));
        Check(synchronous ? provider.LoadAvatarByUsername("owned"):await provider.LoadAvatarByUsernameAsync("owned"));
        Check(synchronous ? provider.LoadAvatarByEmail("owned@example.invalid"):await provider.LoadAvatarByEmailAsync("owned@example.invalid"));
        Check(synchronous ? provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id));
        Check(synchronous ? provider.LoadAvatarDetailByUsername("owned"):await provider.LoadAvatarDetailByUsernameAsync("owned"));
        Check(synchronous ? provider.LoadAvatarDetailByEmail("owned@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid"));
    }
}
