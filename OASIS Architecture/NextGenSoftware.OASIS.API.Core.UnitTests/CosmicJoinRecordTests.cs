using System.Collections;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinRecordTests
{
    private static CosmicProjectionSelection<Holon> Key(string alias="key")=>CosmicProjectionSelection<Holon>.Create(alias,h=>h.Name).Result;
    private static CosmicJoinQuery<Holon,Holon,CosmicProjectionRecord> Join()=>CosmicQuery<Holon>.Create()
        .LeftJoinColumns(CosmicQuery<Holon>.Create(),new[]{Key()},new[]{Key()}).Result;

    [Fact]
    public void RuntimeCompositeKeysFreezeBindingsAndKeepBothOriginalSources()
    {
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var left=CosmicQuery<Holon>.Create().WithReadAccess(access).OrderBy(h=>h.Name).Take(3).Result;
        var right=CosmicQuery<Holon>.Create().WithReadAccess(access).OrderByDescending(h=>h.Name).Skip(2).Take(5).Result;
        var keys=new[]{Key("name"),CosmicProjectionSelection<Holon>.Create("region",h=>CosmicQueryMetadata.Value<long>(h,"region")).Result};
        var captured=left.LeftJoinColumns(right,keys,keys);
        Assert.False(captured.IsError,captured.Message);var join=captured.Result;
        keys[0]=Key("changed");
        Assert.Same(left,join.Left);Assert.Same(right,join.Right);Assert.Equal(CosmicJoinKind.Left,join.Kind);
        Assert.Equal(new[]{"name","region"},join.Keys.Select(key=>key.Alias));
        Assert.Equal(actor,join.Left.ReadAccess.ActorId);
    }

    [Theory]
    [InlineData(0,"CQL_JOIN_SCOPE_MISMATCH")] [InlineData(1,"CQL_JOIN_ACCESS_MISMATCH")]
    [InlineData(2,"CQL_JOIN_UNSUPPORTED")] [InlineData(3,"CQL_JOIN_UNSUPPORTED")]
    [InlineData(4,"CQL_JOIN_UNSUPPORTED")]
    public void RuntimeKeysUseTheSameScopeActorArityAndExactDomainRules(int invalid,string code)
    {
        var left=CosmicQuery<Holon>.Create();var right=CosmicQuery<Holon>.Create();
        var l=new[]{Key()};var r=new[]{Key()};
        if(invalid==0)right=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result);
        if(invalid==1)right=right.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        if(invalid==2)r=new[]{Key("other")};
        if(invalid==3)r=new[]{CosmicProjectionSelection<Holon>.Create("key",h=>h.Version).Result};
        if(invalid==4)r=new[]{Key(),Key("second")};
        var result=left.JoinColumns(right,l,r);Assert.Equal(code,result.ErrorCode);Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RightBindingRequiresExplicitAbsenceAndPreservesRequiredSourcePath(bool allowMissing)
    {
        var join=Join();var result=CosmicJoinBinding.Right(join,"revision",(Holon h)=>h.Version,allowMissing);
        Assert.Equal(!allowMissing,result.IsError);
        if(allowMissing)
        {
            var query=join.SelectColumns(new[]{result.Result});
            Assert.False(query.IsError,query.Message);
            Assert.Equal(typeof(int?),query.Result.Columns.Single().ValueType);
            Assert.Equal(typeof(int),query.Result.Columns.Single().Path.ValueType);
        }
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void RuntimeResultsRejectForeignDuplicateAndExcessiveBindings(int invalid)
    {
        var join=Join();var binding=CosmicJoinBinding.Left(join,"label",(Holon h)=>h.Name).Result;
        var columns=new[]{binding};
        if(invalid==0)columns=new[]{CosmicJoinBinding.Left(Join(),"label",(Holon h)=>h.Name).Result};
        if(invalid==1)columns=new[]{binding,CosmicJoinBinding.Presence(join,"label").Result};
        if(invalid==2)columns=Enumerable.Repeat(binding,33).ToArray();
        var result=join.SelectColumns(columns);Assert.True(result.IsError);Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RuntimeRecordsKeepMatchedNullDistinctFromMissingRight(bool invalidLastRow)
    {
        var join=Join();var query=join.SelectColumns(new[]{
            CosmicJoinBinding.Left(join,"label",(Holon h)=>h.Name).Result,
            CosmicJoinBinding.Right(join,"value",(Holon h)=>CosmicQueryMetadata.Value<long?>(h,"value")).Result,
            CosmicJoinBinding.Presence(join,"exists").Result}).Result;
        var l1=Guid.Parse("10000000-0000-0000-0000-000000000001");var l2=Guid.Parse("10000000-0000-0000-0000-000000000002");var r=Guid.NewGuid();
        var packet=new CosmicJoinPacket(new[]{
            new CosmicJoinInputRow(l1,true,Guid.Empty,new object[]{"coffee"},new object[]{"coffee"}),
            new CosmicJoinInputRow(l2,true,Guid.Empty,new object[]{"tea"},new object[]{"tea"})},
            new[]{new CosmicJoinInputRow(r,true,Guid.Empty,new object[]{"coffee"},new object[]{null!})},new[]{
                new CosmicJoinRow(l1,r,"coffee",null!,true),
                new CosmicJoinRow(l2,null,"tea",null!,invalidLastRow?(object)1:false)},2,1,2);
        var result=CosmicJoinResults.Materialize(query,packet);
        if(invalidLastRow){Assert.Equal("CQL_PROVIDER_RESULT_INVALID",result.ErrorCode);Assert.Null(result.Result);return;}
        Assert.False(result.IsError,result.Message);
        Assert.Equal(new[]{true,false},result.Result.Select(row=>row.HasRight));
        Assert.All(result.Result,row=>Assert.Null(row.Value["value"]));
        Assert.Equal(new[]{"label","value","exists"},result.Result[0].Value.Aliases);
        Assert.Equal("tea",result.Result[1].Value["label"]);
    }

    [Fact]
    public void FailedSourceResultsRetainFirstErrorWithoutInspectingKeyLists()
    {
        var left=new OASISResult<CosmicQuery<Holon>>{IsError=true,ErrorCode="FIRST",Message="left source failed"};
        var right=new OASISResult<CosmicQuery<Holon>>{IsError=true,ErrorCode="SECOND"};
        Assert.Equal("FIRST",left.LeftJoinColumns(right,null!,null!).ErrorCode);
    }

    [Fact]
    public void EmptyAndNinthKeyCannotBecomeAnAdmittedJoin()
    {
        var source=CosmicQuery<Holon>.Create();
        Assert.True(source.JoinColumns(source,Array.Empty<CosmicProjectionSelection<Holon>>(),new[]{Key()}).IsError);
        var keys=Enumerable.Range(0,9).Select(index=>Key("key"+index)).ToArray();
        Assert.Equal("CQL_JOIN_UNSUPPORTED",source.JoinColumns(source,keys,keys).ErrorCode);
    }

    [Fact]
    public void ExcessiveReportedColumnCountIsRejectedBeforeIndexingOrEnumerating()
    {
        var list=new Excessive();var result=Join().SelectColumns(list);
        Assert.True(result.IsError);Assert.Equal(0,list.Reads);
    }
    private sealed class Excessive:IReadOnlyList<CosmicJoinBinding<Holon,Holon,CosmicProjectionRecord>>
    {
        public int Reads;public int Count=>int.MaxValue;
        public CosmicJoinBinding<Holon,Holon,CosmicProjectionRecord> this[int index]{get{Reads++;throw new InvalidOperationException();}}
        public IEnumerator<CosmicJoinBinding<Holon,Holon,CosmicProjectionRecord>> GetEnumerator()=>throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
