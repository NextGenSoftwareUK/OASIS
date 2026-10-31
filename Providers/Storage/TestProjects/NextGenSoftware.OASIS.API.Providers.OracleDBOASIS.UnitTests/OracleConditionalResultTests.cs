using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleConditionalResultTests
{
    [DataTestMethod]
    [DataRow(ConditionalHolonOperation.Save,false)][DataRow(ConditionalHolonOperation.Save,true)]
    [DataRow(ConditionalHolonOperation.SoftDelete,false)][DataRow(ConditionalHolonOperation.SoftDelete,true)]
    [DataRow(ConditionalHolonOperation.HardDelete,false)][DataRow(ConditionalHolonOperation.HardDelete,true)]
    public void ConditionalAcknowledgementCountsSelectedEntitiesNotJustRoots(ConditionalHolonOperation operation,bool graph)
    {
        var root=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};var other=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};
        var child=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};if(graph)root.Children.Add(child);
        var roots=graph?new[]{root,other}:new[]{root};
        var method=typeof(OracleDBOASIS).GetMethod("ConditionalAcknowledgement",BindingFlags.NonPublic|BindingFlags.Static)!;
        var result=(OASISResult<IEnumerable<IHolon>>)method.Invoke(null,new object[]{roots,operation})!;
        Assert.IsFalse(result.IsError);Assert.AreEqual(operation==ConditionalHolonOperation.Save,result.IsSaved);
        Assert.AreEqual(operation!=ConditionalHolonOperation.Save,result.IsDeleted);
        Assert.AreEqual(operation==ConditionalHolonOperation.Save?(graph?3:1):0,result.SavedCount);
        Assert.AreEqual(operation==ConditionalHolonOperation.Save?0:(graph?3:1),result.DeletedCount);
        Assert.IsTrue(result.HasAnyHolonsChanged);Assert.IsFalse(root.IsNewHolon);
        if(graph){Assert.IsFalse(other.IsNewHolon);Assert.IsFalse(child.IsNewHolon);}
    }
}
