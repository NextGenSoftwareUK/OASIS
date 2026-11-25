using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class CriteriaHolon:Holon
    {
        public string? Batch {get;set;}
        public string? Category {get;set;}
        public int Score {get;set;}
        public string? Code {get;set;}
    }

    // Q61: NHibernate-style Criteria/QueryOver compiled onto the session's native LINQ/CQL root.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeCriteriaAndQueryOverMatchLinq(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyCriteriaQueryOverJourney,nativeJourneySuffix:"-criteria-queryover-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyCriteriaQueryOverJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var checks=0;
        var batch="crit-"+Guid.NewGuid().ToString("N");

        var builder=new CosmicModelBuilder();
        var entity=builder.Entity<CriteriaHolon>();
        entity.Property(row=>row.Batch).HasMetadataKey("crit_batch");
        entity.Property(row=>row.Category).HasMetadataKey("crit_category");
        entity.Property(row=>row.Score).HasMetadataKey("crit_score");
        entity.Property(row=>row.Code).HasMetadataKey("crit_code");
        entity.ShadowProperty<string>("Region").HasMetadataKey("crit_region");
        var writable=builder.Build();Assert.IsFalse(writable.IsError,writable.Message);
        entity.HasQueryFilter(row=>row.Batch==batch);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;

        using var writer=CosmicSession.Create(manager,adapter,access,writable.Result).Result;
        var seed=new (string Name,string Category,int Score,string? Code,string Region)[]{
            ("alpha","red",10,"A-1","north"),("bravo","red",20,null,"south"),("charlie","blue",30,"C-3","north"),
            ("delta","blue",40,"D-4","south"),("echo","green",50,"E-5","north"),("foxtrot","green",60,null,"south")};
        var rows=new List<CriteriaHolon>();
        foreach(var item in seed.Append((Name:"golf",Category:"red",Score:70,Code:(string?)"G-7",Region:"north")))
        {
            var row=new CriteriaHolon{Id=Guid.NewGuid(),Name=item.Name,Category=item.Category,Score=item.Score,Code=item.Code,
                Batch=item.Name=="golf"?batch+"-hidden":batch,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);rows.Add(row);Assert.IsFalse(writer.Add(row).IsError);
            Assert.IsFalse(writer.ShadowProperty<string>(row,"Region").Result.SetCurrentValue(item.Region).IsError);
        }
        var created=await Timed("criteria-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);

        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        IQueryable<CriteriaHolon> Linq()=>reader.Linq<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).Result;
        CosmicCriteria<CriteriaHolon> Criteria()=>reader.CreateCriteria<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking);
        static string Names(IEnumerable<CriteriaHolon> found)=>string.Join(",",found.Select(row=>row.Name).OrderBy(name=>name,StringComparer.Ordinal));
        async Task Same(string label,ICriterion criterion,System.Linq.Expressions.Expression<Func<CriteriaHolon,bool>> linq,string expected)
        {
            var viaCriteria=await Timed("criteria-"+label,()=>Criteria().Add(criterion).ListAsync());
            Assert.IsFalse(viaCriteria.IsError,label+": "+viaCriteria.Message+viaCriteria.Exception);
            var viaLinq=await Timed("linq-"+label,()=>Linq().Where(linq).ToCosmicListAsync());
            Assert.IsFalse(viaLinq.IsError,label+": "+viaLinq.Message+viaLinq.Exception);
            Assert.AreEqual(Names(viaLinq.Result),Names(viaCriteria.Result),label);
            Assert.AreEqual(expected,Names(viaCriteria.Result),label);
        }

        // 1. Each restriction family equals its LINQ query on this store.
        await Same("eq",Restrictions.Eq("Category","red"),row=>row.Category=="red","alpha,bravo");
        await Same("ne",Restrictions.Ne("Category","red"),row=>row.Category!="red","charlie,delta,echo,foxtrot");
        await Same("gt",Restrictions.Gt("Score",40),row=>row.Score>40,"echo,foxtrot");
        await Same("ge",Restrictions.Ge("Score",40),row=>row.Score>=40,"delta,echo,foxtrot");
        await Same("lt",Restrictions.Lt("Score",20),row=>row.Score<20,"alpha");
        await Same("le",Restrictions.Le("Score",20),row=>row.Score<=20,"alpha,bravo");
        await Same("between",Restrictions.Between("Score",20,40),row=>row.Score>=20 && row.Score<=40,"bravo,charlie,delta");
        await Same("in",Restrictions.In("Category","blue","green"),row=>new[]{"blue","green"}.Contains(row.Category),"charlie,delta,echo,foxtrot");
        await Same("isnull",Restrictions.IsNull("Code"),row=>row.Code==null,"bravo,foxtrot");
        await Same("isnotnull",Restrictions.IsNotNull("Code"),row=>row.Code!=null,"alpha,charlie,delta,echo");
        await Same("startswith",Restrictions.StartsWith("Name","ch"),row=>row.Name.StartsWith("ch",StringComparison.Ordinal),"charlie");
        await Same("endswith",Restrictions.EndsWith("Name","o"),row=>row.Name.EndsWith("o",StringComparison.Ordinal),"bravo,echo");
        await Same("contains",Restrictions.Contains("Code","-"),row=>row.Code!.Contains("-"),"alpha,charlie,delta,echo");
        checks++;

        // 2. And/Or/Not nesting and explicit junctions.
        await Same("nested",Restrictions.Or(Restrictions.And(Restrictions.Eq("Category","red"),Restrictions.Not(Restrictions.IsNull("Code"))),
                Restrictions.Conjunction().Add(Restrictions.Eq("Category","green")).Add(Restrictions.Gt("Score",55))),
            row=>row.Category=="red" && !(row.Code==null) || row.Category=="green" && row.Score>55,"alpha,foxtrot");
        await Same("disjunction",Restrictions.Not(Restrictions.Disjunction().Add(Restrictions.Eq("Category","red")).Add(Restrictions.Eq("Category","blue"))),
            row=>!(row.Category=="red" || row.Category=="blue"),"echo,foxtrot");
        checks++;

        // 3. Ordering plus paging equals the LINQ window.
        var page=await Timed("criteria-page",()=>Criteria().AddOrder(Order.Asc("Category")).AddOrder(Order.Desc("Score"))
            .SetFirstResult(1).SetMaxResults(3).ListAsync());
        Assert.IsFalse(page.IsError,page.Message+page.Exception);
        var linqPage=await Timed("linq-page",()=>Linq().OrderBy(row=>row.Category).ThenByDescending(row=>row.Score).Skip(1).Take(3).ToCosmicListAsync());
        Assert.IsFalse(linqPage.IsError,linqPage.Message);
        CollectionAssert.AreEqual(linqPage.Result.Select(row=>row.Name).ToArray(),page.Result.Select(row=>row.Name).ToArray());
        CollectionAssert.AreEqual(new[]{"charlie","foxtrot","echo"},page.Result.Select(row=>row.Name).ToArray());
        checks++;

        // 4. Projections and aggregates map onto the native projection/aggregate terminals.
        var names=await Timed("criteria-property",()=>Criteria().Add(Restrictions.Eq("Category","blue")).AddOrder(Order.Asc("Name"))
            .SetProjection(Projections.Property("Name")).ListAsync<string>());
        Assert.IsFalse(names.IsError,names.Message+names.Exception);
        CollectionAssert.AreEqual(new[]{"charlie","delta"},names.Result.ToArray());
        var categories=await Timed("criteria-distinct",()=>Criteria().SetProjection(Projections.Distinct(Projections.Property("Category"))).ListAsync<string>());
        Assert.IsFalse(categories.IsError,categories.Message+categories.Exception);
        var linqCategories=await Linq().Select(row=>row.Category).Distinct().ToCosmicListAsync();
        Assert.IsFalse(linqCategories.IsError,linqCategories.Message);
        CollectionAssert.AreEquivalent(linqCategories.Result.ToArray(),categories.Result.ToArray());
        CollectionAssert.AreEquivalent(new[]{"red","blue","green"},categories.Result.ToArray());
        var count=await Timed("criteria-rowcount",()=>Criteria().Add(Restrictions.Ge("Score",30)).SetProjection(Projections.RowCount()).UniqueResultAsync<int>());
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(4,count.Result);
        var sum=await Timed("criteria-sum",()=>Criteria().SetProjection(Projections.Sum("Score")).UniqueResultAsync<int>());
        Assert.IsFalse(sum.IsError,sum.Message+sum.Exception);Assert.AreEqual(210,sum.Result);
        var linqSum=await Linq().ToCosmicSumAsync(row=>row.Score);Assert.AreEqual(linqSum.Result,sum.Result);
        var min=await Timed("criteria-min",()=>Criteria().Add(Restrictions.Eq("Category","green")).SetProjection(Projections.Min("Score")).UniqueResultAsync<int>());
        Assert.IsFalse(min.IsError,min.Message+min.Exception);Assert.AreEqual(50,min.Result);
        var maxValue=await Timed("criteria-max",()=>Criteria().SetProjection(Projections.Max("Score")).UniqueResultAsync<int>());
        Assert.IsFalse(maxValue.IsError,maxValue.Message+maxValue.Exception);Assert.AreEqual(60,maxValue.Result);
        var avg=await Timed("criteria-avg",()=>Criteria().Add(Restrictions.Eq("Category","red")).SetProjection(Projections.Avg("Score")).UniqueResultAsync<double>());
        Assert.IsFalse(avg.IsError,avg.Message+avg.Exception);Assert.AreEqual(15d,avg.Result);
        var wrongType=await Criteria().SetProjection(Projections.Sum("Score")).UniqueResultAsync<string>();
        Assert.IsTrue(wrongType.IsError);Assert.AreEqual("CQL_CRITERIA_RESULT_TYPE",wrongType.ErrorCode);
        checks++;

        // 5. UniqueResult: one row, none, and more than one refused.
        var unique=await Timed("criteria-unique",()=>Criteria().Add(Restrictions.Eq("Name","delta")).UniqueResultAsync());
        Assert.IsFalse(unique.IsError,unique.Message);Assert.AreEqual(40,unique.Result.Score);
        var none=await Criteria().Add(Restrictions.Eq("Name","zulu")).UniqueResultAsync();
        Assert.IsFalse(none.IsError,none.Message);Assert.IsNull(none.Result);
        var many=await Timed("criteria-not-unique",()=>Criteria().Add(Restrictions.Eq("Category","red")).UniqueResultAsync());
        Assert.IsTrue(many.IsError);Assert.AreEqual("CQL_CRITERIA_NOT_UNIQUE",many.ErrorCode);
        checks++;

        // 6. Typed QueryOver composes onto the same native root.
        var over=await Timed("queryover",()=>reader.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking)
            .Where(row=>row.Score>10).And(row=>row.Category!="green").OrderBy(row=>row.Score).Desc.Take(2).ListAsync());
        Assert.IsFalse(over.IsError,over.Message+over.Exception);
        CollectionAssert.AreEqual(new[]{"delta","charlie"},over.Result.Select(row=>row.Name).ToArray());
        var overMixed=await reader.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking)
            .Where(Restrictions.Eq("crit_category","blue")).OrderBy(row=>row.Name).Asc.Skip(1).SelectListAsync(row=>row.Code);
        Assert.IsFalse(overMixed.IsError,overMixed.Message+overMixed.Exception);
        CollectionAssert.AreEqual(new[]{"D-4"},overMixed.Result.ToArray());
        var overCount=await reader.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).Where(row=>row.Code==null).RowCountAsync();
        Assert.IsFalse(overCount.IsError,overCount.Message);Assert.AreEqual(2,overCount.Result);
        var overMany=await reader.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).SingleOrDefaultAsync();
        Assert.AreEqual("CQL_CRITERIA_NOT_UNIQUE",overMany.ErrorCode);
        checks++;

        // 7. String names resolve through model metadata keys and shadow properties.
        await Same("metadata-key",Restrictions.Eq("crit_score",30),row=>row.Score==30,"charlie");
        await Same("shadow-name",Restrictions.Eq("Region","north"),row=>CosmicShadow.Value<string>(row,"Region")=="north","alpha,charlie,echo");
        await Same("shadow-key",Restrictions.Eq("crit_region","south"),row=>CosmicShadow.Value<string>(row,"Region")=="south","bravo,delta,foxtrot");
        var keyOrder=await Criteria().AddOrder(Order.Desc("crit_score")).SetMaxResults(1).ListAsync();
        Assert.IsFalse(keyOrder.IsError,keyOrder.Message);Assert.AreEqual("foxtrot",keyOrder.Result[0].Name);
        checks++;

        // 8. Unknown properties and untranslatable criteria refuse before any read.
        var unknown=await Criteria().Add(Restrictions.Eq("NoSuchProperty",1)).ListAsync();
        Assert.IsTrue(unknown.IsError);Assert.AreEqual("CQL_CRITERIA_PROPERTY_UNKNOWN",unknown.ErrorCode);
        var unknownOrder=await Criteria().AddOrder(Order.Asc("missing")).ListAsync();
        Assert.AreEqual("CQL_CRITERIA_PROPERTY_UNKNOWN",unknownOrder.ErrorCode);
        var unknownProjection=await Criteria().SetProjection(Projections.Sum("missing")).UniqueResultAsync<int>();
        Assert.AreEqual("CQL_CRITERIA_PROPERTY_UNKNOWN",unknownProjection.ErrorCode);
        var badValue=await Criteria().Add(Restrictions.Eq("Score","thirty")).ListAsync();
        Assert.AreEqual("CQL_CRITERIA_VALUE_INVALID",badValue.ErrorCode);
        var lossy=await Criteria().Add(Restrictions.Eq("Score",2.5)).ListAsync();
        Assert.AreEqual("CQL_CRITERIA_VALUE_INVALID",lossy.ErrorCode);
        var orderedText=await Criteria().Add(Restrictions.Gt("Name","m")).ListAsync();
        Assert.AreEqual("CQL_CRITERIA_UNSUPPORTED",orderedText.ErrorCode);
        var overUnknown=await reader.QueryOver<CriteriaHolon>().Where(Restrictions.IsNull("nope")).ListAsync();
        Assert.AreEqual("CQL_CRITERIA_PROPERTY_UNKNOWN",overUnknown.ErrorCode);
        checks++;

        // 9. Caller visibility and model query filters are those of Linq<T>().
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var foreignRows=await Timed("criteria-foreign",()=>foreign.CreateCriteria<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true)
            .Add(Restrictions.StartsWith("Batch",batch)).ListAsync());
        Assert.IsFalse(foreignRows.IsError,foreignRows.Message);Assert.AreEqual(0,foreignRows.Result.Count);
        var foreignOver=await foreign.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).RowCountAsync();
        Assert.IsFalse(foreignOver.IsError,foreignOver.Message);Assert.AreEqual(0,foreignOver.Result);
        var filtered=await Criteria().Add(Restrictions.Eq("Name","golf")).ListAsync();
        Assert.IsFalse(filtered.IsError,filtered.Message);Assert.AreEqual(0,filtered.Result.Count);
        // Model filters bind with the model (as for Linq<T>()); bypass is a model without filters.
        using var unfilteredReader=CosmicSession.Create(manager,adapter,access,model.WithoutQueryFilters()).Result;
        var unfiltered=await Timed("criteria-model-filter-bypass",()=>unfilteredReader.CreateCriteria<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking)
            .Add(Restrictions.StartsWith("Batch",batch)).SetProjection(Projections.RowCount()).UniqueResultAsync<int>());
        Assert.IsFalse(unfiltered.IsError,unfiltered.Message+unfiltered.Exception);Assert.AreEqual(7,unfiltered.Result);
        var overFiltered=await reader.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).RowCountAsync();
        Assert.AreEqual(6,overFiltered.Result);
        // Session filters apply too, and ignoreQueryFilters bypasses only them, exactly as Linq<T>(..., ignoreQueryFilters).
        Assert.IsFalse(reader.HasQueryFilter<CriteriaHolon>(row=>row.Score>=30).IsError);
        var sessionFiltered=await reader.CreateCriteria<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).SetProjection(Projections.RowCount()).UniqueResultAsync<int>();
        Assert.IsFalse(sessionFiltered.IsError,sessionFiltered.Message);Assert.AreEqual(4,sessionFiltered.Result);
        var linqSessionFiltered=await reader.Linq<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).Result.ToCosmicCountAsync();
        Assert.AreEqual(linqSessionFiltered.Result,sessionFiltered.Result);
        var sessionBypass=await reader.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true).RowCountAsync();
        Assert.IsFalse(sessionBypass.IsError,sessionBypass.Message);Assert.AreEqual(6,sessionBypass.Result);
        checks++;

        // 10. Tracking mode: tracked criteria/QueryOver rows share the session identity; untracked rows do not.
        using var tracker=CosmicSession.Create(manager,adapter,access,model).Result;
        var trackedA=await Timed("criteria-tracked",()=>tracker.CreateCriteria<CriteriaHolon>().Add(Restrictions.Eq("Name","echo")).UniqueResultAsync());
        Assert.IsFalse(trackedA.IsError,trackedA.Message+trackedA.Exception);
        var trackedB=await tracker.QueryOver<CriteriaHolon>().Where(row=>row.Name=="echo").SingleOrDefaultAsync();
        Assert.IsFalse(trackedB.IsError,trackedB.Message);
        Assert.AreSame(trackedA.Result,trackedB.Result);
        var detached=await tracker.CreateCriteria<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).Add(Restrictions.Eq("Name","echo")).UniqueResultAsync();
        Assert.IsFalse(detached.IsError,detached.Message);
        Assert.AreNotSame(trackedA.Result,detached.Result);Assert.AreEqual(trackedA.Result.Id,detached.Result.Id);
        var detachedOver=await tracker.QueryOver<CriteriaHolon>(CosmicQueryTrackingMode.NoTracking).Where(row=>row.Name=="echo").SingleOrDefaultAsync();
        Assert.AreNotSame(trackedA.Result,detachedOver.Result);
        checks++;

        return new NativeFixtureJourneyProof(checks,1,new Dictionary<string,bool>{
            ["restrictionFamiliesEqualLinq"]=true,["andOrNotNesting"]=true,["orderingAndPaging"]=true,
            ["projectionsAndAggregates"]=true,["uniqueResultRefusesMany"]=true,["typedQueryOver"]=true,
            ["metadataKeyAndShadowNames"]=true,["unknownPropertyRefused"]=true,
            ["callerVisibilityAndModelFilters"]=true,["trackedVersusUntracked"]=true});
    }
}
