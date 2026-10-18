using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task NativeImportPreservesAcknowledgementAndReconciliationEvidence(bool synchronous,bool loseCommit)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var field=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var healthy=(IDriver)field.GetValue(provider)!;
        var id=Guid.NewGuid();var commits=0;var cause=new IOException("Owned import acknowledgement loss after native commit.");
        try
        {
            if(loseCommit)field.SetValue(provider,Neo4jCommitFaultProxy.Wrap(healthy,cause,()=>commits++));
            var source=new Holon{Id=id,Name="owned-import-original",IsActive=true};
            var result=synchronous?provider.Import(new[]{source}):await provider.ImportAsync(new[]{source});
            field.SetValue(provider,healthy);
            using var check=healthy.AsyncSession();
            var records=await(await check.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.Name AS name",new{id=id.ToString()})).ToListAsync();
            Assert.AreEqual(1,records.Count);Assert.AreEqual(source.Name,records.Single()["name"].As<string>());
            // Remove only the exact independently inspected, actually committed original.
            await(await check.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h",new{id=id.ToString()})).ConsumeAsync();
            if(loseCommit)
            {
                Assert.AreEqual(1,commits);Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception),"Import must not erase an uncertain native commit.");
                Assert.IsTrue(result.Exception!.ToString().Contains(cause.Message,StringComparison.Ordinal));
            }
            else {Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.Result);Assert.IsTrue(result.IsSaved);}
        }
        finally{field.SetValue(provider,healthy);await provider.DeActivateProviderAsync();}
    }
}
