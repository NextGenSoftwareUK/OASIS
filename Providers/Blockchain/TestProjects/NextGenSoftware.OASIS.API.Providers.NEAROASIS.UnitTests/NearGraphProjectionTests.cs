using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearGraphProjectionTests
{
    private static Task<OASISResult<IHolon>> Project(Holon root, bool children=true, bool recursive=true, int depth=0)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1", "unused", "unused", "unused", "", "");
        return (Task<OASISResult<IHolon>>)typeof(NEAROASIS)
            .GetMethod("ProjectLoadedHolonAsync",BindingFlags.Instance|BindingFlags.NonPublic)!
            .Invoke(provider,new object[]{new OASISResult<IHolon>(root),children,recursive,depth,false,false})!;
    }

    [TestMethod]
    public async Task ProjectionHonorsDepthRecursionAndNoChildrenWithoutMutatingSources()
    {
        var root=new Holon{Id=Guid.NewGuid()};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        var grandchild=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};
        root.Children.Add(child);child.Children.Add(grandchild);
        var complete=await Project(root);
        Assert.IsFalse(complete.IsError,complete.Message);
        Assert.AreEqual(grandchild.Id,complete.Result.Children.Single().Children.Single().Id);
        Assert.AreNotSame(root,complete.Result);
        foreach(var shallow in new[]{await Project(root,recursive:false),await Project(root,depth:1)})
        {
            Assert.IsFalse(shallow.IsError,shallow.Message);
            Assert.AreEqual(child.Id,shallow.Result.Children.Single().Id);
            Assert.AreEqual(0,shallow.Result.Children.Single().Children.Count());
        }
        Assert.AreEqual(0,(await Project(root,children:false)).Result.Children.Count());
        Assert.AreEqual(1,child.Children.Count());
    }

    [TestMethod]
    public async Task ProjectionRejectsCyclesAndNegativeDepthRatherThanUnboundedRecursion()
    {
        var root=new Holon{Id=Guid.NewGuid()};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        root.Children.Add(child);child.Children.Add(root);
        Assert.IsTrue((await Project(root)).IsError);
        Assert.IsTrue((await Project(new Holon{Id=Guid.NewGuid()},depth:-1)).IsError);
    }
}
