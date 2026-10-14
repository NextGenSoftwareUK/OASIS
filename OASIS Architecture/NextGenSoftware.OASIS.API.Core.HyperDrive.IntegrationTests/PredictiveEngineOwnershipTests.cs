using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using Xunit;

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
    public void NullManagerIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PredictiveFailoverEngine.ForProviderManager(null!));
    }
}
