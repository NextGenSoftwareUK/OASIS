using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false,"rollback")] [DataRow(true,false,"rollback")]
    [DataRow(false,true,"rollback")] [DataRow(true,true,"rollback")]
    [DataRow(false,false,"rollbackLost")] [DataRow(true,false,"rollbackLost")]
    [DataRow(false,true,"rollbackLost")] [DataRow(true,true,"rollbackLost")]
    [DataRow(false,false,"commitLost")] [DataRow(true,false,"commitLost")]
    [DataRow(false,true,"commitLost")] [DataRow(true,true,"commitLost")]
    [DataRow(false,false,"cleanup")] [DataRow(true,false,"cleanup")]
    [DataRow(false,true,"cleanup")] [DataRow(true,true,"cleanup")]
    public async Task NativeCosmicOrdinaryWriteOwnsCommitRollbackAndCleanup(bool synchronous,bool batch,string mode)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var field=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var healthy=(IDriver)field.GetValue(provider)!;
        var faults=new Neo4jOrdinaryWriteFaultProxy.Faults(mode);
        var prefix="owned ordinary outcome "+Guid.NewGuid().ToString("N");
        var child=new Holon{Name=prefix+" child"};
        var root=new Holon{Name=prefix+" root",Children=new List<IHolon>{child}};
        var other=new Holon{Name=prefix+" other"};
        var selected=batch ? new[]{root,child,other}:new[]{root,child};
        var names=selected.Select(item=>item.Name).ToArray();var ids=Array.Empty<string>();var complete=false;
        try
        {
            field.SetValue(provider,Neo4jOrdinaryWriteFaultProxy.Wrap(healthy,faults));
            bool error,saved,warning;int count;Exception? cause;IHolon[] acknowledgements;
            if(batch)
            {
                var result=synchronous ? provider.SaveHolons(new IHolon[]{root,other}):await provider.SaveHolonsAsync(new IHolon[]{root,other});
                error=result.IsError;saved=result.IsSaved;warning=result.IsWarning;count=result.SavedCount;cause=result.Exception;
                acknowledgements=result.Result?.ToArray() ?? Array.Empty<IHolon>();
            }
            else
            {
                var result=synchronous ? provider.SaveHolon(root):await provider.SaveHolonAsync(root);
                error=result.IsError;saved=result.IsSaved;warning=result.IsWarning;count=result.SavedCount;cause=result.Exception;
                acknowledgements=result.Result==null ? Array.Empty<IHolon>():new[]{result.Result};
            }
            field.SetValue(provider,healthy);
            using var inspection=healthy.AsyncSession();
            var records=await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Name IN $names RETURN h",new{names})).ToListAsync();
            ids=records.Select(row=>row["h"].As<Neo4j.Driver.INode>()["Id"].As<string>()).ToArray();
            Assert.AreEqual(mode is "cleanup" or "commitLost" ? selected.Length:0,records.Count);
            foreach(var record in records)
                Assert.IsTrue(names.Contains(record["h"].As<Neo4j.Driver.INode>()["Name"].As<string>()));
            Assert.AreEqual(1,faults.TransactionDisposals);Assert.AreEqual(1,faults.SessionDisposals);
            Assert.IsNotNull(cause);
            var flattened=cause is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray()
                :cause.InnerException is AggregateException nested ? nested.Flatten().InnerExceptions.ToArray()
                :new[]{cause.InnerException ?? cause};
            if(mode=="cleanup")
            {
                Assert.IsFalse(error,cause.ToString());Assert.IsTrue(saved);Assert.IsTrue(warning);
                Assert.AreEqual(selected.Length,count);Assert.AreEqual(batch ? 2:1,acknowledgements.Length);
                Assert.IsTrue(flattened.Contains(faults.TransactionCleanup));Assert.IsTrue(flattened.Contains(faults.SessionCleanup));
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(cause));
                Assert.AreEqual(0,faults.Rollbacks);Assert.AreEqual(1,faults.Commits);
                foreach(var item in selected)
                {Assert.AreNotEqual(Guid.Empty,item.Id);Assert.IsFalse(item.IsNewHolon);Assert.AreEqual(item.Id.ToString(),item.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS]);}
                Assert.AreEqual(root.Id,child.ParentHolonId);
                Assert.AreEqual(child.Id,acknowledgements.Single(item=>item.Id==root.Id).Children.Single().Id);
            }
            else
            {
                Assert.IsTrue(error);Assert.IsFalse(saved);Assert.AreEqual(0,count);Assert.AreEqual(0,acknowledgements.Length);
                Assert.AreEqual(mode!="rollback",NativeMutationReconciliation.IsRequired(cause));
                Assert.AreEqual(mode=="commitLost" ? 1:0,faults.Commits);Assert.AreEqual(mode=="commitLost" ? 0:1,faults.Rollbacks);
                foreach(var item in selected)
                {Assert.AreEqual(Guid.Empty,item.Id);Assert.IsTrue(item.IsNewHolon);Assert.IsFalse(item.ProviderUniqueStorageKey.ContainsKey(ProviderType.Neo4jOASIS));}
                Assert.AreEqual(Guid.Empty,child.ParentHolonId);
                if(mode=="commitLost")Assert.AreSame(faults.Commit,cause.InnerException);
                else
                {
                    Assert.IsTrue(flattened.Contains(faults.Query));Assert.IsTrue(flattened.Contains(faults.TransactionCleanup));
                    Assert.IsTrue(flattened.Contains(faults.SessionCleanup));
                    if(mode=="rollbackLost")Assert.IsTrue(flattened.Contains(faults.Rollback));
                }
            }
            complete=true;
        }
        finally
        {
            field.SetValue(provider,healthy);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{names,ids,mode,complete}));
            if(complete && ids.Length!=0)
            {
                using var cleanup=healthy.AsyncSession();
                await(await cleanup.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
