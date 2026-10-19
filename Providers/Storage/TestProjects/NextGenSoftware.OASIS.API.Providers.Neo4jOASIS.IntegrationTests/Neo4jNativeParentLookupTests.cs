using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeParentKeyAbsencePreservesNotFound(bool synchronous)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider = new Provider(uri,"neo4j","oasis-local-test-only");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try {
            var key = "owned-missing-parent-" + Guid.NewGuid().ToString("N");
            var result = synchronous ? provider.LoadHolonsForParent(key,loadChildren:false)
                : await provider.LoadHolonsForParentAsync(key,loadChildren:false);
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            Assert.IsNull(result.Exception);
        } finally { await provider.DeActivateProviderAsync(); }
    }
}
