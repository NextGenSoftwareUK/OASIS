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
    public sealed class HqlHolon:Holon
    {
        public string? Batch {get;set;}
        public string? Category {get;set;}
        public int Score {get;set;}
        public string? Code {get;set;}
    }

    // Q60: bounded NHibernate HQL parsed onto the Q61 criteria objects over the session's native LINQ/CQL root.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeHqlMatchesLinq(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyHqlJourney,nativeJourneySuffix:"-hql-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyHqlJourney(
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
            try{return await action();}finally{timings[name]=clock.ElapsedMilliseconds;}
        }
        var checks=0;
        var batch="hql-"+Guid.NewGuid().ToString("N");

        var builder=new CosmicModelBuilder();
        var entity=builder.Entity<HqlHolon>();
        entity.Property(row=>row.Batch).HasMetadataKey("hql_batch");
        entity.Property(row=>row.Category).HasMetadataKey("hql_category");
        entity.Property(row=>row.Score).HasMetadataKey("hql_score");
        entity.Property(row=>row.Code).HasMetadataKey("hql_code");
        entity.ShadowProperty<string>("Region").HasMetadataKey("hql_region");
        var writable=builder.Build();Assert.IsFalse(writable.IsError,writable.Message);
        entity.HasQueryFilter(row=>row.Batch==batch);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;

        using var writer=CosmicSession.Create(manager,adapter,access,writable.Result).Result;
        var seed=new (string Name,string Category,int Score,string? Code,string Region)[]{
            ("alpha","red",10,"A-1","north"),("bravo","red",20,null,"south"),("charlie","blue",30,"C-3","north"),
            ("delta","blue",40,"D-4","south"),("echo","green",50,"E-5","north"),("foxtrot","green",60,null,"south"),
            ("golf","red",70,"G-7","north")};
        foreach(var item in seed)
        {
            var row=new HqlHolon{Id=Guid.NewGuid(),Name=item.Name,Category=item.Category,Score=item.Score,Code=item.Code,
                Batch=item.Name=="golf"?batch+"-hidden":batch,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);Assert.IsFalse(writer.Add(row).IsError);
            Assert.IsFalse(writer.ShadowProperty<string>(row,"Region").Result.SetCurrentValue(item.Region).IsError);
        }
        var created=await Timed("hql-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);

        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        IQueryable<HqlHolon> Linq()=>reader.Linq<HqlHolon>(CosmicQueryTrackingMode.NoTracking).Result;
        CosmicHqlQuery<HqlHolon> Query(string hql)=>reader.CreateQuery<HqlHolon>(hql,CosmicQueryTrackingMode.NoTracking);
        static string Names(IEnumerable<HqlHolon> found)=>string.Join(",",found.Select(row=>row.Name).OrderBy(name=>name,StringComparer.Ordinal));
        async Task Same(string label,string where,System.Linq.Expressions.Expression<Func<HqlHolon,bool>> linq,string expected,
            params (string Name,object Value)[] parameters)
        {
            var query=Query("from HqlHolon e where "+where);
            foreach(var (name,value) in parameters)query.SetParameter(name,value);
            var viaHql=await Timed("hql-"+label,()=>query.ListAsync());
            Assert.IsFalse(viaHql.IsError,label+": "+viaHql.ErrorCode+" "+viaHql.Message+viaHql.Exception);
            var viaLinq=await Timed("linq-"+label,()=>Linq().Where(linq).ToCosmicListAsync());
            Assert.IsFalse(viaLinq.IsError,label+": "+viaLinq.Message+viaLinq.Exception);
            Assert.AreEqual(Names(viaLinq.Result),Names(viaHql.Result),label);
            Assert.AreEqual(expected,Names(viaHql.Result),label);
        }
        async Task Refused(string hql,string code,int position,Action<CosmicHqlQuery<HqlHolon>>? bind=null)
        {
            var query=Query(hql);bind?.Invoke(query);
            var result=await query.ListAsync();
            Assert.IsTrue(result.IsError,hql);
            Assert.AreEqual(code,result.ErrorCode,hql+": "+result.Message);
            var cause=result.Exception as CosmicHqlException;
            Assert.IsNotNull(cause,hql+": "+result.Exception);
            Assert.AreEqual(position,cause.Position,hql+": "+result.Message);
        }

        // 1. Each WHERE clause equals its LINQ query on this store.
        await Same("eq","e.Category = 'red'",row=>row.Category=="red","alpha,bravo");
        await Same("ne","e.Category <> 'red'",row=>row.Category!="red","charlie,delta,echo,foxtrot");
        await Same("ne-bang","e.Category != 'red'",row=>row.Category!="red","charlie,delta,echo,foxtrot");
        await Same("gt","e.Score > 40",row=>row.Score>40,"echo,foxtrot");
        await Same("ge","e.Score >= 40",row=>row.Score>=40,"delta,echo,foxtrot");
        await Same("lt","e.Score < 20",row=>row.Score<20,"alpha");
        await Same("le","e.Score <= 20",row=>row.Score<=20,"alpha,bravo");
        await Same("flipped","20 >= e.Score",row=>row.Score<=20,"alpha,bravo");
        await Same("between","e.Score between 20 and 40",row=>row.Score>=20 && row.Score<=40,"bravo,charlie,delta");
        await Same("not-between","e.Score not between 20 and 40",row=>!(row.Score>=20 && row.Score<=40),"alpha,echo,foxtrot");
        await Same("in","e.Category in ('blue','green')",row=>new[]{"blue","green"}.Contains(row.Category),"charlie,delta,echo,foxtrot");
        await Same("not-in","e.Category not in ('blue','green')",row=>!new[]{"blue","green"}.Contains(row.Category),"alpha,bravo");
        await Same("isnull","e.Code is null",row=>row.Code==null,"bravo,foxtrot");
        await Same("isnotnull","e.Code is not null",row=>row.Code!=null,"alpha,charlie,delta,echo");
        await Same("like-prefix","e.Name like 'ch%'",row=>row.Name.StartsWith("ch",StringComparison.Ordinal),"charlie");
        await Same("like-suffix","e.Name like '%o'",row=>row.Name.EndsWith("o",StringComparison.Ordinal),"bravo,echo");
        await Same("like-contains","e.Code like '%-%'",row=>row.Code!.Contains("-"),"alpha,charlie,delta,echo");
        await Same("not-like","not (e.Name like 'ch%')",row=>!row.Name.StartsWith("ch",StringComparison.Ordinal),"alpha,bravo,delta,echo,foxtrot");
        await Same("unqualified","Score = 30",row=>row.Score==30,"charlie");
        checks++;

        // 2. AND/OR/NOT precedence and parentheses.
        await Same("nested","e.Category = 'red' and not e.Code is null or (e.Category = 'green' and e.Score > 55)",
            row=>row.Category=="red" && !(row.Code==null) || row.Category=="green" && row.Score>55,"alpha,foxtrot");
        await Same("not-or","not (e.Category = 'red' or e.Category = 'blue')",row=>!(row.Category=="red" || row.Category=="blue"),"echo,foxtrot");
        var aliasAs=await Query("FROM HqlHolon AS h WHERE h.Score = 10").ListAsync();
        Assert.IsFalse(aliasAs.IsError,aliasAs.Message);Assert.AreEqual("alpha",Names(aliasAs.Result));
        checks++;

        // 3. Named parameters are typed data; an injection-looking string matches nothing and changes no plan.
        await Same("param-eq","e.Category = :category and e.Score >= :low",row=>row.Category=="blue" && row.Score>=35,"delta",
            ("category","blue"),("low",35));
        await Same("param-like","e.Name like :prefix",row=>row.Name.StartsWith("ec",StringComparison.Ordinal),"echo",("prefix","ec%"));
        await Same("param-between","e.Score between :a and :b",row=>row.Score>=10 && row.Score<=20,"alpha,bravo",("a",10),("b",20));
        var listParam=await Query("from HqlHolon e where e.Name in (:names)").SetParameterList("names",new[]{"alpha","echo"}).ListAsync();
        Assert.IsFalse(listParam.IsError,listParam.Message);Assert.AreEqual("alpha,echo",Names(listParam.Result));
        var injection="red' or '1'='1";
        await Same("param-injection","e.Category = :category",row=>row.Category==injection,"",("category",injection));
        var injectionLike=await Query("from HqlHolon e where e.Name like :p").SetParameter("p","%' or 1=1 --%").ListAsync();
        Assert.IsFalse(injectionLike.IsError,injectionLike.Message);Assert.AreEqual(0,injectionLike.Result.Count);
        await Refused("from HqlHolon e where e.Score = :missing","CQL_HQL_PARAMETER_MISSING",32);
        await Refused("from HqlHolon e where e.Code = :code","CQL_HQL_PARAMETER_NULL",31,query=>query.SetParameter<string?>("code",null));
        await Refused("from HqlHolon e where e.Score = :s","CQL_HQL_VALUE_INVALID",22,query=>query.SetParameter("s","thirty"));
        await Refused("from HqlHolon e where e.Score = :s","CQL_HQL_PARAMETER_UNKNOWN",-1,query=>query.SetParameter("s",1).SetParameter("extra",2));
        checks++;

        // 4. ORDER BY plus SetFirstResult/SetMaxResults equals the LINQ window.
        var page=await Timed("hql-page",()=>Query("from HqlHolon e order by e.Category asc, e.Score desc").SetFirstResult(1).SetMaxResults(3).ListAsync());
        Assert.IsFalse(page.IsError,page.Message+page.Exception);
        var linqPage=await Timed("linq-page",()=>Linq().OrderBy(row=>row.Category).ThenByDescending(row=>row.Score).Skip(1).Take(3).ToCosmicListAsync());
        Assert.IsFalse(linqPage.IsError,linqPage.Message);
        CollectionAssert.AreEqual(linqPage.Result.Select(row=>row.Name).ToArray(),page.Result.Select(row=>row.Name).ToArray());
        CollectionAssert.AreEqual(new[]{"charlie","foxtrot","echo"},page.Result.Select(row=>row.Name).ToArray());
        var top=await Query("select e from HqlHolon e where e.Score > :s order by e.Score desc").SetParameter("s",15).SetMaxResults(2).ListAsync();
        Assert.IsFalse(top.IsError,top.Message);CollectionAssert.AreEqual(new[]{"foxtrot","echo"},top.Result.Select(row=>row.Name).ToArray());
        checks++;

        // 5. Projections and aggregates map onto the native projection/aggregate terminals.
        var names=await Timed("hql-property",()=>Query("select e.Name from HqlHolon e where e.Category = 'blue' order by e.Name").ListAsync<string>());
        Assert.IsFalse(names.IsError,names.Message+names.Exception);
        CollectionAssert.AreEqual(new[]{"charlie","delta"},names.Result.ToArray());
        var categories=await Timed("hql-distinct",()=>Query("select distinct e.Category from HqlHolon e").ListAsync<string>());
        Assert.IsFalse(categories.IsError,categories.Message+categories.Exception);
        var linqCategories=await Linq().Select(row=>row.Category).Distinct().ToCosmicListAsync();
        CollectionAssert.AreEquivalent(linqCategories.Result.ToArray(),categories.Result.ToArray());
        CollectionAssert.AreEquivalent(new[]{"red","blue","green"},categories.Result.ToArray());
        var count=await Timed("hql-count",()=>Query("select count(*) from HqlHolon e where e.Score >= :s").SetParameter("s",30).UniqueResultAsync<long>());
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(4L,count.Result);
        var countAlias=await Query("select count(e) from HqlHolon e").UniqueResultAsync<int>();
        Assert.IsFalse(countAlias.IsError,countAlias.Message);Assert.AreEqual((await Linq().ToCosmicCountAsync()).Result,countAlias.Result);
        var sum=await Timed("hql-sum",()=>Query("select sum(e.Score) from HqlHolon e").UniqueResultAsync<int>());
        Assert.IsFalse(sum.IsError,sum.Message+sum.Exception);Assert.AreEqual((await Linq().ToCosmicSumAsync(row=>row.Score)).Result,sum.Result);
        Assert.AreEqual(210,sum.Result);
        var min=await Query("select min(e.Score) from HqlHolon e where e.Category = 'green'").UniqueResultAsync<int>();
        Assert.IsFalse(min.IsError,min.Message);Assert.AreEqual(50,min.Result);
        var maxValue=await Query("select max(e.Score) from HqlHolon e").UniqueResultAsync<int>();
        Assert.IsFalse(maxValue.IsError,maxValue.Message);Assert.AreEqual(60,maxValue.Result);
        var avg=await Query("select avg(e.Score) from HqlHolon e where e.Category = 'red'").UniqueResultAsync<double>();
        Assert.IsFalse(avg.IsError,avg.Message+avg.Exception);Assert.AreEqual(15d,avg.Result);
        var one=await Query("select e.Code from HqlHolon e where e.Name = 'delta'").UniqueResultAsync<string>();
        Assert.IsFalse(one.IsError,one.Message);Assert.AreEqual("D-4",one.Result);
        var wrongType=await Query("select sum(e.Score) from HqlHolon e").UniqueResultAsync<string>();
        Assert.AreEqual("CQL_HQL_RESULT_TYPE",wrongType.ErrorCode);
        var valuesAsEntities=await Query("select e.Name from HqlHolon e").ListAsync();
        Assert.AreEqual("CQL_HQL_RESULT_TYPE",valuesAsEntities.ErrorCode);
        var unique=await Query("from HqlHolon e where e.Name = :n").SetParameter("n","delta").UniqueResultAsync();
        Assert.IsFalse(unique.IsError,unique.Message);Assert.AreEqual(40,unique.Result.Score);
        var many=await Query("from HqlHolon e where e.Category = 'red'").UniqueResultAsync();
        Assert.AreEqual("CQL_HQL_NOT_UNIQUE",many.ErrorCode);
        checks++;

        // 6. Model metadata keys and shadow names/keys resolve exactly as in Q61.
        await Same("metadata-key","e.hql_score = 30",row=>row.Score==30,"charlie");
        await Same("shadow-name","e.Region = 'north'",row=>CosmicShadow.Value<string>(row,"Region")=="north","alpha,charlie,echo");
        await Same("shadow-key","e.hql_region = :r",row=>CosmicShadow.Value<string>(row,"Region")=="south","bravo,delta,foxtrot",("r","south"));
        var keyOrder=await Query("from HqlHolon e order by e.hql_score desc").SetMaxResults(1).ListAsync();
        Assert.IsFalse(keyOrder.IsError,keyOrder.Message);Assert.AreEqual("foxtrot",keyOrder.Result[0].Name);
        var keySum=await Query("select sum(e.hql_score) from HqlHolon e where e.hql_category = 'blue'").UniqueResultAsync<int>();
        Assert.IsFalse(keySum.IsError,keySum.Message);Assert.AreEqual(70,keySum.Result);
        checks++;

        // 7. Caller visibility, session filters and model filters are those of Linq<T>().
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var foreignRows=await Timed("hql-foreign",()=>foreign.CreateQuery<HqlHolon>("from HqlHolon e where e.Batch like :b",
            CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true).SetParameter("b",batch+"%").ListAsync());
        Assert.IsFalse(foreignRows.IsError,foreignRows.Message);Assert.AreEqual(0,foreignRows.Result.Count);
        var hidden=await Query("from HqlHolon e where e.Name = 'golf'").ListAsync();
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count);
        using var unfilteredReader=CosmicSession.Create(manager,adapter,access,model.WithoutQueryFilters()).Result;
        var unfiltered=await Timed("hql-model-filter-bypass",()=>unfilteredReader.CreateQuery<HqlHolon>(
            "select count(*) from HqlHolon e where e.Batch like :b",CosmicQueryTrackingMode.NoTracking).SetParameter("b",batch+"%").UniqueResultAsync<int>());
        Assert.IsFalse(unfiltered.IsError,unfiltered.Message+unfiltered.Exception);Assert.AreEqual(7,unfiltered.Result);
        Assert.IsFalse(reader.HasQueryFilter<HqlHolon>(row=>row.Score>=30).IsError);
        var sessionFiltered=await Query("select count(*) from HqlHolon e").UniqueResultAsync<int>();
        Assert.IsFalse(sessionFiltered.IsError,sessionFiltered.Message);Assert.AreEqual(4,sessionFiltered.Result);
        Assert.AreEqual((await Linq().ToCosmicCountAsync()).Result,sessionFiltered.Result);
        var sessionBypass=await reader.CreateQuery<HqlHolon>("select count(*) from HqlHolon e",CosmicQueryTrackingMode.NoTracking,100,
            ignoreQueryFilters:true).UniqueResultAsync<int>();
        Assert.IsFalse(sessionBypass.IsError,sessionBypass.Message);Assert.AreEqual(6,sessionBypass.Result);
        checks++;

        // 8. Tracking mode: tracked HQL rows share the session identity with LINQ; untracked rows do not.
        using var tracker=CosmicSession.Create(manager,adapter,access,model).Result;
        var trackedA=await Timed("hql-tracked",()=>tracker.CreateQuery<HqlHolon>("from HqlHolon e where e.Name = :n").SetParameter("n","echo").UniqueResultAsync());
        Assert.IsFalse(trackedA.IsError,trackedA.Message+trackedA.Exception);
        var trackedB=await tracker.Linq<HqlHolon>(CosmicQueryTrackingMode.Tracking).Result.Where(row=>row.Name=="echo").ToCosmicListAsync();
        Assert.IsFalse(trackedB.IsError,trackedB.Message);Assert.AreSame(trackedA.Result,trackedB.Result.Single());
        var trackedC=await tracker.CreateQuery<HqlHolon>("select e from HqlHolon e where e.Name = 'echo'").UniqueResultAsync<HqlHolon>();
        Assert.IsFalse(trackedC.IsError,trackedC.Message);Assert.AreSame(trackedA.Result,trackedC.Result);
        var detached=await tracker.CreateQuery<HqlHolon>("from HqlHolon e where e.Name = 'echo'",CosmicQueryTrackingMode.NoTracking).UniqueResultAsync();
        Assert.IsFalse(detached.IsError,detached.Message);
        Assert.AreNotSame(trackedA.Result,detached.Result);Assert.AreEqual(trackedA.Result.Id,detached.Result.Id);
        checks++;

        // 9. Every construct outside the subset refuses with a precise code and text position, before any read.
        await Refused("from HqlHolon e join e.Children c","CQL_HQL_UNSUPPORTED_JOIN",16);
        await Refused("from HqlHolon e left join fetch e.Children","CQL_HQL_UNSUPPORTED_JOIN",16);
        await Refused("from HqlHolon e, HqlHolon f","CQL_HQL_UNSUPPORTED_JOIN",15);
        await Refused("from HqlHolon e where e.Parent.Name = 'x'","CQL_HQL_UNSUPPORTED_JOIN",30);
        await Refused("from HqlHolon e where e.Score in (select f.Score from HqlHolon f)","CQL_HQL_UNSUPPORTED_SUBQUERY",34);
        await Refused("from HqlHolon e where exists (from HqlHolon f)","CQL_HQL_UNSUPPORTED_SUBQUERY",22);
        await Refused("from HqlHolon e group by e.Category","CQL_HQL_UNSUPPORTED_GROUPING",16);
        await Refused("select e.Category from HqlHolon e order by e.Category having count(*) > 1","CQL_HQL_UNSUPPORTED_GROUPING",54);
        await Refused("update HqlHolon set Score = 1","CQL_HQL_UNSUPPORTED_DML",0);
        await Refused("delete from HqlHolon e","CQL_HQL_UNSUPPORTED_DML",0);
        await Refused("from HqlHolon e where upper(e.Name) = 'ALPHA'","CQL_HQL_UNSUPPORTED_FUNCTION",22);
        await Refused("select lower(e.Name) from HqlHolon e","CQL_HQL_UNSUPPORTED_FUNCTION",7);
        await Refused("select count(e.Code) from HqlHolon e","CQL_HQL_UNSUPPORTED_FUNCTION",13);
        await Refused("from HqlHolon e where e.Score + 1 > 2","CQL_HQL_UNSUPPORTED_EXPRESSION",30);
        await Refused("from HqlHolon e where e.Score = e.Score","CQL_HQL_UNSUPPORTED_EXPRESSION",32);
        await Refused("select e.Name, e.Score from HqlHolon e","CQL_HQL_UNSUPPORTED_SELECT",13);
        await Refused("select new Foo(e.Name) from HqlHolon e","CQL_HQL_UNSUPPORTED_SELECT",7);
        await Refused("from HqlHolon e where e.Name like 'a_c%'","CQL_HQL_LIKE_PATTERN",34);
        await Refused("from HqlHolon e where e.Name like 'alpha'","CQL_HQL_LIKE_PATTERN",34);
        await Refused("from HqlHolon e where e.Name like :p","CQL_HQL_LIKE_PATTERN",34,query=>query.SetParameter("p","a%b%"));
        await Refused("from HqlHolon e where e.Score = ?","CQL_HQL_PARAMETER_POSITIONAL",32);
        await Refused("from CriteriaHolon e","CQL_HQL_ENTITY_UNKNOWN",5);
        await Refused("from HqlHolon e where x.Score = 1","CQL_HQL_ALIAS_UNKNOWN",22);
        await Refused("from HqlHolon e where e.NoSuchProperty = 1","CQL_HQL_PROPERTY_UNKNOWN",22);
        await Refused("from HqlHolon e order by e.missing","CQL_HQL_PROPERTY_UNKNOWN",25);
        await Refused("from HqlHolon e where e.Name > 'm'","CQL_HQL_UNSUPPORTED",22);
        await Refused("from HqlHolon e where e.Code = null","CQL_HQL_SYNTAX",31);
        await Refused("from HqlHolon e where e.Name = 'open","CQL_HQL_SYNTAX",31);
        await Refused("from HqlHolon e limit 5","CQL_HQL_UNSUPPORTED_PAGING",16);
        await Refused("from HqlHolon e where e.Score = 2.5","CQL_HQL_VALUE_INVALID",22);
        // An undeclared entity type is refused even when it matches T.
        using var bare=CosmicSession.Create(manager,adapter,access,new CosmicModelBuilder().Build().Result).Result;
        var undeclared=await bare.CreateQuery<HqlHolon>("from HqlHolon e").ListAsync();
        Assert.AreEqual("CQL_HQL_ENTITY_UNKNOWN",undeclared.ErrorCode);
        Assert.AreEqual(5,((CosmicHqlException)undeclared.Exception).Position);
        checks++;

        return new NativeFixtureJourneyProof(checks,1,new Dictionary<string,bool>{
            ["whereClausesEqualLinq"]=true,["andOrNotParentheses"]=true,["namedParametersAsData"]=true,
            ["orderingAndPaging"]=true,["projectionsAndAggregates"]=true,["metadataKeyAndShadowNames"]=true,
            ["callerVisibilityAndModelFilters"]=true,["trackedVersusUntracked"]=true,["unsupportedConstructsRefusedWithPosition"]=true});
    }
}
