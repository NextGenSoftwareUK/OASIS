using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task UnfilteredMappingUsesNativeUnionPagingAndOwnerIsolation(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,unfilteredMappingHttp:true);
    [TestMethod]
    public Task UnfilteredSqliteMappingRetainsCanonicalStoredText()
        => RunGroupedDistinctNativeFixture("SQLLiteDBOASIS",false,unfilteredMappingHttp:true);
}
