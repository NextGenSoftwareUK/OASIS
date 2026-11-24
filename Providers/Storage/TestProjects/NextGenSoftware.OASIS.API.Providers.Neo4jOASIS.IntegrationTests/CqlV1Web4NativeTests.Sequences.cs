using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    /// <summary>M08: native atomic sequences and HiLo blocks never hand out a value twice.</summary>
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeSequencesReserveUniqueBlocksUnderConcurrency(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifySequenceJourney,nativeJourneySuffix:"-sequences-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifySequenceJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var sequences=(ICosmicSequenceStorageProvider)provider;var checks=0;
        var name="seq"+Guid.NewGuid().ToString("N");
        var first=await sequences.ReserveSequenceBlockAsync(name);
        Assert.IsFalse(first.IsError,first.Message+first.Exception);
        Assert.AreEqual(1L,first.Result.First);Assert.AreEqual(1L,first.Result.Last);checks++;
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var blocks=await Task.WhenAll(Enumerable.Range(0,20).Select(_=>sequences.ReserveSequenceBlockAsync(name,5)));
        timings.Add("concurrent-blocks",clock.ElapsedMilliseconds);
        Assert.IsTrue(blocks.All(block=>!block.IsError),string.Join("; ",blocks.Where(b=>b.IsError).Select(b=>b.Message+b.Exception)));
        var values=blocks.SelectMany(block=>Enumerable.Range(0,5).Select(i=>block.Result.First+i)).ToList();
        CollectionAssert.AreEquivalent(Enumerable.Range(2,100).Select(i=>(long)i).ToList(),values,"Concurrent blocks must tile 2..101 exactly once.");checks++;
        var hilo=new CosmicHiLoGenerator(sequences,name,10);
        var ids=new List<long>();for(var i=0;i<15;i++)ids.Add((await hilo.NextAsync()).Result);
        CollectionAssert.AreEqual(Enumerable.Range(102,15).Select(i=>(long)i).ToList(),ids);checks++;
        var bad=await sequences.ReserveSequenceBlockAsync("bad name",1);
        Assert.IsTrue(bad.IsError);Assert.AreEqual("COSMIC_SEQUENCE_INVALID",bad.ErrorCode);checks++;
        return new NativeFixtureJourneyProof(checks,0,new Dictionary<string,bool>{
            ["concurrentBlocksUniqueVerified"]=true,["hiLoVerified"]=true,["invalidNameRefusedVerified"]=true});
    }
}
