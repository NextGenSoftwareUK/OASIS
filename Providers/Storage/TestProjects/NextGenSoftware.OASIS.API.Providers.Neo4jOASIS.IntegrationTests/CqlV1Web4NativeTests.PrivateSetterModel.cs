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
    public sealed class PrivateSetterMappedHolon:Holon
    {
        public int Score {get;private set;}
        public string Label {get;private set;}="";
        public void Change(int score,string label){Score=score;Label=label;}
    }
    public sealed class GetterOnlyMappedHolon:Holon {public int Score=>1;}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeExplicitPrivateSettersHydrateTrackAndQuery(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyPrivateSetterModelJourney,nativeJourneySuffix:"-private-setter-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyPrivateSetterModelJourney(
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
        builder.Entity<PrivateSetterMappedHolon>().Property(row=>row.Score).HasMetadataKey("private_score");
        builder.Entity<PrivateSetterMappedHolon>().Property(row=>row.Label).IsRequired().HasMaxLength(8).HasMetadataKey("private_label");
        var model=builder.Build();Assert.IsFalse(model.IsError,model.Message);
        Assert.ThrowsException<ArgumentException>(()=>new CosmicModelBuilder()
            .Entity<GetterOnlyMappedHolon>().Property(row=>row.Score).HasMetadataKey("read_only_score"));
        var xmlBuilder=new CosmicModelBuilder();
        var imported=xmlBuilder.ImportNHibernateXml(
            "<hibernate-mapping xmlns=\"urn:nhibernate-mapping-2.2\"><class name=\"PrivateSetter\">"+
            "<property name=\"Score\" type=\"Int32\" column=\"private_score\"/>"+
            "<property name=\"Label\" type=\"String\" column=\"private_label\" not-null=\"true\" length=\"8\"/>"+
            "</class></hibernate-mapping>",
            new Dictionary<string,Type>{{"PrivateSetter",typeof(PrivateSetterMappedHolon)}});
        Assert.IsFalse(imported.IsError,imported.Message);
        var xmlModel=xmlBuilder.Build();Assert.IsFalse(xmlModel.IsError,xmlModel.Message);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}
        }
        using var writer=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var entity=new PrivateSetterMappedHolon{Id=Guid.NewGuid(),Name="private setters",
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        entity.Change(17,"first");var id=entity.Id;owned.Add(id);
        Assert.IsFalse(writer.Add(entity).IsError);
        var created=await Timed("create-valid",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var version=entity.Version;
        Assert.IsTrue(entity.MetaData.ContainsKey("private_score"));
        using var reader=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var query=reader.Query<PrivateSetterMappedHolon>().Result.Where(row=>row.Id==id&&row.Score==17).Result;
        var loaded=await Timed("read-fluent",()=>reader.ReadAsync(query));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.AreEqual(1,loaded.Result.Count);
        var current=loaded.Result.Single();Assert.AreEqual(17,current.Score);Assert.AreEqual("first",current.Label);
        Assert.AreNotSame(entity,current);
        var root=CosmicLinq.Create(manager,adapter,query).Result;
        var projected=await Timed("project-native",()=>root.Select(row=>new{row.Score,row.Label}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);
        Assert.AreEqual(17,projected.Result.Single().Score);Assert.AreEqual("first",projected.Result.Single().Label);
        current.Change(23,"second");
        Assert.AreEqual(CosmicEntityState.Modified,reader.State(current).Result);
        var saved=await Timed("save-update",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.AreEqual(version+1,current.Version);
        using var xmlReader=CosmicSession.Create(manager,adapter,access,xmlModel.Result).Result;
        var updated=await Timed("read-xml",()=>xmlReader.ReadAsync(
            xmlReader.Query<PrivateSetterMappedHolon>().Result.Where(row=>row.Id==id&&row.Score==23).Result));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.AreEqual(1,updated.Result.Count);
        var xmlEntity=updated.Result.Single();Assert.AreEqual("second",xmlEntity.Label);
        xmlEntity.Change(99,"too-long-label");
        var denied=await Timed("refuse-invalid",()=>xmlReader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);Assert.AreEqual(version+1,xmlEntity.Version);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result;
        var hidden=await Timed("read-foreign",()=>foreign.ReadAsync(
            foreign.Query<PrivateSetterMappedHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        using var verifier=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var final=await Timed("read-authority",()=>verifier.ReadAsync(
            verifier.Query<PrivateSetterMappedHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(final.IsError,final.Message+final.Exception);Assert.AreEqual(23,final.Result.Single().Score);
        Assert.AreEqual("second",final.Result.Single().Label);Assert.AreEqual(version+1,final.Result.Single().Version);
        return new NativeFixtureJourneyProof(7,2,new Dictionary<string,bool>{
            ["privateScalarAliasWriteRead"]=true,["domainMethodTrackedUpdate"]=true,
            ["nativeMappedPredicateProjection"]=true,["xmlExplicitPrivateSetter"]=true,
            ["privateSetterConstraints"]=true,["foreignScopePreserved"]=true,["getterOnlyMappingRefused"]=true});
    }
}
