using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class StoredRecordPublicationSnapshotTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public void HighPrecisionMetadataCannotBeRoundedOrHiddenFromPublicationGuard(bool detail,bool edit)
    {
        const decimal original=1.0000000000000000000000000001m;
        const decimal newer=1.0000000000000000000000000002m;
        void Check<T>(T source)where T:class,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase{
            source.MetaData["precise"]=original;
            var snapshot=StoredRecordPublicationSnapshot<T>.Capture(source);
            if(edit){source.MetaData["precise"]=newer;Assert.ThrowsException<InvalidOperationException>(snapshot.ValidateUnchanged);}
            else{Assert.AreEqual(original,Convert.ToDecimal(snapshot.Proposal.MetaData["precise"]));snapshot.ValidateUnchanged();}
        }
        if(detail)Check<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(new AvatarDetail());
        else Check<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(new Holon());
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void LargeFiniteDoubleMetadataRetainsItsRangeAndDetectsEdits(bool edit)
    {
        var source=new Holon{MetaData=new(){["wide"]=1e100}};var snapshot=StoredRecordPublicationSnapshot<Holon>.Capture(source);
        Assert.AreEqual(1e100,Convert.ToDouble(snapshot.Proposal.MetaData["wide"]));
        if(edit){source.MetaData["wide"]=double.BitIncrement(1e100);Assert.ThrowsException<InvalidOperationException>(snapshot.ValidateUnchanged);}
        else snapshot.ValidateUnchanged();
    }
    [TestMethod]
    public void MutableOwnedBytesCannotAliasCallerProposalOrExpectedSnapshot()
    {
        var source=new AvatarDetail();source.Inventory.Add(new InventoryItem{Image2D=new byte[]{1,2,3}});
        var snapshot=StoredRecordPublicationSnapshot<AvatarDetail>.Capture(source);
        Assert.AreNotSame(source.Inventory[0].Image2D,snapshot.Proposal.Inventory[0].Image2D);
        snapshot.Proposal.Inventory[0].Image2D[0]=9;
        CollectionAssert.AreEqual(new byte[]{1,2,3},source.Inventory[0].Image2D);snapshot.ValidateUnchanged();
        source.Inventory[0].Image2D[1]=8;
        Assert.ThrowsException<InvalidOperationException>(snapshot.ValidateUnchanged);
    }

    [TestMethod]
    public void InterfaceContractsUseCanonicalConcreteDtosAndDetectCallerEdits()
    {
        void Check<T>(T source)where T:class,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase{
            var snapshot=StoredRecordPublicationSnapshot<T>.Capture(source);snapshot.ValidateUnchanged();
            Assert.AreNotSame(source,snapshot.Proposal);source.Description="changed";
            Assert.ThrowsException<InvalidOperationException>(snapshot.ValidateUnchanged);
        }
        Check<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar>(new Avatar());
        Check<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(new AvatarDetail());
        Check<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(new Holon());
    }
    [TestMethod]
    public void ProposalChangesDoNotChangeOriginalContractOrPublishToCaller()
    {
        var source=new Holon();var capture=StoredRecordPublicationSnapshot<Holon>.Capture(source);
        capture.Proposal.Id=Guid.NewGuid();capture.Proposal.Version=7;capture.ValidateUnchanged();Assert.AreEqual(Guid.Empty,source.Id);Assert.AreEqual(0,source.Version);
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void IdentityVersionAndContentEditsAreDetected(int field)
    {
        var source=new Holon{Id=Guid.NewGuid(),Name="before"};var capture=StoredRecordPublicationSnapshot<Holon>.Capture(source);
        if(field==0)source.Id=Guid.NewGuid();else if(field==1)source.Version=9;else source.Name="after";
        Assert.ThrowsException<InvalidOperationException>(capture.ValidateUnchanged);
    }
    [TestMethod]
    public void OwnedInventoryEditIsDetected()
    {
        var source=new AvatarDetail();source.Inventory.Add(new InventoryItem{Quantity=1});var capture=StoredRecordPublicationSnapshot<AvatarDetail>.Capture(source);
        source.Inventory[0].Quantity=2;Assert.ThrowsException<InvalidOperationException>(capture.ValidateUnchanged);
    }
    [TestMethod]
    public void MetadataReorderingAloneDoesNotChangeContract()
    {
        var source=new Holon{MetaData=new(){["a"]=1,["b"]="two"}};var capture=StoredRecordPublicationSnapshot<Holon>.Capture(source);
        source.MetaData=new(){["b"]="two",["a"]=1};capture.ValidateUnchanged();
    }
}
