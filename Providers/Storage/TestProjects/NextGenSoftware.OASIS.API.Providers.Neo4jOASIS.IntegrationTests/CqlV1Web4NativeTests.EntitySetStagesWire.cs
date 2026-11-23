using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeEntitySetStagesHttpPreservesWindowsColumnsAndPermissions(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyEntitySetStagesHttpJourney,nativeJourneySuffix:"-entity-set-stages-wire-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyEntitySetStagesHttpJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var checks=await VerifyNativeSetHttpJourney(provider,owned,timings,lateSelection:true,entityStages:true);
        Assert.AreEqual(3,checks);
        return new NativeFixtureJourneyProof(3,1,new Dictionary<string,bool>{
            ["orderedEntityStagesKeepLateColumns"]=true,
            ["ownerForeignAndAnonymousPolicyPreserved"]=true,
            ["oneSettledPhysicalAttemptPerAuthenticatedRequest"]=true});
    }
}
