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
    public sealed class AnnotatedFieldHolon:Holon
    {
        private string? code="CONSTRUCTOR";
        private int units=77;
        private int getterCalls;
        [CosmicField(nameof(code)),CustomOASISProperty(MetadataKey="field_code")]
        public string? Code {get{getterCalls++;return code==null?null:"DISPLAY:"+code;}}
        [CosmicField(nameof(units)),CustomOASISProperty(MetadataKey="field_units")]
        public int Units=>units;
        public void Change(string? value,int number){code=value;units=number;}
        public string? StoredCode()=>code;
        public int GetterCalls()=>getterCalls;
    }
    public sealed class FluentFieldHolon:Holon
    {
        private string? code="CONSTRUCTOR";
        [CosmicField(nameof(code))] public string? Code=>code;
        public void Change(string? value){code=value;}
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeBackingFieldsUseOneStorageSlotAcrossOrmPaths(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyBackingFieldJourney,nativeJourneySuffix:"-backing-field-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyBackingFieldJourney(
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
        builder.Entity<AnnotatedFieldHolon>().Property(row=>row.Code).IsRequired().HasMaxLength(6);
        builder.Entity<FluentFieldHolon>().Property(row=>row.Code).HasMetadataKey("fluent_code");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;
        builder.Entity<FluentFieldHolon>().Property(row=>row.Code).HasMetadataKey("later_code");
        using var writer=CosmicSession.Create(manager,adapter,access,model).Result;
        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        var annotated=new AnnotatedFieldHolon{Id=Guid.NewGuid(),Name="annotated field",
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        annotated.Change("Alpha",12);owned.Add(annotated.Id);
        var fluent=new FluentFieldHolon{Id=Guid.NewGuid(),Name="fluent field",
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        fluent.Change(null);owned.Add(fluent.Id);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Assert.IsFalse(writer.Add(annotated).IsError);Assert.IsFalse(writer.Add(fluent).IsError);
        var created=await Timed("create-fields",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.AreEqual("Alpha",annotated.StoredCode());Assert.AreEqual(0,annotated.GetterCalls());
        Assert.AreEqual("Alpha",annotated.MetaData["field_code"]);
        Assert.IsTrue(fluent.MetaData.ContainsKey("fluent_code"));Assert.IsFalse(fluent.MetaData.ContainsKey("later_code"));
        Assert.IsNull(fluent.Code);
        var id=annotated.Id;var optionalId=fluent.Id;
        var query=reader.Query<AnnotatedFieldHolon>().Result.Where(row=>row.Id==id && row.Code=="Alpha" && row.Units==12).Result
            .OrderBy(row=>row.Units).Result;
        var read=await Timed("read-fields",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        Assert.AreEqual("Alpha",read.Result[0].StoredCode());Assert.AreEqual(12,read.Result[0].Units);
        Assert.AreEqual(0,read.Result[0].GetterCalls());Assert.AreEqual("DISPLAY:Alpha",read.Result[0].Code);
        var linq=CosmicLinq.Create(manager,adapter,query).Result;
        var selected=await Timed("project-fields",()=>linq.Select(row=>new{row.Code,row.Units}).ToCosmicListAsync());
        Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);Assert.AreEqual(1,selected.Result.Count);
        Assert.AreEqual("Alpha",selected.Result[0].Code);Assert.AreEqual(12,selected.Result[0].Units);
        var optional=reader.Query<FluentFieldHolon>().Result.Where(row=>row.Id==optionalId && row.Code==null).Result;
        var nullRead=await Timed("read-null-field",()=>reader.ReadAsync(optional,trackResults:false));
        Assert.IsFalse(nullRead.IsError,nullRead.Message+nullRead.Exception);Assert.AreEqual(1,nullRead.Result.Count);
        Assert.IsNull(nullRead.Result[0].Code,"A field null must replace its constructor default.");

        annotated.Change("discard",13);
        Assert.AreEqual(CosmicEntityState.Modified,writer.State(annotated).Result);
        var restored=HolonChangeTracking.RejectChanges(annotated,includeChildren:false);
        Assert.IsFalse(restored.IsError,restored.Message+restored.Exception);
        Assert.AreEqual("Alpha",annotated.StoredCode());Assert.AreEqual(12,annotated.Units);
        Assert.AreEqual(0,annotated.GetterCalls());
        var version=annotated.Version;
        annotated.Change("Beta",14);
        var updated=await Timed("update-fields",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(version+1,annotated.Version);Assert.AreEqual(0,annotated.GetterCalls());
        Assert.AreEqual("Beta",annotated.StoredCode());Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(annotated).Result);
        var changed=reader.Query<AnnotatedFieldHolon>().Result.Where(row=>row.Id==id && row.Code=="Beta" && row.Units==14).Result;
        var fresh=await Timed("read-updated-fields",()=>reader.ReadAsync(changed,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.AreEqual("Beta",fresh.Result[0].StoredCode());Assert.AreEqual(14,fresh.Result[0].Units);
        annotated.Change("TOOLONG",14);
        var refused=await Timed("reject-too-long-field",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",refused.ErrorCode);
        Assert.IsFalse(refused.IsSaved);Assert.AreEqual(version+1,annotated.Version);
        annotated.Change("Beta",14);

        fluent.Change("Omega");
        var fluentUpdated=await Timed("update-fluent-field",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(fluentUpdated.IsError,fluentUpdated.Message+fluentUpdated.Exception);Assert.IsTrue(fluentUpdated.IsSaved);
        var fluentQuery=reader.Query<FluentFieldHolon>().Result.Where(row=>row.Id==optionalId && row.Code=="Omega").Result;
        var fluentRead=await Timed("read-fluent-field",()=>reader.ReadAsync(fluentQuery,trackResults:false));
        Assert.IsFalse(fluentRead.IsError,fluentRead.Message+fluentRead.Exception);Assert.AreEqual(1,fluentRead.Result.Count);
        Assert.AreEqual("Omega",fluentRead.Result[0].Code);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var hiddenQuery=foreign.Query<AnnotatedFieldHolon>().Result.Where(row=>row.Id==id && row.Code=="Beta").Result;
        var hidden=await Timed("foreign-fields",()=>foreign.ReadAsync(hiddenQuery,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count);
        Assert.AreEqual(0,annotated.GetterCalls());
        return new NativeFixtureJourneyProof(8,4,new Dictionary<string,bool>{
            ["declaredFieldRoundtrip"]=true,["nativeFieldFilterOrderProjection"]=true,
            ["fieldSnapshotsRestoreWithoutGetter"]=true,["acknowledgedFieldUpdates"]=true,
            ["optionalNullOverridesConstructorDefault"]=true,["immutableFluentFieldAlias"]=true,
            ["constraintsUseFieldValues"]=true,["callerFieldIsolation"]=true});
    }
}
