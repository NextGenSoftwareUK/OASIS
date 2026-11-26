using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // Q47: correlated child subqueries (Count/Sum/Max/Min/All/filtered Any) over a root's direct children.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeCorrelatedChildSubqueriesMatchAcrossStores(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyCorrelatedSubqueryJourney,nativeJourneySuffix:"-correlated-subquery-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyCorrelatedSubqueryJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var foreignActor=Guid.NewGuid();
        var run="q47-"+Guid.NewGuid().ToString("N");
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        using var session=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result;
        Holon New(string name,Guid owner,Guid? parent=null,long? score=null,bool root=false)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=owner,IsPublic=false,IsActive=true,IsNewHolon=true};
            if(parent.HasValue)row.ParentHolonId=parent.Value;
            if(score.HasValue)row.MetaData["q47score"]=score.Value;
            if(root)row.MetaData["q47run"]=run;
            owned.Add(row.Id);return row;
        }
        async Task Save(CosmicSession writer,string name,params Holon[] rows)
        {
            foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
            var saved=await Timed(name,()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        }
        var a=New("A",actor,root:true);var b=New("B",actor,root:true);var c=New("C",actor,root:true);var d=New("D",actor,root:true);
        await Save(session,"correlated-create-roots",a,b,c,d);
        await Save(session,"correlated-create-children",
            New("a1",actor,a.Id,5),New("a2",actor,a.Id,7),New("a3",actor,a.Id,1),
            New("b1",actor,b.Id,9),New("d1",actor,d.Id),New("d2",actor,d.Id,3));
        // A private child of A owned by another actor: never visible to the caller, so never counted.
        await Save(foreign,"correlated-create-hidden-child",New("a-hidden",foreignActor,a.Id,100));

        var checks=0;
        CosmicQuery<Holon> Roots()=>session.Query<Holon>().Result.Where(row=>CosmicQueryMetadata.Value<string>(row,"q47run")==run).Result;
        async Task<string> Names(string name,Expression<Func<Holon,bool>> predicate,Func<CosmicQuery<Holon>,OASISResult<CosmicQuery<Holon>>>? shape=null)
        {
            var query=Roots().Where(predicate);
            Assert.IsFalse(query.IsError,name+": "+query.Message);
            var shaped=shape==null?query:shape(query.Result);
            Assert.IsFalse(shaped.IsError,name+": "+shaped.Message);
            var rows=await Timed("correlated-"+name,()=>session.ReadAsync(shaped.Result,trackResults:false));
            Assert.IsFalse(rows.IsError,name+": "+rows.Message+rows.Exception);
            var names=rows.Result.Select(row=>row.Name).ToList();
            return string.Join(",",shape==null?names.OrderBy(value=>value,StringComparer.Ordinal):names);
        }
        async Task Expect(string name,Expression<Func<Holon,bool>> predicate,string expected,Func<CosmicQuery<Holon>,OASISResult<CosmicQuery<Holon>>>? shape=null)
        {Assert.AreEqual(expected,await Names(name,predicate,shape),name);checks++;}

        // 1. Count of matching children compared with a constant; the hidden foreign child never counts.
        await Expect("count-filtered-ge",row=>row.Children.Count(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4)>=2,"A");
        await Expect("count-filtered-hidden-excluded",row=>row.Children.Count(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4)==3,"");
        await Expect("count-all-visible",row=>row.Children.Count()==3,"A");
        await Expect("count-property",row=>row.Children.Count>=1,"A,B,D");
        await Expect("long-count-where",row=>row.Children.Where(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4).LongCount()>=1L,"A,B");
        await Expect("count-reversed-operands",row=>2<=row.Children.Count(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4),"A");
        // 2. Empty children: Count is 0, All is vacuously true, Sum is 0, Max/Min have no value (every comparison false).
        await Expect("count-empty",row=>row.Children.Count()==0,"C");
        await Expect("all-vacuous-and-missing-value",row=>row.Children.All(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>=3),"B,C");
        await Expect("sum-empty-zero",row=>row.Children.Sum(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))==0,"C");
        await Expect("max-empty-no-value",row=>row.Children.Max(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))<5,"D");
        await Expect("max-not-equal-empty-excluded",row=>row.Children.Max(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))!=7,"B,D");
        // 3. Scalar aggregates over visible children only (hidden score 100 would otherwise win).
        await Expect("max-gt",row=>row.Children.Max(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))>8,"B");
        await Expect("max-ge",row=>row.Children.Max(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))>=7,"A,B");
        await Expect("max-eq",row=>row.Children.Max(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))==7,"A");
        await Expect("min-le",row=>row.Children.Min(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))<=1,"A");
        await Expect("min-gt",row=>row.Children.Min(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))>2,"B,D");
        await Expect("sum-eq-visible",row=>row.Children.Sum(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))==13,"A");
        await Expect("sum-hidden-excluded",row=>row.Children.Sum(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))>100,"");
        await Expect("sum-filtered",row=>row.Children.Where(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4).Sum(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))==12,"A");
        await Expect("any-filtered",row=>row.Children.Where(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4).Any(),"A,B");
        await Expect("all-filtered",row=>row.Children.Where(child=>child.Name!="a3").All(child=>CosmicQueryMetadata.Value<long>(child,"q47score")>4),"A,B,C");
        // 4. Combination with ordinary predicates, Boolean logic, ordering and paging.
        await Expect("combined-or",row=>row.Children.Count()==0||row.Children.Sum(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))>12,"A,C");
        await Expect("combined-not",row=>!(row.Children.Count()>=1),"C");
        await Expect("combined-ordinary-paged",row=>row.Name!="B"&&row.Children.Count()>=1,"D",
            query=>query.OrderBy(row=>row.Name).Result.Skip(1).Result.Take(1));
        await Expect("combined-exists",row=>row.Children.Any(child=>child.Name=="b1")||row.Children.Min(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))==3,"B,D");

        // 5. Deeper nesting and unsupported shapes are refused with errors; nothing is evaluated client-side.
        async Task Refused(string name,Func<OASISResult<CosmicQuery<Holon>>> compose,bool atAdmission=false)
        {
            var composed=compose();
            if(!atAdmission)
            {
                Assert.IsTrue(composed.IsError,name+" must be refused by the expression compiler.");
                StringAssert.StartsWith(composed.ErrorCode,"CQL_EXPRESSION_",name);
            }
            else
            {
                Assert.IsFalse(composed.IsError,name+": "+composed.Message);
                var read=await Timed("correlated-refused-"+name,()=>session.ReadAsync(composed.Result,trackResults:false));
                Assert.IsTrue(read.IsError,name+" must be refused before native execution.");
                StringAssert.Contains(read.Message,"one correlation level",name);
            }
            checks++;
        }
        await Refused("nested-count",()=>Roots().Where(row=>row.Children.Count(child=>child.Children.Count()>0)>0));
        await Refused("nested-in-any",()=>Roots().Where(row=>row.Children.Any(child=>child.Children.Count()>1)));
        await Refused("nested-via-where-any-child",()=>Roots().WhereAnyChild(child=>child.Children.Count()>1),atAdmission:true);
        await Refused("nested-all",()=>Roots().Where(row=>row.Children.All(child=>child.Children.Any())));
        await Refused("canonical-sum",()=>Roots().Where(row=>row.Children.Sum(child=>child.Version)>1));
        await Refused("double-max",()=>Roots().Where(row=>row.Children.Max(child=>CosmicQueryMetadata.Value<double>(child,"q47score"))>1d));
        await Refused("field-bound",()=>Roots().Where(row=>row.Children.Count()>row.Version));
        await Refused("aggregate-vs-aggregate",()=>Roots().Where(row=>row.Children.Count()>row.Children.Sum(child=>CosmicQueryMetadata.Value<long>(child,"q47score"))));

        return new NativeFixtureJourneyProof(checks,3,new Dictionary<string,bool>{
            ["childCountComparedWithConstant"]=true,["hiddenForeignChildNeverCounted"]=true,
            ["emptyChildrenCountZeroAllVacuousExtremaNoValue"]=true,["maxMinSumOverVisibleChildren"]=true,
            ["filteredAnyAllCountSum"]=true,["combinedWithOrdinaryPredicatesAndPaging"]=true,
            ["nestedAndUnsupportedShapesRefused"]=true});
    }
}
