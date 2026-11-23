using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [TestMethod]
    public Task NativeNeoProjectedSetsUseTupleEqualityAndOneReadStatement()
        =>RunGroupedDistinctNativeFixture("Neo4jOASIS",false,projectedSetJourney:true);
}
