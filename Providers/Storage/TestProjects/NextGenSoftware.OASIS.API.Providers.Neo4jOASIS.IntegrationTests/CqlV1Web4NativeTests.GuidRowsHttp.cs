using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GuidRowsMixedHttpUsesRealNativeProviderAndGlobalPaging(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, guidRowsHttp: true);
}
