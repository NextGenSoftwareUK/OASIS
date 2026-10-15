using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaHolonMappingTests
{
    [DataTestMethod]
    [DataRow(false,true,0,0)]
    [DataRow(true,false,0,1)]
    [DataRow(true,true,1,1)]
    [DataRow(true,true,2,2)]
    [DataRow(true,true,0,2)]
    public void SharedStoredGraphProjectionHonorsDepthAndIndependentRoots(bool children,bool recursive,int depth,int expectedEdges)
    {
        var root=new Holon {Id=Guid.NewGuid(),Name="root"};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child"};
        var leaf=new Holon {Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="leaf"};
        var rows=new NextGenSoftware.OASIS.API.Core.Interfaces.IHolon[] {root,child,leaf};
        var project=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(rows,children,recursive,depth,0,true,
            source=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Holon>(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(source)));
        var projected=project(root);
        Assert.AreEqual(expectedEdges>0 ? 1:0,projected.Children.Count);
        if(expectedEdges>0) Assert.AreEqual(expectedEdges>1 ? 1:0,projected.Children[0].Children.Count);
        var independent=project(root); projected.Name="changed";
        Assert.AreEqual("root",independent.Name); Assert.AreEqual("root",root.Name);
    }

    [TestMethod]
    public void SharedStoredGraphCycleFailsInsteadOfReturningPartialGraph()
    {
        var root=new Holon {Id=Guid.NewGuid()};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id}; root.ParentHolonId=child.Id;
        var project=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(new[] {root,child},true,true,0,0,true,source=>new Holon {Id=source.Id});
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>project(root));
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StoredHolonRetainsParentTypeAndAuditFields(bool deleted)
    {
        var holon=new Holon {Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),HolonType=HolonType.Holon,CustomHolonType="park-object",
            Name="actual holon",Description="stored description",CreatedByAvatarId=Guid.NewGuid(),ModifiedByAvatarId=Guid.NewGuid(),DeletedByAvatarId=Guid.NewGuid(),
            IsActive=!deleted,DeletedDate=deleted ? new DateTime(2026,10,14,1,2,3,DateTimeKind.Utc).AddTicks(123):DateTime.MinValue,
            Version=7,PreviousVersionId=Guid.NewGuid()};
        var loaded=JsonConvert.DeserializeObject<SolanaHolonDto>(JsonConvert.SerializeObject(holon.GetSolanaHolonDto()))!.GetHolon();
        Assert.AreEqual(holon.Id,loaded.Id); Assert.AreEqual(holon.ParentHolonId,loaded.ParentHolonId);
        Assert.AreEqual(holon.HolonType,loaded.HolonType); Assert.AreEqual(holon.CustomHolonType,loaded.CustomHolonType);
        Assert.AreEqual(holon.CreatedByAvatarId,loaded.CreatedByAvatarId); Assert.AreEqual(holon.ModifiedByAvatarId,loaded.ModifiedByAvatarId);
        Assert.AreEqual(holon.DeletedByAvatarId,loaded.DeletedByAvatarId); Assert.AreEqual(holon.DeletedDate,loaded.DeletedDate);
        Assert.AreEqual(holon.IsActive,loaded.IsActive); Assert.AreEqual(holon.Version,loaded.Version); Assert.AreEqual(holon.PreviousVersionId,loaded.PreviousVersionId);
    }
}
