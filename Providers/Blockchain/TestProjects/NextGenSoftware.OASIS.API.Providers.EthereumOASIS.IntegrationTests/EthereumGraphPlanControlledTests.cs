using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class EthereumGraphPlanControlledTests
{
    private sealed class AcknowledgedGraphProvider(Action edit):EthereumOASIS("http://127.0.0.1:1",new string('1',64),31337,"0x0000000000000000000000000000000000000001")
    {
        public int Writes;
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync(){IsProviderActivated=true;return Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<bool>{Result=true});}
        protected override async Task<(Nethereum.RPC.Eth.DTOs.TransactionReceipt Receipt,Holon Record)> UpsertPlannedStorageHistoryAsync(PlannedHolonWrite write,bool history)
        {
            Writes++;await Task.Yield();if(Writes==1)edit();
            var record=OasisJson.DeserializeStored<Holon>(OasisJson.Serialize(write.Snapshot));record.Version=1;record.VersionId=Guid.NewGuid();
            return(new(){TransactionHash="0x"+new string('1',64),Status=new Nethereum.Hex.HexTypes.HexBigInteger(1)},record);
        }
    }

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public Task PublicGraphSaveGuardsAcknowledgedAndLaterCallers(bool synchronous,int edit)=>WithKey(async()=>{
        var root=new Holon{Id=Guid.NewGuid(),Name="root"};var child=new Holon{Id=Guid.NewGuid(),Name="child"};root.Children.Add(child);
        var provider=new AcknowledgedGraphProvider(()=>{if(edit==1)root.Description="newer root";if(edit==2)child.Description="newer child";});
        var result=synchronous?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
        Assert.AreEqual(edit!=0,result.IsError);Assert.AreEqual(edit==0,result.IsSaved);
        if(edit==0){Assert.AreEqual(2,provider.Writes);Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);return;}
        Assert.AreEqual(1,provider.Writes);Assert.AreEqual(edit==1?0:1,root.Version);Assert.AreEqual(0,child.Version);
        Assert.AreEqual(edit==1?"newer root":"newer child",edit==1?root.Description:child.Description);
        Assert.IsInstanceOfType(result.Exception,typeof(EthereumPartialStorageMutationException));
        Assert.AreEqual("0x"+new string('1',64),((INativeMutationReconciliationRequired)result.Exception!).OriginalTransactionId);
    });

    [TestMethod]
    public Task EmbeddedChildCachesPreserveSelectedGraphAndUnselectedManagerReferences()=>WithKey(()=>{
        var root=new Holon();var child=new Holon();var grandchild=new Holon();root.Children.Add(child);child.Children.Add(grandchild);
        var method=typeof(EthereumOASIS).GetMethod("PlanStoredGraph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var plan=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{root},true,true,0,0})!;
        var snapshot=((List<IHolon>)plan[0]!)[0];var embedded=snapshot.Children.Single();
        Assert.AreEqual(embedded.Id.ToString(),snapshot.ChildIdListCache);Assert.AreEqual(embedded.Id+","+embedded.Children.Single().Id,snapshot.AllChildIdListCache);
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,grandchild.Id);
        var reference=Guid.NewGuid();var manager=new Holon{Id=Guid.NewGuid(),ChildrenIds=new List<Guid>{reference},ChildIdListCache=reference.ToString(),AllChildIdListCache=reference.ToString()};
        var managerPlan=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{manager},false,true,0,0})!;
        var managerSnapshot=((List<IHolon>)managerPlan[0]!)[0];Assert.AreEqual(0,managerSnapshot.Children.Count);
        Assert.AreEqual(reference.ToString(),managerSnapshot.ChildIdListCache);Assert.AreEqual(reference.ToString(),managerSnapshot.AllChildIdListCache);
        var selectedEmpty=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{manager},true,true,0,0})!;
        var empty=(Holon)((List<IHolon>)selectedEmpty[0]!)[0];Assert.AreEqual(0,empty.ChildrenIds.Count);Assert.AreEqual(string.Empty,empty.ChildIdListCache);Assert.AreEqual(string.Empty,empty.AllChildIdListCache);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task CapturedNamedRolesReachEncryptedHistoryWithoutRecapturingCaller()=>WithKey(async()=>{
        var source=new NamedOwner();source.Components.Add(new Holon());
        var planner=typeof(EthereumOASIS).GetMethod("PlanStoredGraph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var graph=(System.Runtime.CompilerServices.ITuple)planner.Invoke(null,new object[]{new IHolon[]{source},true,true,0,0})!;
        var node=((System.Collections.IList)graph[1]!)[0]!;
        var write=(PlannedHolonWrite)node.GetType().GetProperty("Write")!.GetValue(node)!;
        var expected=HolonChildRoleManifestJson.Serialize(write.RelationshipManifest);source.Components.Clear();
        var method=typeof(EthereumOASIS).GetMethod("StageStorageHistoryWithPlanAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon));
        var staged=await (Task<(Holon Record,string Ciphertext)>)method.Invoke(null,new object?[]{"holon",write.Snapshot,BigInteger.Zero,null,null,true,write})!;
        Assert.AreEqual(expected,staged.Record.ChildRoleManifestJson);Assert.AreEqual(Guid.Empty,source.Id);
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY");
        try{
            using var codec=new EncryptedRecordCodec(key,compress:true);
            var context="oasis/ethereum/storage/v1/holon/"+HashUtility.GetNumericHash(staged.Record.Id.ToString()).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var document=Newtonsoft.Json.Linq.JObject.Parse(codec.Decrypt(context,staged.Ciphertext));
            var decoded=OasisJson.DeserializeStored<Holon>(new EmbeddedRecordHistory().CanonicalPayload(document).ToString(Newtonsoft.Json.Formatting.None));
            Assert.AreEqual(expected,decoded.ChildRoleManifestJson);Assert.AreEqual(write.SelectedChildIds.Single(),decoded.Children.Single().Id);
        }finally{CryptographicOperations.ZeroMemory(key);}
    });

    private sealed class NamedOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override List<IHolon> AllChildren=>Components.Cast<IHolon>().ToList();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }
    private static async Task WithKey(Func<Task> test)
    {
        const string name="OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY";var previous=Environment.GetEnvironmentVariable(name);var key=RandomNumberGenerator.GetBytes(32);
        try{Environment.SetEnvironmentVariable(name,Convert.ToBase64String(key));await test();}
        finally{Environment.SetEnvironmentVariable(name,previous);CryptographicOperations.ZeroMemory(key);}
    }
    [DataTestMethod]
    [DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public void ProviderLinkedReadsHonorCapturedMembershipAndIndependentChildVersions(int kind)
    {
        var root=new Holon{Id=Guid.NewGuid(),Version=1};
        var selected=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=9};
        var unselected=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=8};
        if(kind!=0)root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,kind==5?Guid.NewGuid():root.Id,
            new[]{new HolonChildRole("Components",kind==1?Array.Empty<Guid>():new[]{kind==3?Guid.NewGuid():selected.Id})}));
        if(kind==4)selected.ParentHolonId=Guid.NewGuid();
        var method=typeof(EthereumOASIS).GetMethod("CreateHolonProjector",BindingFlags.Static|BindingFlags.NonPublic)!;
        var project=(Func<IHolon,IHolon>)method.Invoke(null,new object[]{new IHolon[]{root,selected,unselected},true,true,0,0,true,false})!;
        if(kind>=3){Assert.ThrowsException<InvalidDataException>(()=>project(root));return;}
        var result=project(root);Assert.AreEqual(1,result.Version);
        Assert.AreEqual(kind==0?2:kind==1?0:1,result.Children.Count);
        if(kind==2){Assert.AreEqual(selected.Id,result.Children.Single().Id);Assert.AreEqual(9,result.Children.Single().Version);}
        Assert.AreEqual(0,root.Children.Count);Assert.AreEqual(9,selected.Version);Assert.AreEqual(8,unselected.Version);
    }

    [TestMethod]
    public Task NumericStorageKeyCollisionRejectsBeforeCallerPublication()=>WithKey(()=>{
        var first=Guid.Parse("00005f42-0000-0000-0000-000000000000");var second=Guid.Parse("00017b6d-0000-0000-0000-000000000000");
        Assert.AreEqual(HashUtility.GetNumericHash(first.ToString()),HashUtility.GetNumericHash(second.ToString()));
        var method=typeof(EthereumOASIS).GetMethod("PlanStoredGraph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[]{new IHolon[]{new Holon{Id=first},new Holon{Id=second}},true,true,0,0}));
        Assert.IsInstanceOfType(error.InnerException,typeof(ArgumentException));return Task.CompletedTask;
    });

    [TestMethod]
    public Task NamedChildrenParticipateWithoutPublishingCallerIdentities()=>WithKey(()=>{
        var source=new NamedOwner();var child=new Holon();source.Components.Add(child);
        var method=typeof(EthereumOASIS).GetMethod("PlanStoredGraph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var plan=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{source},true,true,0,0})!;
        Assert.AreEqual(2,((System.Collections.IList)plan[1]!).Count);
        Assert.AreEqual(Guid.Empty,source.Id);Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,child.ParentHolonId);
        return Task.CompletedTask;
    });

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public Task RootOnlyHistoryPreservesObservedMembership(bool hasRoles)=>WithKey(async()=>{
        var id=Guid.NewGuid();
        string Manifest()=>HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,id,new[]{new HolonChildRole("Components",new[]{Guid.NewGuid()})}));
        var observed=hasRoles?Manifest():null;
        var method=typeof(EthereumOASIS).GetMethod("StageStorageHistoryAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon));
        Task<(Holon Record,string Ciphertext)> Stage(Holon source,BigInteger entity,string? identity,string? ciphertext)=>(Task<(Holon,string)>)method.Invoke(null,new object?[]{"holon",source,entity,identity,ciphertext,true})!;
        var first=await Stage(new Holon{Id=id,ChildRoleManifestJson=observed},0,null,null);
        var caller=new Holon{Id=id,ChildRoleManifestJson=Manifest()};var original=caller.ChildRoleManifestJson;
        var second=await Stage(caller,new BigInteger(HashUtility.GetNumericHash(id.ToString())),id.ToString(),first.Ciphertext);
        Assert.AreEqual(observed,second.Record.ChildRoleManifestJson);Assert.AreEqual(original,caller.ChildRoleManifestJson);
        Assert.AreEqual(2,second.Record.Version);Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);
    });
}
