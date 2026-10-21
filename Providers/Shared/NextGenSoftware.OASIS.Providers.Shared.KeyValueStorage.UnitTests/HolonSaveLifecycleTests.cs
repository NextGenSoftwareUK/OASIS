using System;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonSaveLifecycleTests
{
    [DataTestMethod]
    [DataRow("active")] [DataRow("cleared")] [DataRow("retimed")] [DataRow("reattributed")]
    public void TombstoneCannotBeClearedOrRewrittenBySave(string scenario)
    {
        var old=new Holon{IsActive=false,DeletedDate=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc),DeletedByAvatarId=Guid.NewGuid()};
        var next=new Holon{IsActive=false,DeletedDate=old.DeletedDate,DeletedByAvatarId=old.DeletedByAvatarId};
        if(scenario=="active")next.IsActive=true;
        if(scenario=="cleared")next.DeletedDate=DateTime.MinValue;
        if(scenario=="retimed")next.DeletedDate=next.DeletedDate.AddTicks(1);
        if(scenario=="reattributed")next.DeletedByAvatarId=Guid.NewGuid();
        Assert.ThrowsException<StorageLifecycleConflictException>(()=>HolonSaveLifecycle.RequireCompatibleSave(old,next));
    }
    [TestMethod] public void CompatibleEditsAndLiveOrNewRecordsAreAllowed()
    {
        var time=DateTime.UtcNow;var actor=Guid.NewGuid();
        HolonSaveLifecycle.RequireCompatibleSave(new Holon{IsActive=false,DeletedDate=time,DeletedByAvatarId=actor},new Holon{IsActive=false,DeletedDate=time,DeletedByAvatarId=actor,Name="edited"});
        HolonSaveLifecycle.RequireCompatibleSave(new Holon(),new Holon());HolonSaveLifecycle.RequireCompatibleSave(null,new Holon());
    }
    [TestMethod] public void NativeGuidFormattingAndLegacyMissingActorHaveExplicitIdentitySemantics()
    {
        var time=DateTime.UtcNow;var actor=Guid.NewGuid();
        HolonSaveLifecycle.RequireCompatibleSave(time,actor.ToString("B"),false,time,actor.ToString("D"));
        HolonSaveLifecycle.RequireCompatibleSave(time,null,false,time,Guid.Empty.ToString());
        HolonSaveLifecycle.RequireCompatibleSave(time,"",false,time,null);
    }
    [DataTestMethod] [DataRow(true)] [DataRow(false)]
    public void MalformedDeletionActorsAreIntegrityErrors(bool previous)
    {var time=DateTime.UtcNow;Assert.ThrowsException<InvalidDataException>(()=>HolonSaveLifecycle.RequireCompatibleSave(time,previous?"invalid":Guid.Empty.ToString(),false,time,previous?Guid.Empty.ToString():"invalid"));}
}
