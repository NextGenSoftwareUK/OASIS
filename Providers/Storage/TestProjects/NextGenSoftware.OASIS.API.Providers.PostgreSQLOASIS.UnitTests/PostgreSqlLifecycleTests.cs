using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlLifecycleTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task FailedActivationClearsPreviouslyPublishedState(bool synchronous)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true"){IsProviderActivated=true};
        var result=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);Assert.IsFalse(result.Result);
        Assert.IsFalse(provider.IsProviderActivated,"Failed native activation must not leave a stale activated provider.");
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DeactivationIsLocalIdempotentAndClearsState(bool synchronous)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true"){IsProviderActivated=true};
        for(var index=0;index<2;index++){
            var result=synchronous?provider.DeActivateProvider():await provider.DeActivateProviderAsync();
            Assert.IsFalse(result.IsError);Assert.IsTrue(result.Result);Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
