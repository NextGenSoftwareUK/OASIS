using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Enums;

public class PredictiveEngineOwnershipTests
{
    [Fact]
    public void RoutersShareTheirManagersEngineButNotOtherManagersEngine()
    {
        var first = new ProviderManager(null, null);
        var second = new ProviderManager(null, null);
        var engine = PredictiveFailoverEngine.ForProviderManager(first);
        Assert.Same(engine, PredictiveFailoverEngine.ForProviderManager(first));
        Assert.NotSame(engine, PredictiveFailoverEngine.ForProviderManager(second));
        var field = typeof(OASISHyperDrive).GetField("_failoverEngine", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Same(engine, field.GetValue(new OASISHyperDrive(first)));
        Assert.Same(engine, field.GetValue(new OASISHyperDrive(first)));
        Assert.Same(PredictiveFailoverEngine.Instance,
            PredictiveFailoverEngine.ForProviderManager(ProviderManager.Instance));
    }

    [Fact]
    public void FailureSnapshotsAndInputDoNotExposeMutableEngineState()
    {
        var engine = PredictiveFailoverEngine.ForProviderManager(new ProviderManager(null, null));
        var original = new FailureEvent { Timestamp = DateTime.UtcNow, Cause = "original", Description = "owned" };
        engine.RecordFailureEvent(ProviderType.MongoDBOASIS, original);
        original.Cause = "caller mutation";
        var history = engine.GetFailureHistory();
        history[ProviderType.MongoDBOASIS][0].Description = "snapshot mutation";
        history[ProviderType.MongoDBOASIS].Clear();
        var model = engine.GetFailureModel(ProviderType.MongoDBOASIS);
        model.HistoricalFailures[0].Cause = "model mutation";
        model.HistoricalFailures.Clear();
        model.RiskFactors.Clear();
        var fresh = engine.GetFailureHistory()[ProviderType.MongoDBOASIS];
        Assert.Equal("original", Assert.Single(fresh).Cause);
        Assert.Equal("owned", fresh[0].Description);
        Assert.Single(engine.GetFailureModel(ProviderType.MongoDBOASIS).HistoricalFailures);
        Assert.NotEmpty(engine.GetFailureModel(ProviderType.MongoDBOASIS).RiskFactors);
    }

    [Fact]
    public void NullManagerIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PredictiveFailoverEngine.ForProviderManager(null!));
    }
}
