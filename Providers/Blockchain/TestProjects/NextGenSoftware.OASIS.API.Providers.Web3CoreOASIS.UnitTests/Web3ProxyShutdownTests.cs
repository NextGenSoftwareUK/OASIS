using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class Web3ProxyShutdownTests
{
    [TestMethod]
    public async Task OwnedShutdownHandlesPendingAcceptAbortAndReleasesSamePort()
    {
        for(var index=0;index<25;index++)
        {
            var proxy=new Web3AcceptedWriteProxy();await proxy.DisposeAsync();
        }
    }
}
