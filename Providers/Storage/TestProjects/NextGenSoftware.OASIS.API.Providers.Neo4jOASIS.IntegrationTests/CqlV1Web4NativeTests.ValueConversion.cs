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
    public sealed record ConvertedCode {public string Value {get;set;}="";}
    public sealed class ConvertedReadingHolon:Holon
    {
        public int Units {get;set;}
        public ConvertedCode? Code {get;set;}
    }

    private const string XmlConvertedValues="""
        <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'>
          <class name='Converted'>
            <property name='Units' column='stored_units' type='scaled-units'/>
            <property name='Code' column='stored_code' type='code-value'/>
          </class>
        </hibernate-mapping>
        """;
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlUserTypesRoundtripFilterProjectAndTrack(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:(p,o,t)=>VerifyValueConversionJourney(p,o,t,true),nativeJourneySuffix:"-xml-user-type-native.json");

    [TestMethod] public void XmlUserTypeRegistryIsExplicitAndAtomic()
    {
        var contracts=new Dictionary<string,Type>{{"Converted",typeof(ConvertedReadingHolon)}};
        var builder=new CosmicModelBuilder();
        var invalid=new Dictionary<string,CosmicValueConverter>{
            ["scaled-units"]=new CosmicValueConverter<string,string>(x=>x,x=>x)};
        Assert.IsTrue(builder.ImportNHibernateXml(XmlConvertedValues,contracts,invalid).IsError);
        Assert.IsTrue(builder.Build().IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(XmlConvertedValues,contracts).IsError);
        Assert.IsTrue(builder.Build().IsError);
        var good=new Dictionary<string,CosmicValueConverter>{
            ["scaled-units"]=new CosmicValueConverter<int,long>(x=>x,x=>checked((int)x),CosmicValueConversionSemantics.Ordering),
            ["code-value"]=new CosmicValueConverter<ConvertedCode,string>(x=>x.Value,x=>new ConvertedCode{Value=x})};
        var canonical=XmlConvertedValues.Replace("name='Units'","name='Name'").Replace("type='scaled-units'","type='code-value'");
        Assert.IsTrue(builder.ImportNHibernateXml(canonical,contracts,good).IsError);Assert.IsTrue(builder.Build().IsError);
        Assert.IsFalse(builder.ImportNHibernateXml(XmlConvertedValues,contracts,good).IsError);
        good.Clear();Assert.IsFalse(builder.Build().IsError,"The imported converter registry must be frozen.");
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeValueConversionsRoundtripFilterProjectAndTrack(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:(p,o,t)=>VerifyValueConversionJourney(p,o,t,false),nativeJourneySuffix:"-value-conversion-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyValueConversionJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings,bool fromXml=false)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var codeConversion=new CosmicValueConverter<ConvertedCode,string>(
            code=>"code:"+code.Value,
            text=>text.StartsWith("code:",StringComparison.Ordinal)
                ?new ConvertedCode{Value=text.Substring(5)}:throw new ArgumentException("Invalid code representation."));
        var unitsConversion=new CosmicValueConverter<int,long>(
            value=>checked((long)value*10),
            value=>value%10==0?checked((int)(value/10)):throw new ArgumentException("Invalid units representation."),
            CosmicValueConversionSemantics.Ordering);
        var builder=new CosmicModelBuilder();
        if(fromXml)
        {
            var types=new Dictionary<string,CosmicValueConverter>{{"scaled-units",unitsConversion},{"code-value",codeConversion}};
            var imported=builder.ImportNHibernateXml(XmlConvertedValues,
                new Dictionary<string,Type>{{"Converted",typeof(ConvertedReadingHolon)}},types);
            Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);types.Clear();
        }
        else
        {
        builder.Entity<ConvertedReadingHolon>().Property(row=>row.Units).HasConversion(unitsConversion).HasMetadataKey("stored_units");
        builder.Entity<ConvertedReadingHolon>().Property(row=>row.Code).HasConversion(codeConversion).HasMetadataKey("stored_code");
        }
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var frozen=built.Result.WithoutQueryFilters();
        // Reconfiguration must not affect an already built model, including its filter-bypass copy.
        builder.Entity<ConvertedReadingHolon>().Property(row=>row.Code)
            .HasConversion(new CosmicValueConverter<ConvertedCode,string>(
                value=>"later:"+value.Value,value=>new ConvertedCode{Value=value}));
        using var writer=CosmicSession.Create(manager,adapter,access,frozen).Result;
        var code=new ConvertedCode{Value="Alpha"};
        var entity=new ConvertedReadingHolon{Id=Guid.NewGuid(),Name="converted reading",Units=2,Code=code,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Assert.IsFalse(writer.Add(entity).IsError);
        var saved=await Timed("create-converted",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.AreEqual(20L,Convert.ToInt64(entity.MetaData["stored_units"]));
        Assert.AreEqual("code:Alpha",entity.MetaData["stored_code"]);
        using var reader=CosmicSession.Create(manager,adapter,access,frozen).Result;
        var queryResult=reader.Query<ConvertedReadingHolon>().Result
            .Where(row=>row.Id==id && row.Units>=2 && row.Code==code);
        Assert.IsFalse(queryResult.IsError,queryResult.Message);
        var ordered=queryResult.Result.OrderBy(row=>row.Units);Assert.IsFalse(ordered.IsError,ordered.Message);
        var read=await Timed("read-converted",()=>reader.ReadAsync(ordered.Result,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        Assert.AreEqual(2,read.Result[0].Units);Assert.AreEqual("Alpha",read.Result[0].Code!.Value);
        var root=CosmicLinq.Create(manager,adapter,ordered.Result).Result;
        var projection=await Timed("project-converted",()=>root.Select(row=>new{row.Units,row.Code})
            .Where(row=>row.Units==2 && row.Code==code).OrderBy(row=>row.Units).ToCosmicListAsync());
        Assert.IsFalse(projection.IsError,projection.Message+projection.Exception);
        Assert.AreEqual(1,projection.Result.Count);Assert.AreEqual(2,projection.Result[0].Units);
        Assert.AreEqual("Alpha",projection.Result[0].Code!.Value);
        var combined=await Timed("set-converted",()=>root.Union(root).Where(row=>row.Units==2 && row.Code==code).ToCosmicListAsync());
        Assert.IsFalse(combined.IsError,combined.Message+combined.Exception);Assert.AreEqual(1,combined.Result.Count);
        Assert.AreEqual(2,combined.Result[0].Units);Assert.AreEqual("Alpha",combined.Result[0].Code!.Value);
        var codes=new[]{code,new ConvertedCode{Value="Other"}};
        var membership=reader.Query<ConvertedReadingHolon>().Result.Where(row=>row.Id==id && codes.Contains(row.Code!));
        Assert.IsFalse(membership.IsError,membership.Message);
        var memberRows=await Timed("membership-converted",()=>reader.ReadAsync(membership.Result,trackResults:false));
        Assert.IsFalse(memberRows.IsError,memberRows.Message);Assert.AreEqual(1,memberRows.Result.Count);
        var forbidden=reader.Query<ConvertedReadingHolon>().Result.OrderBy(row=>row.Code);
        Assert.IsTrue(forbidden.IsError,"An equality-only conversion must not invent an ordering contract.");
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,frozen).Result;
        var foreignQuery=foreign.Query<ConvertedReadingHolon>().Result.Where(row=>row.Id==id && row.Code==code);
        Assert.IsFalse(foreignQuery.IsError,foreignQuery.Message);
        var hidden=await Timed("foreign-converted",()=>foreign.ReadAsync(foreignQuery.Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count);
        var version=entity.Version;
        entity.Units=3;entity.Code!.Value="Beta";
        Assert.AreEqual(CosmicEntityState.Modified,writer.State(entity).Result);
        var updated=await Timed("update-converted",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(version+1,entity.Version);
        var beta=new ConvertedCode{Value="Beta"};
        var freshQuery=reader.Query<ConvertedReadingHolon>().Result.Where(row=>row.Id==id && row.Units==3 && row.Code==beta);
        Assert.IsFalse(freshQuery.IsError,freshQuery.Message);
        var fresh=await Timed("read-updated-converted",()=>reader.ReadAsync(freshQuery.Result,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.AreEqual(3,fresh.Result[0].Units);Assert.AreEqual("Beta",fresh.Result[0].Code!.Value);
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(entity).Result);
        return new NativeFixtureJourneyProof(8,2,new Dictionary<string,bool>{
            ["convertedStorageRepresentation"]=true,["typedConversionRoundtrip"]=true,
            ["nativeConvertedFilterAndOrder"]=true,["nativeConvertedProjectionAndMembership"]=true,["convertedSetModelPreserved"]=true,
            ["convertedCallerIsolation"]=true,["convertedMutableUpdateAcknowledged"]=true,
            ["immutableConverterModelAndFilterBypass"]=true});
    }
}
