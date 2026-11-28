using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    public sealed class XmlComponentHolon:Holon {public XmlAddress? Address {get;set;}}
    public sealed class XmlAddress
    {public string? Zip {get;set;}public XmlGeo? Geo {get;set;}public string Unmapped {get;set;}="constructor default";}
    public sealed class XmlGeo {public int Rank {get;set;}public decimal Price {get;set;}}
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlComponentsBindAliasedNestedValues(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyXmlComponents,
            nativeJourneySuffix:"-xml-components-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyXmlComponents(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        const string xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='ComponentRecord'>
              <id name='Id' generator='assigned'/>
              <component name='Address'>
                <property name='Zip' column='postal_code' type='String' not-null='true' length='5'/>
                <component name='Geo'>
                  <property name='Rank' column='native_rank' type='Int32'/>
                  <property name='Price' column='native_price' type='Decimal' precision='8' scale='2'/>
                </component>
              </component>
            </class></hibernate-mapping>
            """;
        var contracts=new Dictionary<string,Type>{{"ComponentRecord",typeof(XmlComponentHolon)}};
        var builder=new CosmicModelBuilder();
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("column='native_price'","column='native_rank'"),contracts).IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("<component name='Geo'>","<component name='Geo' class='System.IO.FileInfo'>"),contracts).IsError);
        var imported=builder.ImportNHibernateXml(xml,contracts);Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        var model=builder.Build();Assert.IsFalse(model.IsError,model.Message+model.Exception);
        var record=new XmlComponentHolon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,IsActive=true,IsPublic=false,
            Address=new XmlAddress{Zip="90210",Geo=new XmlGeo{Rank=7,Price=12.25m},Unmapped="not a mapped field"}};
        owned.Add(record.Id);
        using(var writer=CosmicSession.Create(manager,adapter,access,model.Result).Result)
        {
            Assert.IsFalse(writer.Add(record).IsError);
            var saved=await Timed("component-insert",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        }
        var id=record.Id;
        using(var raw=CosmicSession.Create(manager,adapter,access).Result)
        {
            var stored=await Timed("component-raw-aliases",()=>raw.Linq<Holon>().Result.Where(row=>row.Id==id).ToCosmicListAsync());
            Assert.IsFalse(stored.IsError,stored.Message+stored.Exception);
            var metadata=stored.Result.Single().MetaData["Address"];
            var token=metadata as JObject??JObject.Parse(System.Text.Json.JsonSerializer.Serialize(metadata));
            Assert.AreEqual("90210",(string?)token["postal_code"]);Assert.IsNull(token["Zip"]);Assert.IsNull(token["Unmapped"]);
            Assert.AreEqual(7,(int)token["Geo"]!["native_rank"]!);Assert.IsNull(token["Geo"]!["Rank"]);
        }
        using var reader=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var projected=await Timed("component-native-projection",()=>reader.Linq<XmlComponentHolon>().Result
            .Where(row=>row.Address!.Zip=="90210"&&row.Address.Geo!.Rank==7)
            .Select(row=>new{row.Id,Zip=row.Address!.Zip,Price=row.Address.Geo!.Price}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual(id,projected.Result.Single().Id);Assert.AreEqual(12.25m,projected.Result[0].Price);
        var loaded=await Timed("component-hydration",()=>reader.Linq<XmlComponentHolon>().Result.Where(row=>row.Id==id).ToCosmicListAsync());
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);var tracked=loaded.Result.Single();
        Assert.AreEqual("90210",tracked.Address!.Zip);Assert.AreEqual(7,tracked.Address.Geo!.Rank);
        Assert.AreEqual("constructor default",tracked.Address.Unmapped);
        tracked.Address.Zip="10000";tracked.Address.Geo.Rank=8;
        var updated=await Timed("component-update",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        using var verifier=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var reread=await Timed("component-updated-hydration",()=>verifier.Linq<XmlComponentHolon>().Result.Where(row=>row.Id==id).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(reread.IsError,reread.Message+reread.Exception);Assert.AreEqual("10000",reread.Result.Single().Address!.Zip);Assert.AreEqual(8,reread.Result[0].Address!.Geo!.Rank);
        tracked.Address.Zip=null;
        var rejected=await Timed("component-required-refusal",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(rejected.IsError);Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",rejected.ErrorCode);
        var stable=await Timed("component-authority-unchanged",()=>verifier.Linq<XmlComponentHolon>().Result.Where(row=>row.Id==id).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(stable.IsError,stable.Message+stable.Exception);Assert.AreEqual("10000",stable.Result.Single().Address!.Zip);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result;
        var hidden=await Timed("component-foreign-policy",()=>foreign.Linq<XmlComponentHolon>().Result.ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        var readonlyBuilder=new CosmicModelBuilder();Assert.IsFalse(readonlyBuilder.ImportNHibernateXml(xml.Replace("name='ComponentRecord'","name='ComponentRecord' mutable='false'"),contracts).IsError);
        using var readOnly=CosmicSession.Create(manager,adapter,access,readonlyBuilder.Build().Result).Result;
        Assert.IsFalse(readOnly.Add(new XmlComponentHolon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Address=new XmlAddress{Zip="12345",Geo=new XmlGeo{Rank=1,Price=1m}}}).IsError);
        var readonlyRefusal=await Timed("component-readonly-refusal",()=>readOnly.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(readonlyRefusal.IsError);Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",readonlyRefusal.ErrorCode);
        return new NativeFixtureJourneyProof(8,2,new Dictionary<string,bool>{
            ["atomicComponentAdmission"]=true,["aliasedNestedStorage"]=true,["nativeNestedPredicateAndProjection"]=true,
            ["declaredMemberHydration"]=true,["nestedUpdateRoundTrip"]=true,["nestedConstraintsPreserveAuthority"]=true,
            ["foreignCallerIsolated"]=true,["readOnlyModelPreserved"]=true});
    }
}
