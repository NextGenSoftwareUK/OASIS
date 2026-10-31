using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonGraphManifestProjectionTests
{
    private static string Manifest(Guid root,params Guid[] children)=>HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root,new[]{new HolonChildRole("Children",children)}));
    private static Func<IHolon,IHolon> Project(IHolon[] pool,bool fromProvider=true)=>HolonGraphProjection.Create(pool,true,true,0,0,fromProvider,source=>OasisJson.Project<Holon>(source));

    [TestMethod]
    public void ExplicitEmptyManifestDoesNotResurrectAnUnlinkedRetainedRecord()
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.ChildRoleManifestJson=Manifest(root.Id);
        Assert.AreEqual(0,Project(new IHolon[]{root,child})(root).Children.Count);Assert.AreEqual(root.Id,child.ParentHolonId);
    }

    [DataTestMethod][DataRow("missing")][DataRow("parent")][DataRow("duplicate")][DataRow("owner")]
    public void InvalidSelectedMembershipCannotBecomeAnUnrelatedParentScan(string invalid)
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.ChildRoleManifestJson=Manifest(invalid=="owner"?Guid.NewGuid():root.Id,child.Id);
        if(invalid=="parent")child.ParentHolonId=Guid.NewGuid();
        var pool=invalid=="missing"?new IHolon[]{root}:invalid=="duplicate"?new IHolon[]{root,child,new Holon{Id=child.Id,ParentHolonId=root.Id}}:new IHolon[]{root,child};
        Assert.ThrowsException<InvalidDataException>(()=>Project(pool)(root));
    }

    [TestMethod]
    public void HistoricalOwnerSelectsCurrentNamedMemberWithoutChangingEmbeddedMode()
    {
        var root=new Holon{Id=Guid.NewGuid(),Version=1};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=7,Name="independent current"};
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Components",new[]{child.Id})}));
        var currentRoot=new Holon{Id=root.Id,Version=3,ChildRoleManifestJson=Manifest(root.Id)};
        var projected=Project(new IHolon[]{currentRoot,child})(root);Assert.AreEqual(1,projected.Version);Assert.AreEqual(7,projected.Children.Single().Version);Assert.AreEqual(root.ChildRoleManifestJson,((Holon)projected).ChildRoleManifestJson);
        root.Children.Add(new Holon{Id=child.Id,ParentHolonId=root.Id,Version=2,Name="embedded snapshot"});
        Assert.AreEqual(2,Project(new IHolon[]{currentRoot,child},false)(root).Children.Single().Version);
    }
}
