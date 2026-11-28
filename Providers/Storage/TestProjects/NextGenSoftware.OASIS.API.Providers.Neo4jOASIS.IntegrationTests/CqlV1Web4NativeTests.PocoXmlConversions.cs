using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    public sealed class ConvertedPlainStock
    {
        public Guid Id {get;set;}
        public int Units {get;set;}
        public string Label {get;set;}="";
        public ConvertedCode Code {get;set;}=new();
    }
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeOrdinaryXmlUserTypesShareFluentNativeMapping(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyPocoXmlConversions,
            nativeJourneySuffix:"-poco-xml-conversion-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyPocoXmlConversions(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var watch=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}}
        var units=new CosmicValueConverter<int,long>(x=>checked(x*10L),x=>x%10==0?checked((int)(x/10)):throw new ArgumentException("Invalid scaled units."),CosmicValueConversionSemantics.Ordering);
        var label=new CosmicValueConverter<string,string>(x=>"label:"+x,x=>x.StartsWith("label:",StringComparison.Ordinal)?x.Substring(6):throw new ArgumentException("Invalid label."),CosmicValueConversionSemantics.Ordering);
        var code=new CosmicValueConverter<ConvertedCode,string>(x=>"code:"+x.Value,x=>x.StartsWith("code:",StringComparison.Ordinal)?new ConvertedCode{Value=x.Substring(5)}:throw new ArgumentException("Invalid code."));
        var contract="M04_poco_types_"+Guid.NewGuid().ToString("N");
        var fluent=new CosmicPocoModelBuilder<ConvertedPlainStock>(contract);
        fluent.Property(x=>x.Units).HasConversion(units).HasMetadataKey("stored_units");
        fluent.Property(x=>x.Label).HasConversion(label).HasMetadataKey("stored_label").HasMaxLength(8).IsRequired();
        fluent.Property(x=>x.Code).HasConversion(code).HasMetadataKey("stored_code");
        var fluentModel=fluent.Build();Assert.IsFalse(fluentModel.IsError,fluentModel.Message+fluentModel.Exception);
        const string xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='PlainStock'>
              <id name='Id' type='Guid'><generator class='guid'/></id>
              <property name='Units' type='units' column='stored_units'/>
              <property name='Label' type='label' column='stored_label' length='8' not-null='true'/>
              <property name='Code' type='code' column='stored_code'/>
            </class></hibernate-mapping>
            """;
        var types=new Dictionary<string,CosmicValueConverter>{{"units",units},{"label",label},{"code",code}};
        var mapped=new CosmicPocoModelBuilder<ConvertedPlainStock>(contract);
        var imported=mapped.ImportNHibernateXml(xml,"PlainStock",types);Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        var wrong=new Dictionary<string,CosmicValueConverter>(types){["units"]=label};
        Assert.IsTrue(mapped.ImportNHibernateXml(xml,"PlainStock",wrong).IsError);
        Assert.IsTrue(mapped.ImportNHibernateXml(xml,"PlainStock").IsError);
        types.Clear();var xmlModel=mapped.Build();Assert.IsFalse(xmlModel.IsError,xmlModel.Message+xmlModel.Exception);
        using var writer=CosmicPocoContext<ConvertedPlainStock>.Create(manager,adapter,access,fluentModel.Result).Result;
        var entity=new ConvertedPlainStock{Units=2,Label="sample",Code=new ConvertedCode{Value="A"}};
        Assert.IsFalse(writer.Add(entity).IsError);var id=writer.Identity(entity).Result;owned.Add(id);
        var saved=await Timed("poco-converted-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(id,entity.Id);
        var raw=await Timed("poco-converted-storage",()=>provider.LoadHolonAsync(id));
        Assert.IsFalse(raw.IsError,raw.Message+raw.Exception);
        using var storedUnits=System.Text.Json.JsonDocument.Parse(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.SerializeRuntime(raw.Result.MetaData["stored_units"]));
        Assert.AreEqual(20L,storedUnits.RootElement.GetInt64());
        Assert.AreEqual("label:sample",raw.Result.MetaData["stored_label"].ToString());Assert.AreEqual("code:A",raw.Result.MetaData["stored_code"].ToString());
        using var reader=CosmicPocoContext<ConvertedPlainStock>.Create(manager,adapter,access,xmlModel.Result).Result;
        var wanted=new ConvertedCode{Value="A"};
        var projection=await Timed("poco-converted-native-projection",()=>reader.Linq().Result.Where(x=>x.Units>=2&&x.Code==wanted)
            .OrderBy(x=>x.Units).Select(x=>new{x.Label,x.Units,x.Code}).ToCosmicListAsync());
        Assert.IsFalse(projection.IsError,projection.Message+projection.Exception);Assert.AreEqual("sample",projection.Result.Single().Label);
        Assert.AreEqual(2,projection.Result[0].Units);Assert.AreEqual(wanted,projection.Result[0].Code);
        var loaded=await Timed("poco-converted-roundtrip",()=>reader.Linq().Result.Where(x=>x.Id==id).ToCosmicSingleAsync());
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.AreEqual(wanted,loaded.Result.Code);
        loaded.Result.Units=3;loaded.Result.Label="changed";loaded.Result.Code=new ConvertedCode{Value="B"};
        var updated=await Timed("poco-converted-update",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        using var foreign=CosmicPocoContext<ConvertedPlainStock>.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,xmlModel.Result).Result;
        var hidden=await Timed("poco-converted-foreign",()=>foreign.Linq().Result.Where(x=>x.Id==id).ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        loaded.Result.Label="too long!";
        var refused=await Timed("poco-converted-model-constraint",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refused.IsError);Assert.IsFalse(refused.IsSaved);
        using var preserved=CosmicPocoContext<ConvertedPlainStock>.Create(manager,adapter,access,xmlModel.Result).Result;
        var winner=await Timed("poco-converted-preserved",()=>preserved.Linq().Result.Where(x=>x.Id==id).ToCosmicSingleAsync());
        Assert.IsFalse(winner.IsError,winner.Message+winner.Exception);Assert.AreEqual("changed",winner.Result.Label);Assert.AreEqual(3,winner.Result.Units);Assert.AreEqual("B",winner.Result.Code.Value);
        fluent.Property(x=>x.Units).HasConversion(new CosmicValueConverter<int,long>(x=>x,x=>checked((int)x),CosmicValueConversionSemantics.Ordering));
        using var frozen=CosmicPocoContext<ConvertedPlainStock>.Create(manager,adapter,access,fluentModel.Result).Result;
        var frozenRead=await Timed("poco-converted-frozen-model",()=>frozen.Linq().Result.Where(x=>x.Units==3).ToCosmicSingleAsync());
        Assert.IsFalse(frozenRead.IsError,frozenRead.Message+frozenRead.Exception);Assert.AreEqual(id,frozenRead.Result.Id);
        return new NativeFixtureJourneyProof(9,2,new Dictionary<string,bool>{["fluentAndXmlNativeModel"]=true,["providerStorageRepresentation"]=true,
            ["convertedNativeFilterOrderProjection"]=true,["typedValueRoundtrip"]=true,["convertedTrackedUpdate"]=true,
            ["callerVisibility"]=true,["modelDomainConstraints"]=true,["atomicRegistryImport"]=true,["frozenConverters"]=true});
    }
}
