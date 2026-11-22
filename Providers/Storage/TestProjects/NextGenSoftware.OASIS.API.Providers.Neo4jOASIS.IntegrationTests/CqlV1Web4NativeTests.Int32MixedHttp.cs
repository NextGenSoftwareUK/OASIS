using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task Int32MixedHttpReconcilesRealVirtualFiltersPagingAndUsage(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,int32FilterHttp:true);
}
