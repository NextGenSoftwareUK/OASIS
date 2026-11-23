using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public class ConventionBaseHolon:Holon
    {
        [CustomOASISProperty(MetadataKey="fixed_caption")]
        public string Caption {get;set;}="";
    }
    public sealed class ConventionHolon:ConventionBaseHolon
    {
        public int HTTPStatus {get;set;}
        public int ReadingValue {get;set;}
    }
    public sealed class ConventionCollisionHolon:Holon
    {
        public static int GetterCalls;
        public int AValue {get{GetterCalls++;return 1;}set{}}
        public int A_Value {get{GetterCalls++;return 2;}set{}}
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeFluentConventionsShareWriteReadAndQueryNames(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyFluentConventionJourney,nativeJourneySuffix:"-fluent-conventions-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyFluentConventionJourney(
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
        var builder=new CosmicModelBuilder();
        builder.Entity<ConventionHolon>().UsePropertyNamingConvention(CosmicPropertyNamingConvention.SnakeCase)
            .Property(row=>row.ReadingValue).HasMetadataKey("explicit_reading");
        builder.Entity<ConventionHolon>().Property(row=>row.HTTPStatus).IsRequired();
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new ConventionHolon{Id=Guid.NewGuid(),Name="convention model",HTTPStatus=201,
            ReadingValue=7,Caption="before",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);Assert.IsFalse(writer.Add(entity).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.IsTrue(entity.MetaData.ContainsKey("http_status"));
        Assert.IsTrue(entity.MetaData.ContainsKey("explicit_reading"));
        Assert.IsTrue(entity.MetaData.ContainsKey("fixed_caption"),"Stored fixture keys: "+string.Join(",",entity.MetaData.Keys));
        Assert.IsFalse(entity.MetaData.ContainsKey("HTTPStatus"));
        Assert.IsFalse(entity.MetaData.ContainsKey("reading_value"));
        Assert.IsFalse(entity.MetaData.ContainsKey("caption"));
        builder.Entity<ConventionHolon>().UsePropertyNamingConvention(CosmicPropertyNamingConvention.PropertyName);
        var later=builder.Build();Assert.IsFalse(later.IsError,later.Message);
        using(var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result)
        {
            var q=reader.Query<ConventionHolon>().Result
                .Where(row=>row.Id==id && row.HTTPStatus==201 && row.ReadingValue==7 && row.Caption=="before").Result;
            var read=await Timed("read",()=>reader.ReadAsync(q,trackResults:false));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
            Assert.AreEqual(201,read.Result[0].HTTPStatus);Assert.AreEqual(7,read.Result[0].ReadingValue);
            Assert.AreEqual("before",read.Result[0].Caption);
        }
        entity.HTTPStatus=202;entity.ReadingValue=8;entity.Caption="after";
        var saved=await Timed("update",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        using(var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result)
        {
            var q=reader.Query<ConventionHolon>().Result
                .Where(row=>row.Id==id && row.HTTPStatus==202 && row.ReadingValue==8 && row.Caption=="after").Result;
            var read=await Timed("read-updated",()=>reader.ReadAsync(q,trackResults:false));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
            Assert.AreEqual(202,read.Result[0].HTTPStatus);Assert.AreEqual("after",read.Result[0].Caption);
        }
        ConventionCollisionHolon.GetterCalls=0;
        var collision=new CosmicModelBuilder();
        collision.Entity<ConventionCollisionHolon>().UsePropertyNamingConvention(CosmicPropertyNamingConvention.SnakeCase);
        Assert.IsTrue(collision.Build().IsError);Assert.AreEqual(0,ConventionCollisionHolon.GetterCalls);
        return new NativeFixtureJourneyProof(5,2,new Dictionary<string,bool>{
            ["conventionStoredAndQueried"]=true,["explicitAliasPrecedence"]=true,
            ["inheritedAnnotationPrecedence"]=true,["immutableConventionSnapshot"]=true,
            ["collisionBeforeGetters"]=true});
    }
}
