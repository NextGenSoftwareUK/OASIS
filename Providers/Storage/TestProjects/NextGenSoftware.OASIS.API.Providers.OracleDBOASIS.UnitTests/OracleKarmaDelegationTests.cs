using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleKarmaDelegationTests
{
    public class KarmaBoundary:DispatchProxy
    {
        public string? Method;
        public object?[]? Arguments;
        public OASISResult<KarmaAkashicRecord> Response=new();
        protected override object? Invoke(MethodInfo? targetMethod,object?[]? args)
        {
            Method=targetMethod!.Name;Arguments=args;
            return Method.EndsWith("Async",StringComparison.Ordinal)?Task.FromResult(Response):Response;
        }
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task KarmaInterfaceForwardsToExistingDomainBoundaryWithoutProviderIo(bool remove,bool synchronous,bool failure)
    {
        IOASISStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var detail=DispatchProxy.Create<IAvatarDetail,KarmaBoundary>();var boundary=(KarmaBoundary)(object)detail;
        var cause=new IOException("owned domain failure");
        boundary.Response=new(){IsError=failure,IsWarning=true,Message="owned domain result",Exception=failure?cause:null,ErrorCode=failure?"OWNED_DOMAIN_ERROR":null};
        var source=Enum.GetValues<KarmaSourceType>().First();
        var positive=Enum.GetValues<KarmaTypePositive>().First();var negative=Enum.GetValues<KarmaTypeNegative>().First();
        var result=remove
            ?synchronous?provider.RemoveKarmaFromAvatar(detail,negative,source,"owned title","owned description","https://example.invalid")
                :await provider.RemoveKarmaFromAvatarAsync(detail,negative,source,"owned title","owned description","https://example.invalid")
            :synchronous?provider.AddKarmaToAvatar(detail,positive,source,"owned title","owned description","https://example.invalid")
                :await provider.AddKarmaToAvatarAsync(detail,positive,source,"owned title","owned description","https://example.invalid");
        Assert.AreSame(boundary.Response,result);Assert.AreEqual(failure,result.IsError);Assert.IsTrue(result.IsWarning);
        Assert.AreEqual((remove?"KarmaLost":"KarmaEarnt")+(synchronous?"":"Async"),boundary.Method);
        Assert.IsNotNull(boundary.Arguments);Assert.AreEqual(7,boundary.Arguments.Length);
        Assert.AreEqual(remove?(object)negative:positive,boundary.Arguments[0]);Assert.AreEqual(source,boundary.Arguments[1]);
        Assert.AreEqual("owned title",boundary.Arguments[2]);Assert.AreEqual("owned description",boundary.Arguments[3]);Assert.AreEqual("https://example.invalid",boundary.Arguments[4]);
        Assert.AreEqual(true,boundary.Arguments[5]);Assert.AreEqual(0,boundary.Arguments[6]);
    }
}
