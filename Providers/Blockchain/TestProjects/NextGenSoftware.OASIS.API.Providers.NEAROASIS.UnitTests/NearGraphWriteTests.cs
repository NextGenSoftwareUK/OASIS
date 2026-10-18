using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearGraphWriteTests
{
    private static NEAROASIS Unavailable() => new("http://127.0.0.1:1","unused","unused","unused","","");

    [TestMethod]
    public async Task InvalidSelectedForestsFailBeforeActivationWithoutPublishingIdentities()
    {
        var root=new Holon();var child=new Holon();root.Children.Add(child);child.Children.Add(root);
        var duplicate=Guid.NewGuid();
        var conflict=new Holon();conflict.Children.Add(new Holon{ParentHolonId=Guid.NewGuid()});
        var forests=new IEnumerable<IHolon>[]
        {
            new[]{root},new[]{new Holon{Id=duplicate},new Holon{Id=duplicate}},
            new[]{new Holon(),null!},new[]{conflict},Array.Empty<IHolon>()
        };
        foreach(var forest in forests)
        {
            var provider=Unavailable();
            var result=await provider.SaveHolonsAsync(forest);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message,"Invalid NEAR save graph");
            Assert.IsFalse(provider.IsProviderActivated);
        }
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,child.Id);
        Assert.AreEqual(Guid.Empty,child.ParentHolonId);
        var defaultSave=await Unavailable().SaveHolonAsync(root);
        Assert.IsTrue(defaultSave.IsError);
        StringAssert.Contains(defaultSave.Message,"Invalid NEAR save graph");
    }

    [TestMethod]
    public async Task RejectedActivationDoesNotPublishPlannedGraphIdentities()
    {
        var root=new Holon();var child=new Holon();root.Children.Add(child);
        var result=await Unavailable().SaveHolonAsync(root,saveChildren:true);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,child.Id);
        Assert.AreEqual(Guid.Empty,child.ParentHolonId);
        Assert.AreEqual(0,root.Version);Assert.AreEqual(Guid.Empty,root.VersionId);
    }

    [TestMethod]
    public async Task NegativeCurrentDepthIsRejectedBySyncAndAsyncBatchEntrypoints()
    {
        var provider=Unavailable();var root=new Holon();
        var asyncResult=await provider.SaveHolonsAsync(new[]{root},curentChildDepth:-1);
        var syncResult=provider.SaveHolons(new[]{root},curentChildDepth:-1);
        Assert.IsTrue(asyncResult.IsError);Assert.IsTrue(syncResult.IsError);
        StringAssert.Contains(asyncResult.Message,"Invalid NEAR save graph");
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(0,root.Version);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
