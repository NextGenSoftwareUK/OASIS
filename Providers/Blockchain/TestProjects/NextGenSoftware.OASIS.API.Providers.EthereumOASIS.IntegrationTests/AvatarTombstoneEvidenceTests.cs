using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class AvatarTombstoneEvidenceTests
{
    [DataTestMethod]
    [DataRow("empty-id")]
    [DataRow("local-time")]
    [DataRow("unspecified-time")]
    [DataRow("empty-time")]
    public void InvalidCommitDescriptorsAreRejected(string scenario)
    {
        var id=scenario == "empty-id" ? Guid.Empty : Guid.NewGuid();
        var timestamp=scenario == "empty-time" ? DateTime.SpecifyKind(DateTime.MinValue,DateTimeKind.Utc)
            : DateTime.SpecifyKind(new DateTime(2026,10,14),scenario == "local-time" ? DateTimeKind.Local : scenario == "unspecified-time" ? DateTimeKind.Unspecified : DateTimeKind.Utc);
        Assert.ThrowsException<ArgumentException>(()=>new AvatarTombstone(id,timestamp));
    }

    [DataTestMethod]
    [DataRow(true,true)]
    [DataRow(false,false)]
    public void FailedResultsCannotCarryCommittedEvidence(bool error,bool value)
    {
        var result=new OASISResult<bool> {Result=value,IsError=error};
        Assert.ThrowsException<InvalidOperationException>(()=>AvatarTombstoneEvidence.Attach(result,new AvatarTombstone(Guid.NewGuid(),DateTime.UtcNow)));
        Assert.IsFalse(AvatarTombstoneEvidence.TryGet(result,out _));
    }

    [TestMethod]
    public void HyperDriveResultConversionPreservesEvidenceWithoutSerializingIt()
    {
        var snapshot=new AvatarTombstone(Guid.NewGuid(),DateTime.UtcNow,Guid.NewGuid());
        var source=new OASISResult<bool> {Result=true};
        AvatarTombstoneEvidence.Attach(source,snapshot);
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
        var router=new OASISHyperDrive(new ProviderManager(null,dna));
        var method=typeof(OASISHyperDrive).GetMethod("ConvertResult",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(object),typeof(bool));
        var converted=(OASISResult<object>)method.Invoke(router,new object[] {source})!;
        Assert.IsTrue((bool)converted.Result);
        Assert.IsTrue(AvatarTombstoneEvidence.TryGet(converted,out var copied)); Assert.AreSame(snapshot,copied);
        foreach(var json in new[] {Newtonsoft.Json.JsonConvert.SerializeObject(converted),System.Text.Json.JsonSerializer.Serialize(converted)})
        {
            Assert.IsFalse(json.Contains(snapshot.AvatarId.ToString()));
            Assert.IsFalse(json.Contains(snapshot.DeletedByAvatarId.ToString()));
            Assert.IsFalse(json.Contains("DeletedAtUtc"));
        }
        Assert.AreEqual(0,converted.MetaData.Count);
    }

    [TestMethod]
    public void EvidenceCannotBeReplacedEvenByConcurrentAttachments()
    {
        var result=new OASISResult<bool> {Result=true};
        var attached=0;
        Parallel.For(0,16,_=>
        {
            try { AvatarTombstoneEvidence.Attach(result,new AvatarTombstone(Guid.NewGuid(),DateTime.UtcNow)); Interlocked.Increment(ref attached); }
            catch(InvalidOperationException) { }
        });
        Assert.AreEqual(1,attached); Assert.IsTrue(AvatarTombstoneEvidence.TryGet(result,out _));
        var failed=new OASISResult<object> {IsError=true};
        AvatarTombstoneEvidence.Transfer(result,failed); Assert.IsFalse(AvatarTombstoneEvidence.TryGet(failed,out _));
    }
}
