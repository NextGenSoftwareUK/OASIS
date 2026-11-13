using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

[TestClass]
public class ArweaveCqlExecutionTests
{
    private sealed class Service : IArweaveService
    {
        public string Mode = "valid";
        public Dictionary<string, byte[]> Rows = new();
        public int Reads;
        public Task ProbeAsync() => Task.CompletedTask;
        public Task<string> PostTransactionAsync(byte[] data,string type,Dictionary<string,string>? tags=null)
            => throw new AssertFailedException("CQL must never write");
        public Task<List<string>> QueryByTagsAsync(Dictionary<string,string> tags)
            => throw new AssertFailedException("CQL must never discover unpinned public records");
        public Task<List<string>> QueryOwnedByTagsAsync(Dictionary<string,string> tags)
        {
            if (tags["OASIS-Type"] == "Holon-Tombstone")
                return Task.FromResult(Mode == "guidTombstone" && tags.ContainsKey("OASIS-Id") ||
                    Mode == "txTombstone" && tags.ContainsKey("OASIS-Original-TxId") ? new List<string>{"tombstone"}:new());
            var key=tags["OASIS-Id"];
            return Task.FromResult(Mode == "ambiguous" ? new List<string>{key,key+"other"} :
                Mode == "missing" ? new List<string>() : new List<string>{key});
        }
        public Task<byte[]> GetTransactionDataAsync(string id)
        {
            Reads++;
            if(Mode == "failure" || Mode == "lateFailure" && Reads == 2) throw new IOException("gateway unavailable");
            if(Mode == "malformed") return Task.FromResult(new byte[]{255});
            return Task.FromResult(Rows[id]);
        }
    }

    private static (ArweaveOASIS Provider,Service Service,CosmicQuery<Holon> Query) Setup(int count=1)
    {
        var ids=Enumerable.Range(0,count).Select(_=>Guid.NewGuid()).OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray();
        var service=new Service();
        foreach(var id in ids) service.Rows[id.ToString("D")]=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Holon{Id=id,Name="coffee"}));
        var provider=new ArweaveOASIS("{}","http://127.0.0.1:1","absent-bridge.mjs"){IsProviderActivated=true};
        typeof(ArweaveOASIS).GetField("_arweaveService",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,service);
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArweaveOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.ArweaveOASIS,ids).Result).Result;
        return(provider,service,query);
    }
    private static CosmicQueryPlan<Holon> Plan(ArweaveOASIS provider,CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query,ProviderType.ArweaveOASIS,new[]{new CosmicQueryTarget(ProviderType.ArweaveOASIS,provider.QueryCapabilities)}).Result;

    [TestMethod]
    public async Task NativeSelectionAndPagingReturnAuthorizedDetachedRows()
    {
        var (provider,service,query)=Setup(3);
        var result=await provider.ExecuteCosmicQueryAsync(Plan(provider,query.Skip(1).Take(1).Result));
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(1,result.Result.Count);Assert.AreEqual(3,service.Reads);
        Assert.AreEqual(0,result.Result[0].Children.Count());
    }
    [DataTestMethod]
    [DataRow("missing")][DataRow("guidTombstone")][DataRow("txTombstone")]
    public async Task MissingOrEitherNativeTombstoneReturnsNoRow(string mode)
    {
        var(p,s,q)=Setup();s.Mode=mode;var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);Assert.AreEqual(0,s.Reads);
    }
    [DataTestMethod]
    [DataRow("failure")][DataRow("malformed")][DataRow("lateFailure")][DataRow("ambiguous")]
    public async Task FailedOrAmbiguousSourcePublishesNoPartialRows(string mode)
    {
        var(p,s,q)=Setup(2);s.Mode=mode;var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual(mode=="ambiguous"?"CQL_REVISION_AMBIGUOUS":"CQL_QUERY_FAILED",result.ErrorCode);
    }
    [TestMethod]
    public async Task WrongNativeIdentityFailsClosed()
    {
        var(p,s,q)=Setup();s.Rows[s.Rows.Keys.Single()]=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Holon{Id=Guid.NewGuid()}));
        var result=await p.ExecuteCosmicQueryAsync(Plan(p,q));Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task CompletePageOverBudgetFailsWithoutRows()
    {
        var(p,s,q)=Setup(2);var result=await p.ExecuteCosmicQueryAsync(Plan(p,q),maximumResults:1);
        Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);Assert.IsNull(result.Result);
    }
}
