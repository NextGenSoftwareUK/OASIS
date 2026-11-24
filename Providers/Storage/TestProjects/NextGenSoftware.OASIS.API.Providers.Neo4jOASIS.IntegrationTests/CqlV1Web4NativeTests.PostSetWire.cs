using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeLateSetSelectionHttpPreservesColumnsPermissionsAndMetering(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyLateSetSelectionHttpJourney,nativeJourneySuffix:"-post-set-wire-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyLateSetSelectionHttpJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var checks=await VerifyNativeSetHttpJourney(provider,owned,timings,lateSelection:true);
        Assert.AreEqual(3,checks);
        return new NativeFixtureJourneyProof(3,1,new Dictionary<string,bool>{
            ["lateNativeColumnsUseIdentitySetRoute"]=true,
            ["ownerForeignAndAnonymousPolicyPreserved"]=true,
            ["oneSettledPhysicalAttemptPerAuthenticatedRequest"]=true});
    }
}
