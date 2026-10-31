using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    public sealed class NativeProjectionProbe
    {
        public static int Constructors;
        public NativeProjectionProbe(){Constructors++;}
        public string Name {get;set;}="";
        public long Precision {get;set;}
        public ulong Unsigned {get;set;}
        public decimal Amount {get;set;}
    }
    private static Holon CreateEntityProjectionFixture(Guid[] ids,Guid actor,Guid other)
    {
        var root=new Holon{Id=ids[0],Name="protected source root",CreatedByAvatarId=actor,IsPublic=true,Version=1,
            MetaData=new(){["i64"]=9007199254740993L,["u64"]=ulong.MaxValue,["decimal"]=1.0000000000000000000000000001m,["text"]="Café 😀"}};
        root.Children.Add(new Holon{Id=ids[1],ParentHolonId=root.Id,Name="visible match",CreatedByAvatarId=actor,IsPublic=true,Version=1});
        var branch=new Holon{Id=ids[2],ParentHolonId=root.Id,Name="private branch",CreatedByAvatarId=other,Version=1};
        branch.Children.Add(new Holon{Id=ids[3],ParentHolonId=branch.Id,Name="grandchild one",CreatedByAvatarId=other,IsPublic=true,Version=1});
        branch.Children.Add(new Holon{Id=ids[4],ParentHolonId=branch.Id,Name="grandchild two",CreatedByAvatarId=other,IsPublic=true,Version=1});
        root.Children.Add(branch);return root;
    }

    private static async Task VerifyEntityProjection(ICosmicQueryEntityProjectionStorageProvider adapter,Guid rootId,List<object> checks)
    {
        async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicProjectionExecution<TResult>>> Read<T,TResult>(
            CosmicEntityProjectionQuery<T,TResult> query,CosmicEntityCompositionOptions? options=null,int maximumBytes=1048576) where T:IHolon
        {
            var plan=CosmicEntityProjectionPlanner.Plan(query,adapter.QueryProviderType,
                new CosmicProjectionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities),adapter.EntityCompositionCapabilities,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            return await NativeManager(adapter).ExecuteEntityProjectionQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter},10,maximumBytes);
        }
        void Passed(string name)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),entityProjection=name});
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(3).Result;
        var projection=source.Compose().Result.Reverse().Result.Take(2).Result.Select(row=>row.Name).Result;
        var selected=await Read(projection);Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
        CollectionAssert.AreEqual(new[]{"visible match","protected source root"},selected.Result.Providers.Single().Rows.ToArray());Passed("native selection after reversal/window");
        var rootSource=CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result.Skip(0).Result;
        NativeProjectionProbe.Constructors=0;
        var typed=rootSource.Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64"),
            Unsigned=CosmicQueryMetadata.Value<ulong>(row,"u64"),Amount=CosmicQueryMetadata.Value<decimal>(row,"decimal")}).Result;
        var native=await Read(typed);Assert.IsFalse(native.IsError,native.Message+native.Exception);
        var dto=native.Result.Providers.Single().Rows.Single();Assert.AreEqual(9007199254740993L,dto.Precision);
        Assert.AreEqual(ulong.MaxValue,dto.Unsigned);Assert.AreEqual(1.0000000000000000000000000001m,dto.Amount);
        Assert.AreEqual(1,NativeProjectionProbe.Constructors);Passed("exact native cells without source entity construction");
        var firstSource=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).OrderBy(row=>row.Name).Take(1).Result;
        var excluded=await Read(firstSource.Compose().Result.Where(row=>row.Name=="protected source root").Result.Select(row=>row.Name).Result);
        Assert.IsFalse(excluded.IsError,excluded.Message+excluded.Exception);Assert.AreEqual(0,excluded.Result.Providers.Single().Rows.Count);Passed("selection cannot move filter before source page");
        excluded=await Read(source.Compose().Result.Reverse().Result.Take(1).Result.Where(row=>row.Name=="protected source root").Result.Select(row=>row.Name).Result);
        Assert.IsFalse(excluded.IsError,excluded.Message+excluded.Exception);Assert.AreEqual(0,excluded.Result.Providers.Single().Rows.Count);Passed("selection cannot move filter before stage page");
        var tight=CosmicQueryTraversalOptions.Create(2,1).Result;
        var root=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result;
        var protectedQuery=root.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)).Result.Take(0).Result.Select(row=>row.Name).Result;
        var failed=await Read(protectedQuery);Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);Passed("native projection protects preceding scopes");
        var empty=root.Skip(0).Result.Take(0).Result.Select(row=>row.Name).Result;
        foreach(var options in new[]{new CosmicEntityCompositionOptions(maximumInputPayloadBytes:1),new CosmicEntityCompositionOptions(maximumStageRows:1)})
        {
            var limited=await Read(empty,options);Assert.AreEqual("CQL_QUERY_LIMIT",limited.ErrorCode,limited.Message+limited.Exception);Passed("projection source/work budget before empty result");
        }
        var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var secured=CosmicQuery<Holon>.Create(scope).WithReadAccess(policy).Where(row=>row.Id==rootId).Take(1).Result.Compose().Result
            .Where(row=>row.Children.Any(child=>child.Name=="private branch")).Result.Select(row=>row.Name).Result;
        var invisible=await Read(secured);Assert.IsFalse(invisible.IsError,invisible.Message+invisible.Exception);
        Assert.AreEqual(0,invisible.Result.Providers.Single().Rows.Count);Passed("projection preserves relationship visibility");
        NativeProjectionProbe.Constructors=0;
        var invalid=root.Skip(0).Result.Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")}).Result;
        var rejected=await Read(invalid);Assert.AreEqual("CQL_DATA_INVALID",rejected.ErrorCode,rejected.Message+rejected.Exception);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("invalid native cells precede DTO construction");
        var payload=await Read(projection,maximumBytes:1);Assert.AreEqual("CQL_QUERY_LIMIT",payload.ErrorCode,payload.Message+payload.Exception);Passed("projection output payload bound");
    }

    private static async Task VerifyStandardEntityProjection(ICosmicQueryEntityProjectionStorageProvider adapter,Guid rootId,List<object> checks)
    {
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var source=CosmicQuery<Holon>.Create(scope).Where(row=>row.Id==rootId || row.ParentHolonId==rootId).Result;
        var query=CosmicLinq.Create(NativeManager(adapter),adapter,source).Result.OrderBy(row=>row.Name);
        void Passed(string label)=>checks.Add(new{provider=adapter.QueryProviderType.ToString(),standardEntityProjection=label});
        var rows=await query.Take(3).Where(row=>row.Version>=1).Reverse().Take(2).Select(row=>row.Name).ToCosmicListAsync();
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);
        CollectionAssert.AreEqual(new[]{"visible match","protected source root"},rows.Result.ToArray());Passed("standard Select preserves staged order and pages");
        var empty=query.Take(1).Where(row=>row.Name=="protected source root").Select(row=>row.Name).ToArray();
        Assert.AreEqual(0,empty.Length);Passed("standard Select cannot move filter before source page");
        var rootSource=CosmicLinq.Create(NativeManager(adapter),adapter,CosmicQuery<NativeCountWithoutConstructor>.Create(scope).Where(row=>row.Id==rootId).Result).Result;
        var desired="not-matched-at-construction";
        NativeProjectionProbe.Constructors=0;
        var deferred=rootSource.Take(1).Where(row=>row.Name==desired)
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"i64")});
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);desired="protected source root";
        var selected=await deferred.ToCosmicListAsync();Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
        Assert.AreEqual(9007199254740993L,selected.Result.Single().Precision);Assert.AreEqual(1,NativeProjectionProbe.Constructors);
        desired="not-matched-on-second-read";Assert.AreEqual(0,deferred.ToArray().Length);Passed("deferred selector rebinds without application source construction or eager DTOs");
        NativeProjectionProbe.Constructors=0;
        var invalid=await rootSource.Take(1).Where(row=>row.Name=="protected source root")
            .Select(row=>new NativeProjectionProbe{Name=row.Name,Precision=CosmicQueryMetadata.Value<long>(row,"text")}).ToCosmicListAsync();
        Assert.AreEqual("CQL_DATA_INVALID",invalid.ErrorCode,invalid.Message+invalid.Exception);
        Assert.AreEqual(0,NativeProjectionProbe.Constructors);Passed("standard Select rejects invalid native cells before DTOs");
    }
}
