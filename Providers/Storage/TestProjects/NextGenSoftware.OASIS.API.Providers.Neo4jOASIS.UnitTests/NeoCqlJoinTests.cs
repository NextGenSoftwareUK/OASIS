using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlJoinTests
{
    private static CosmicJoinExecutionLimits Limits(int rows=10,long pairs=100)=>new(rows,rows,pairs,100,10000,10000,10000);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void SourceCaptureOwnsIndependentWindowsAndOneSharedParameterBudget(bool outer)
    {
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="left-owned").OrderBy(h=>h.Name).Skip(2).Take(3);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="right-owned").OrderByDescending(h=>h.Name).Skip(4).Take(5);
        var query=(outer?left.LeftJoin(right,h=>h.Name,h=>h.Name):left.Join(right,h=>h.Name,h=>h.Name))
            .Select((l,r)=>new{l.Name,Present=r!=null}).Result;
        var command=Neo4jOASIS.CompileCqlJoin(query,Limits());
        Assert.IsTrue(command.Parameters.Values.Contains("left-owned"));
        Assert.IsTrue(command.Parameters.Values.Contains("right-owned"));
        Assert.IsTrue(command.Parameters.Values.Contains(3));Assert.IsTrue(command.Parameters.Values.Contains(5));
        Assert.AreEqual(1,command.Parameters.Keys.Count(key=>key=="schema"));
        Assert.IsFalse(command.Text.Contains("left-owned"));Assert.IsFalse(command.Text.Contains("right-owned"));
        StringAssert.Contains(command.Text,"RETURN collect(input) AS leftInput");
        StringAssert.Contains(command.Text,"RETURN collect(input) AS rightInput");
        StringAssert.Contains(command.Text,"l.keys[0].s=2 AND r.keys[0].s=2");
        Assert.AreEqual(outer,command.Parameters.Values.Last(value=>value is bool));
    }

    [DataTestMethod]
    [DataRow(0,1)] [DataRow(1,2)]
    public void RemainingLimitsKeepOverflowProofsInsteadOfTruncatingSuccess(int remaining,int sentinel)
    {
        var source=CosmicQuery<Holon>.Create();
        var query=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var command=Neo4jOASIS.CompileCqlJoin(query,Limits(remaining,remaining));
        Assert.IsTrue(command.Parameters.Values.Contains(sentinel));
        Assert.IsTrue(command.Parameters.Values.Contains((long)sentinel));
        StringAssert.Contains(command.Text,"size(pairs) AS nativeCount");
        StringAssert.Contains(command.Text,"size(leftInput) AS nativeCount");
        StringAssert.Contains(command.Text,"ORDER BY kind ASC,lp ASC,rp ASC");
    }

    [TestMethod]
    public void CompositeKeysAndHostilePathsUseBoundParameters()
    {
        var source=CosmicQuery<Holon>.Create();
        var query=source.Join(source,h=>new{h.Name,Region=CosmicQueryMetadata.Value<long>(h,"$region.dot")},
            h=>new{h.Name,Region=CosmicQueryMetadata.Value<long>(h,"$region.dot")}).Select((l,r)=>l.Name).Result;
        var command=Neo4jOASIS.CompileCqlJoin(query,Limits());
        StringAssert.Contains(command.Text,"l.keys[1].v=r.keys[1].v");
        Assert.IsFalse(command.Text.Contains("$region.dot"));
        Assert.IsTrue(command.Parameters.Values.Contains(NeoCqlProjection.PathKey(new[]{"$region.dot"})+"_kind"));
    }

    [TestMethod]
    public void InvalidRemainingBoundsStopBeforeNativeStatementCreation()
    {
        var source=CosmicQuery<Holon>.Create();
        var query=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        Assert.ThrowsException<ArgumentException>(()=>Neo4jOASIS.CompileCqlJoin(query,Limits(1001)));
        Assert.ThrowsException<ArgumentException>(()=>Neo4jOASIS.CompileCqlJoin(query,null!));
    }
}
