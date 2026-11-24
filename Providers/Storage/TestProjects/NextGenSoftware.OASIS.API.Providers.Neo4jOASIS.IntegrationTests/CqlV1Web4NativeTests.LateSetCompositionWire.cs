using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeLateSetCompositionHttpPreservesOccurrencesColumnsAndPermissions(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyLateSetCompositionHttpJourney,nativeJourneySuffix:"-late-set-composition-wire-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyLateSetCompositionHttpJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var checks=await VerifyNativeSetHttpJourney(provider,owned,timings,lateSelection:true,selectedStages:true);
        Assert.AreEqual(3,checks);
        return new NativeFixtureJourneyProof(3,1,new Dictionary<string,bool>{
            ["selectedStagesAfterEntityMembership"]=true,
            ["ownerForeignAndAnonymousPolicyPreserved"]=true,
            ["oneSettledPhysicalAttemptPerAuthenticatedRequest"]=true});
    }
}
