using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonGraphTraversalTests
{
    private sealed class CountingChildrenHolon:Holon,NextGenSoftware.OASIS.API.Core.Interfaces.IHolon
    {
        [Newtonsoft.Json.JsonIgnore] public int Reads;
        [Newtonsoft.Json.JsonIgnore] public NextGenSoftware.OASIS.API.Core.Interfaces.IHolon CapturedChild;
        public new System.Collections.Generic.IList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> Children
        {get=>++Reads==1 ? new[]{CapturedChild}:Array.Empty<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>();set{}}
    }
    [TestMethod]
    public void ConditionalTopologyUsesTheSingleSelectedChildCollectionCapture()
    {
        var child=new Holon{Id=Guid.NewGuid()};var root=new CountingChildrenHolon{Id=Guid.NewGuid(),CapturedChild=child};
        var request=new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest(
            new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWrite(Guid.NewGuid(),Guid.Empty,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Neo4jOASIS,
                new[]{new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(root.Id,null),
                    new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(child.Id,null)}),
            NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,new[]{root});
        var plan=ConditionalHolonPlan.Create(request,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Neo4jOASIS);
        Assert.AreEqual(1,root.Reads);Assert.AreEqual(child.Id,plan.ChildIds[root.Id][0]);
        Assert.AreEqual(2,plan.Nodes.Count);
    }
    private sealed class CountingHolon:Holon
    {
        [Newtonsoft.Json.JsonIgnore] public int Reads;
        public new string Name {get=>++Reads==1 ? "captured once":"read twice";set{}}
    }
    [TestMethod]
    public void SelectedNodeIntentAndSnapshotUseTheSameSingleSourceSerialization()
    {
        var source=new CountingHolon{Id=Guid.NewGuid()};
        var plan=HolonGraphWritePlan.Create(new[]{source},false,true,0,0);
        Assert.AreEqual(1,source.Reads);
        Assert.AreEqual("captured once",plan.Nodes[0].Snapshot.Name);
        Assert.AreEqual("captured once",Newtonsoft.Json.Linq.JObject.Parse(plan.Nodes[0].CapturedIntentJson).Value<string>("Name"));
        Assert.IsTrue(plan.Nodes[0].CapturedIntentJson.Contains(nameof(CountingHolon)));
    }
    [DataTestMethod]
    [DataRow(false, false, 0, 0)] [DataRow(false, true, 0, 1)]
    [DataRow(false, true, 1, 0)] [DataRow(false, true, 2, 1)]
    [DataRow(true, false, 0, 0)] [DataRow(true, true, 0, 1)]
    [DataRow(true, true, 1, 0)] [DataRow(true, true, 2, 1)]
    public void ParentResultHasAlreadyTraversedOneEdge(bool fromProvider, bool recursive, int maximum, int grandchildCount)
    {
        var parent = new Holon { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id };
        var grandchild = new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id };
        child.Children.Add(grandchild); parent.Children.Add(child);
        var project = HolonGraphProjection.Create(new[] { parent, child, grandchild }, true, recursive,
            maximum, 0, fromProvider, source => OasisJson.Project<Holon>(source), rootsAreChildren: true);
        var projected = project(child);
        Assert.AreEqual(child.Id, projected.Id);
        Assert.AreEqual(grandchildCount, projected.Children.Count());
        Assert.AreEqual(1, child.Children.Count(), "Read projection must not mutate persisted/source graph membership.");
        var offset = HolonGraphProjection.Create(new[] { parent, child, grandchild }, true, true,
            2, 1, fromProvider, source => OasisJson.Project<Holon>(source), rootsAreChildren: true);
        Assert.AreEqual(0, offset(child).Children.Count());
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void DirectRootAndCollectionRootsStillStartAtTheirDeclaredDepth(bool fromProvider)
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        var grand = new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id };
        root.Children.Add(child); child.Children.Add(grand);
        var project = HolonGraphProjection.Create(new[] { root, child, grand }, true, false, 0, 0,
            fromProvider, source => OasisJson.Project<Holon>(source));
        var result = project(root);
        Assert.AreEqual(1, result.Children.Count());
        Assert.AreEqual(0, result.Children.Single().Children.Count());
    }
    [DataTestMethod]
    [DataRow(false, true, 0, 0, 1, false)]
    [DataRow(true, false, 0, 0, 2, false)]
    [DataRow(true, true, 1, 0, 2, false)]
    [DataRow(true, true, 2, 1, 2, false)]
    [DataRow(true, true, 1, 1, 1, false)]
    [DataRow(true, true, 0, 0, 3, true)]
    public void WritePlanDistinguishesExcludedChildrenFromSelectedEmptyMembership(bool children, bool recursive,
        int maximum, int initial, int count, bool lastSelectsChildren)
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        var grand = new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id };
        root.Children.Add(child); child.Children.Add(grand);
        var plan = HolonGraphWritePlan.Create(new[] { root }, children, recursive, maximum, initial);
        Assert.AreEqual(count, plan.Nodes.Count);
        Assert.AreEqual(children && (maximum == 0 || initial < maximum), plan.Nodes.First().SelectsChildren);
        Assert.AreEqual(lastSelectsChildren, plan.Nodes.Last().SelectsChildren);
        Assert.AreEqual(1, child.Children.Count);
    }
    [TestMethod]
    public void UnsupportedRuntimeValueIsAnAdmissionErrorRatherThanStoredCorruption()
    {
        var source = new Holon { MetaData = new() { ["unsupported"] = new Action(() => { }) } };
        var error = Assert.ThrowsException<Newtonsoft.Json.JsonSerializationException>(() =>
            HolonGraphWritePlan.Create(new[] { source }, false, true, 0, 0));
        StringAssert.Contains(error.Message, "System.Action");
        Assert.AreEqual(Guid.Empty, source.Id);
        Assert.IsFalse(source.IsSaving);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void CyclicPersistentMetadataFailsBeforeItCanBeTruncated(bool list)
    {
        object cycle;
        if (list) { var values = new List<object>(); values.Add(values); cycle = values; }
        else { var values = new Dictionary<string, object>(); values["self"] = values; cycle = values; }
        var source = new Holon { MetaData = new() { ["owned"] = cycle } };
        var error = Assert.ThrowsException<Newtonsoft.Json.JsonSerializationException>(() =>
            HolonGraphWritePlan.Create(new[] { source }, false, true, 0, 0));
        StringAssert.Contains(error.Message, "Self referencing loop");
        Assert.AreEqual(Guid.Empty, source.Id);
    }

    [TestMethod]
    public void StrictInputCapturePreservesExistingBytesAndRepeatedAcyclicValues()
    {
        var value = new Dictionary<string, object> { ["value"] = 42 };
        var source = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["first"] = value, ["second"] = value } };
        Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize<IHolon>(source),
            NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.SerializeInput<IHolon>(source));
        var plan = HolonGraphWritePlan.Create(new[] { source }, false, true, 0, 0);
        Assert.AreEqual(2, plan.Nodes.Single().Snapshot.MetaData.Count);
    }

    private static Holon[] Chain(int count)
    {
        var rows=Enumerable.Range(0,count).Select(_=>new Holon{Id=Guid.NewGuid()}).ToArray();
        for(var index=1;index<count;index++)
        {rows[index].ParentHolonId=rows[index-1].Id;rows[index-1].Children.Add(rows[index]);}
        return rows;
    }
    [TestMethod]
    public void UnlimitedWriteTraversalUsesHeapFramesAndPreservesPreorder()
    {
        var rows=Chain(4096);
        var plan=HolonGraphWritePlan.Create(new[]{rows[0]},true,true,0,int.MaxValue);
        CollectionAssert.AreEqual(rows.Select(row=>row.Id).ToArray(),plan.Nodes.Select(node=>node.Snapshot.Id).ToArray());
        for(var index=0;index<rows.Length;index++)
        {
            Assert.IsTrue(plan.Nodes[index].SelectsChildren);
            CollectionAssert.AreEqual(index+1<rows.Length ? new[]{rows[index+1].Id}:Array.Empty<Guid>(),
                plan.Nodes[index].SelectedChildIds.ToArray());
        }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void UnlimitedReadTraversalDoesNotDependOnTheProcessStack(bool fromProvider)
    {
        var rows=Chain(12000);
        var project=HolonGraphProjection.Create(rows,true,true,0,0,fromProvider,
            source=>new Holon{Id=source.Id,ParentHolonId=source.ParentHolonId});
        var current=project(rows[0]);
        for(var index=0;index<rows.Length;index++)
        {
            Assert.AreEqual(rows[index].Id,current.Id);Assert.AreNotSame(rows[index],current);
            Assert.AreEqual(index+1<rows.Length ? 1:0,current.Children.Count);
            if(index+1<rows.Length)current=(Holon)current.Children.Single();
        }
        Assert.AreEqual(1,rows[0].Children.Count);
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public void ParentResultsAtMaximumCurrentDepthDoNotOverflow(bool fromProvider,bool recursive)
    {
        var rows=Chain(2);
        var project=HolonGraphProjection.Create(rows,true,recursive,0,int.MaxValue,fromProvider,
            source=>new Holon{Id=source.Id},rootsAreChildren:true);
        Assert.AreEqual(recursive ? 1:0,project(rows[0]).Children.Count);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ReadTraversalKeepsCycleDetectionScopedToTheCurrentPath(bool fromProvider)
    {
        var rows=Chain(3);rows[0].ParentHolonId=rows[2].Id;rows[2].Children.Add(rows[0]);
        var project=HolonGraphProjection.Create(rows,true,true,0,0,fromProvider,source=>new Holon{Id=source.Id});
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>project(rows[0]));
        var bounded=HolonGraphProjection.Create(rows,true,true,1,0,fromProvider,source=>new Holon{Id=source.Id});
        Assert.AreEqual(0,bounded(rows[0]).Children.Single().Children.Count);
    }
    [TestMethod]
    public void WriteTraversalPreservesRootSiblingOrderAndRejectsSharedNodes()
    {
        var rows=Chain(3);var sibling=new Holon{Id=Guid.NewGuid(),ParentHolonId=rows[0].Id};
        rows[0].Children.Add(sibling);var secondRoot=new Holon{Id=Guid.NewGuid()};
        var plan=HolonGraphWritePlan.Create(new[]{rows[0],secondRoot},true,true,0,0);
        CollectionAssert.AreEqual(new[]{rows[0].Id,rows[1].Id,rows[2].Id,sibling.Id,secondRoot.Id},
            plan.Nodes.Select(node=>node.Snapshot.Id).ToArray());
        CollectionAssert.AreEqual(new[]{rows[1].Id,sibling.Id},plan.Nodes[0].SelectedChildIds.ToArray());
        rows[0].Children.Add(rows[1]);
        Assert.ThrowsException<ArgumentException>(()=>HolonGraphWritePlan.Create(new[]{rows[0]},true,true,0,0));
    }
}
