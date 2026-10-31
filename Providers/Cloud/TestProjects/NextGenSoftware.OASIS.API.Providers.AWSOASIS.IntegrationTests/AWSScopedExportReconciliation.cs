using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public sealed partial class AWSNativeRunLifecycle
{
    [TestMethod]
    public async Task ReconcileOnlyOriginalScopedExportExpectation()
    {
        static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        var report=Path.Combine(Work(),"aws-full-native-current.trx");
        var intent=Path.Combine(Work(),"aws-full-suite.pending.json");
        Assert.AreEqual("F1841D20C2D9D3BA1BD96E5F3E17E2D4E5D42D6714D52CCA617516F5825F1706",Hash(await File.ReadAllBytesAsync(report)));
        Assert.AreEqual("ABE880287FDA9DD1475BD9F55FC6DC545FFE53727ECFBFFABDF8EAF4149F25FB",Hash(await File.ReadAllBytesAsync(intent)));
        var xml=XDocument.Load(report);XNamespace ns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var counters=xml.Descendants(ns+"Counters").Single();Assert.AreEqual("138",(string?)counters.Attribute("total"));Assert.AreEqual("137",(string?)counters.Attribute("passed"));Assert.AreEqual("1",(string?)counters.Attribute("failed"));
        var failure=xml.Descendants(ns+"UnitTestResult").Single(row=>(string?)row.Attribute("outcome")!="Passed");
        Assert.AreEqual("HolonRoundTripAndSearch_UseOfficialDynamoDbApi",(string?)failure.Attribute("testName"));
        StringAssert.Contains(failure.Descendants(ns+"StackTrace").Single().Value,"AWSOASISIntegrationTests.cs:line 318");
        StringAssert.Contains(failure.Descendants(ns+"Message").Single().Value,"Sequence contains no elements");
        using var native=Client();
        var originals=new Dictionary<string,Dictionary<string,string>>();
        foreach(var table in Tables)originals.Add(table,(await Rows(native,table)).ToDictionary(row=>row["id"].S,row=>row["data_json"].S));
        var holons=originals["OasisHolons"].Values.Select(OasisJson.DeserializeStored<Holon>).ToArray();
        var root=holons.Single(row=>row.Name=="owned-page-0"&&row.MetaData?.ContainsKey("owned-marker")==true);
        var child=holons.Single(row=>row.ParentHolonId==root.Id&&row.Name=="owned-grandchild");
        var leaf=holons.Single(row=>row.ParentHolonId==child.Id&&row.Name=="owned-great-grandchild");
        Assert.AreNotEqual(Guid.Empty,root.CreatedByAvatarId);Assert.AreEqual(Guid.Empty,child.CreatedByAvatarId);Assert.AreEqual(Guid.Empty,leaf.CreatedByAvatarId);
        var provider=new AWSOASIS("us-east-1","OwnedFullSuite","owned-synthetic-secret","http://127.0.0.1:18000");
        try{
            // Read the SAME originals. No activation/schema repair, save replay or new record.
            var complete=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.IsFalse(complete.IsError,complete.Exception?.ToString());
            Assert.AreEqual(child.Id,complete.Result.Children.Single().Id);Assert.AreEqual(leaf.Id,complete.Result.Children.Single().Children.Single().Id);
            foreach(var synchronous in new[]{false,true}){
                var scoped=synchronous?provider.ExportAllDataForAvatarById(root.CreatedByAvatarId):await provider.ExportAllDataForAvatarByIdAsync(root.CreatedByAvatarId);
                Assert.IsFalse(scoped.IsError,scoped.Exception?.ToString());var selected=(Holon)scoped.Result.Single();Assert.AreEqual(root.Id,selected.Id);Assert.AreEqual(0,selected.Children.Count);
                selected.AcceptChanges();var rejected=await provider.SaveHolonAsync(selected);Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);
            }
            foreach(var table in Tables){var after=(await Rows(native,table)).ToDictionary(row=>row["id"].S,row=>row["data_json"].S);Assert.AreEqual(originals[table].Count,after.Count);foreach(var row in originals[table])Assert.AreEqual(row.Value,after[row.Key],"Original native payload changed during reconciliation.");}
            Console.WriteLine($"Original scoped export reconciled: root={root.Id}, hidden child={child.Id}, hidden leaf={leaf.Id}; unchanged native JSON and no replay.");
        }finally{await provider.DeActivateProviderAsync();}
        await CleanupObservedAsync("aws-scoped-export-cleanup.json");
        Assert.AreEqual("F1841D20C2D9D3BA1BD96E5F3E17E2D4E5D42D6714D52CCA617516F5825F1706",Hash(await File.ReadAllBytesAsync(report)));
    }
}
