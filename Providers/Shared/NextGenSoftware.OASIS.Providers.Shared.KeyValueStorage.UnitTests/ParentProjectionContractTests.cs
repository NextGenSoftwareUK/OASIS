using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ParentProjectionContractTests
{
    [DataTestMethod]
    [DataRow(false,false,0,0)] [DataRow(true,false,0,0)]
    [DataRow(false,true,1,0)] [DataRow(true,true,2,1)]
    public void ParentLookupCountsTheEdgeToItsResult(bool fromProvider,bool recursive,int maximum,int expected)
    {
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid()};
        var grandchild=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};
        child.Children.Add(grandchild);
        var project=HolonGraphProjection.Create(new[]{child,grandchild},true,recursive,maximum,0,
            fromProvider,source=>OasisJson.Project<Holon>(source),rootsAreChildren:true);
        Assert.AreEqual(expected,project(child).Children.Count());
        Assert.AreEqual(1,child.Children.Count());
    }
}
