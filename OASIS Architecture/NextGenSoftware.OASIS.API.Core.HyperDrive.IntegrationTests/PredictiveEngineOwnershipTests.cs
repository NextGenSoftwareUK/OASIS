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
    public async Task ConcurrentFailureRecordingAndSnapshotReadsRetainAllOwnedEvents()
    {
        var engine = PredictiveFailoverEngine.ForProviderManager(new ProviderManager(null, null));
        var workers = Enumerable.Range(0, 4).Select(worker => Task.Run(() =>
        {
            for (var index = 0; index < 200; index++)
            {
                engine.RecordFailureEvent(ProviderType.MongoDBOASIS,
                    new FailureEvent { Timestamp = DateTime.UtcNow, Cause = $"{worker}:{index}" });
                foreach (var events in engine.GetFailureHistory().Values)
                    events.Clear();
                engine.GetFailureModel(ProviderType.MongoDBOASIS).RiskFactors.Clear();
            }
        }));
        await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(30));
        var retained = engine.GetFailureHistory()[ProviderType.MongoDBOASIS];
        Assert.Equal(800, retained.Count);
        Assert.Equal(800, retained.Select(item => item.Cause).Distinct().Count());
        Assert.Equal(800, engine.GetFailureModel(ProviderType.MongoDBOASIS).HistoricalFailures.Count);
    }

    [Fact]
    public void PreventiveActionSnapshotsDoNotExposeInternalRecords()
    {
        var engine = PredictiveFailoverEngine.ForProviderManager(new ProviderManager(null, null));
        typeof(PredictiveFailoverEngine).GetMethod("RecordPreventiveAction", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(engine, new object[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS });
        var snapshot = engine.GetPreventiveActions();
        snapshot[0].Success = false;
        snapshot[0].ToProvider = ProviderType.Default;
        snapshot.Clear();
        var fresh = Assert.Single(engine.GetPreventiveActions());
        Assert.True(fresh.Success);
        Assert.Equal(ProviderType.SQLLiteDBOASIS, fresh.ToProvider);
    }

    [Fact]
    public void NullManagerIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PredictiveFailoverEngine.ForProviderManager(null!));
    }
}
