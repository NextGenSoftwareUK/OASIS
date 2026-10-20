using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleHistoryStageTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OrdinaryWritesAdvanceDetachedProviderRevisionWithOrWithoutHistory(bool history)
    {
        var provider=new OracleDBOASIS("unused"){IsVersionControlEnabled=history};var source=new Holon{Id=Guid.NewGuid()};
        var firstJson=await Stage(provider,source,null);var first=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(firstJson);
        var second=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(await Stage(provider,source,firstJson));
        var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;
        var a=CosmicConcurrency.GetProviderRevisionId(first,authority);var b=CosmicConcurrency.GetProviderRevisionId(second,authority);
        Assert.IsTrue(a.HasValue);Assert.AreNotEqual(Guid.Empty,a.Value);Assert.IsTrue(b.HasValue);Assert.AreNotEqual(a,b);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(source,authority));
    }
    [TestMethod]
    public async Task ExistingSharedHistorySurvivesDisablingCreationWithoutMutatingProposal()
    {
        var provider=new OracleDBOASIS("unused"){IsVersionControlEnabled=true};
        var proposal=new Holon{Id=Guid.NewGuid(),Name="first"};
        var first=await Stage(provider,proposal,null);proposal.Name="second";
        var second=await Stage(provider,proposal,first);provider.IsVersionControlEnabled=false;proposal.Name="third";
        var third=await Stage(provider,proposal,second);
        using var document=JsonDocument.Parse(third);
        Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
        var history=document.RootElement.GetProperty("_oasis_record_history").GetProperty("records");
        Assert.AreEqual(2,history.GetArrayLength());Assert.AreEqual("first",history[0].GetProperty("Name").GetString());Assert.AreEqual("second",history[1].GetProperty("Name").GetString());
        Assert.AreEqual(0,proposal.Version);Assert.AreEqual(Guid.Empty,proposal.VersionId);
    }

    [TestMethod]
    public async Task PlainDisabledRecordDoesNotInventHistory()
    {
        var proposal=new Holon{Id=Guid.NewGuid(),Version=7};
        using var document=JsonDocument.Parse(await Stage(new OracleDBOASIS("unused"),proposal,null));
        Assert.AreEqual(7,document.RootElement.GetProperty("Version").GetInt32());Assert.IsFalse(document.RootElement.TryGetProperty("_oasis_record_history",out _));
    }

    [TestMethod]
    public async Task StoredIdentityMismatchIsRejectedBeforeNativeMutation()
    {
        var provider=new OracleDBOASIS("unused"){IsVersionControlEnabled=true};
        var original=await Stage(provider,new Holon{Id=Guid.NewGuid()},null);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(provider,new Holon{Id=Guid.NewGuid()},original));
    }

    [TestMethod]
    public async Task MalformedStoredJsonIsDataCorruptionBeforeNativeMutation()
        =>await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(new OracleDBOASIS("unused"),new Holon{Id=Guid.NewGuid()},"{broken"));

    private static async Task<string> Stage(OracleDBOASIS provider,Holon proposal,string? previous)
        =>await (Task<string>)typeof(OracleDBOASIS).GetMethod("StageRecordHistoryAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon)).Invoke(provider,new object?[]{proposal,previous,null})!;
}
