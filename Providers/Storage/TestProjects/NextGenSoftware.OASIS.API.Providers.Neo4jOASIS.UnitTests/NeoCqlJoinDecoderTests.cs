using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlJoinDecoderTests
{
    private static CosmicJoinSelection<Holon,Holon,string,string> Query()=>CosmicQuery<Holon>.Create()
        .Join(CosmicQuery<Holon>.Create(),h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
    private static Neo4jOASIS.NeoCqlJoinDecoder<Holon,Holon,string,string> Decoder()
        =>new(Query(),new CosmicJoinExecutionLimits(10,10,100,100,10000,10000,10000),()=>{});
    private static Dictionary<string,object> Cell(object value)=>new(){{"s",2L},{"v",value}};
    private static Dictionary<string,object> Source(bool left)=>new()
    {
        {"id",left?"10000000-0000-0000-0000-000000000001":"20000000-0000-0000-0000-000000000001"},
        {"creator",Guid.Empty.ToString("D")},{"visible",true},{"valid",true},{"domainValid",true},
        {"cycle",false},{"traversalLimit",false},{"payloadLimit",false},{"bytes",100L},
        {"keys",new object[]{Cell("coffee")}},{"values",left?new object[]{Cell("coffee")}:Array.Empty<object>()},
        {"ordering",Array.Empty<object>()}
    };
    private static Dictionary<string,object> Record(long kind)=>new()
    {
        {"kind",kind},{"lp",kind==1?-1L:0L},{"rp",kind==0?-1L:0L},
        {"proof",kind==2?null!:Source(kind==0)},{"nativeCount",1L}
    };

    [TestMethod]
    public void CompleteNativeStreamsMaterializeThroughTheSharedCoreBarrier()
    {
        var decoder=Decoder();decoder.Add(Record(0));decoder.Add(Record(1));decoder.Add(Record(2));
        var result=CosmicJoinResults.Materialize(Query(),decoder.Complete());
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual("coffee",result.Result.Single().Value);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void MalformedProofsCannotProduceAnAcknowledgedPacket(int mutation)
    {
        var decoder=Decoder();var left=Record(0);var right=Record(1);var pair=Record(2);
        if(mutation==0)((Dictionary<string,object>)left["proof"])["domainValid"]=false;
        if(mutation==1)((Dictionary<string,object>)left["proof"])["values"]=new object[]{Cell(7L)};
        if(mutation==2)pair["rp"]=9L;
        if(mutation==3)left["nativeCount"]=-1L;
        if(mutation==5)pair["proof"]=Source(true);
        Assert.ThrowsException<InvalidDataException>(()=>
        {
            if(mutation==4){decoder.Add(right);decoder.Add(left);}
            else{decoder.Add(left);decoder.Add(right);decoder.Add(pair);}
            decoder.Complete();
        });
    }

    [TestMethod]
    public void NativeCountsRequireTheCompleteSourceStream()
    {
        var decoder=Decoder();var row=Record(0);row["nativeCount"]=2L;decoder.Add(row);
        Assert.ThrowsException<InvalidDataException>(()=>decoder.Complete());
    }

    [TestMethod]
    public void JoinCapabilitiesDoNotGrantRepeatableSnapshots()
    {
        Assert.IsFalse(Neo4jOASIS.CqlJoinCapabilities.SupportsSharedSnapshot);
        Assert.IsFalse(Neo4jOASIS.CqlJoinCapabilities.SupportsGraphSources);
    }
}
