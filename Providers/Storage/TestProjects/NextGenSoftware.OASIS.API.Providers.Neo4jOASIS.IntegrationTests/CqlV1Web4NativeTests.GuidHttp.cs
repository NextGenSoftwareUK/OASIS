using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private static readonly Guid MixedGuidFirst = Guid.Parse("abcdef01-2345-6789-abcd-ef0123456789");
    private static readonly Guid MixedGuidSecond = Guid.Parse("abcdef01-2345-6789-abcd-ef012345678a");
    private static readonly Guid MixedGuidStale = Guid.Parse("abcdef01-2345-6789-abcd-ef012345678b");

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GroupedGuidDistinctMixedHttpUsesRealNativeProvider(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, mixedGuidHttp: true);
}
