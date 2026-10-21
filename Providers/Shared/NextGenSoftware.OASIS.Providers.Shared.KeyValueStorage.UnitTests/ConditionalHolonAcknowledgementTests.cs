using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class ConditionalHolonAcknowledgementTests
{
    [DataTestMethod]
    [DataRow(ConditionalHolonOperation.Save,false)][DataRow(ConditionalHolonOperation.Save,true)]
    [DataRow(ConditionalHolonOperation.SoftDelete,false)][DataRow(ConditionalHolonOperation.SoftDelete,true)]
    [DataRow(ConditionalHolonOperation.HardDelete,false)][DataRow(ConditionalHolonOperation.HardDelete,true)]
    public void CountsTheAcknowledgedSelectedGraphAndPreservesItsIdentity(ConditionalHolonOperation operation,bool children)
    {
        var root=new Holon{Id=Guid.NewGuid(),Name="owned ack",Version=7,IsNewHolon=true};
        var child=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};var other=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};
        var roots=new List<IHolon>{root};if(children){root.Children.Add(child);roots.Add(other);}
        var result=ConditionalHolonAcknowledgement.Create(roots,operation);
        Assert.IsFalse(result.IsError);Assert.AreEqual(operation==ConditionalHolonOperation.Save,result.IsSaved);
        Assert.AreEqual(operation!=ConditionalHolonOperation.Save,result.IsDeleted);
        Assert.AreEqual(operation==ConditionalHolonOperation.Save?(children?3:1):0,result.SavedCount);
        Assert.AreEqual(operation==ConditionalHolonOperation.Save?0:(children?3:1),result.DeletedCount);
        Assert.IsTrue(result.HasAnyHolonsChanged);Assert.IsFalse(root.IsNewHolon);Assert.AreSame(root,result.Result.First());
        Assert.AreEqual(7,root.Version);Assert.AreEqual("owned ack",root.Name);
        if(children){Assert.IsFalse(child.IsNewHolon);Assert.IsFalse(other.IsNewHolon);Assert.AreSame(child,result.Result.First().Children.Single());}
    }

    [DataTestMethod][DataRow(-1)][DataRow(3)]
    public void UndefinedOperationCannotPublishAnAcknowledgement(int operation)
    {
        var root=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>ConditionalHolonAcknowledgement.Create(new[]{root},(ConditionalHolonOperation)operation));
        Assert.IsTrue(root.IsNewHolon);
    }
    [TestMethod]
    public void NullRootsAreRejected()=>Assert.ThrowsException<ArgumentNullException>(()=>ConditionalHolonAcknowledgement.Create(null!,ConditionalHolonOperation.Save));
    [TestMethod]
    public void EmptyRootsCannotBecomeSuccessfulAcknowledgement()=>Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonAcknowledgement.Create(Array.Empty<IHolon>(),ConditionalHolonOperation.Save));
    [TestMethod]
    public void EnumeratesOriginalRootsOnlyOnce()
    {
        var calls=0;var root=new Holon{Id=Guid.NewGuid()};
        IEnumerable<IHolon> Roots(){calls++;if(calls>1)throw new InvalidOperationException("Original source enumerated twice.");yield return root;}
        var result=ConditionalHolonAcknowledgement.Create(Roots(),ConditionalHolonOperation.Save);
        Assert.AreEqual(1,calls);Assert.AreEqual(1,result.SavedCount);Assert.AreSame(root,result.Result.Single());Assert.AreEqual(1,calls);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void InvalidGraphCannotPublishPartialPersistedFlags(bool cycle)
    {
        var root=new Holon{Id=Guid.NewGuid(),IsNewHolon=true};var child=new Holon{Id=Guid.Empty,IsNewHolon=true};
        root.Children.Add(cycle?root:child);
        Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonAcknowledgement.Create(new[]{root},ConditionalHolonOperation.Save));
        Assert.IsTrue(root.IsNewHolon);Assert.IsTrue(child.IsNewHolon);Assert.AreEqual(Guid.Empty,child.Id);
    }
}
