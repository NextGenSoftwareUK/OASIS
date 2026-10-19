using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleLifecycleTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedActivationClearsPreviouslyActiveFlagAndPreservesSdkError(bool synchronous)
    {
        var provider = new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        SetActive(provider);
        var result = synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DeactivationClearsFlagAndIsIdempotentWithoutOpeningConnection(bool synchronous)
    {
        var provider = new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        SetActive(provider);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = synchronous ? provider.DeActivateProvider() : await provider.DeActivateProviderAsync();
            Assert.IsFalse(result.IsError);
            Assert.IsTrue(result.Result);
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }

    private static void SetActive(OracleDBOASIS provider)
    {
        typeof(OracleDBOASIS).GetProperty(nameof(provider.IsProviderActivated))!.SetValue(provider, true);
        Assert.IsTrue(provider.IsProviderActivated);
    }
}
