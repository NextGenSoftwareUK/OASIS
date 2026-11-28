using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    public sealed class XmlPocoOrder {public Guid Id {get;set;}public string Label {get;set;}="";public XmlAddress? Address {get;set;}}
    public sealed class XmlPocoConstructorOrder
    {public string Code {get;}public XmlAddress Address {get;}public XmlPocoConstructorOrder(string code,XmlAddress address){Code=code;Address=address;}}
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativePocoXmlComponentsUseSameNativeValueCodec(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyPocoXmlComponents,
            nativeJourneySuffix:"-poco-xml-components-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyPocoXmlComponents(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        const string xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='PocoOrder'>
              <id name='Id' generator='guid'/>
              <property name='Label' column='order_label' type='String'/>
              <component name='Address'>
                <property name='Zip' column='postal_code' type='String' not-null='true' length='5'/>
                <component name='Geo'>
                  <property name='Rank' column='native_rank' type='Int32'/>
                  <property name='Price' column='native_price' type='Decimal' precision='8' scale='2'/>
                </component>
              </component>
            </class></hibernate-mapping>
            """;
        var builder=new CosmicPocoModelBuilder<XmlPocoOrder>("M04_poco_component_"+Guid.NewGuid().ToString("N"));
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("column='postal_code'","column='Geo'"),"PocoOrder").IsError);
        var imported=builder.ImportNHibernateXml(xml,"PocoOrder");Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        var model=builder.Build();Assert.IsFalse(model.IsError,model.Message+model.Exception);Assert.IsTrue(model.Result.Properties.Contains("Address"));
        using var writer=CosmicPocoContext<XmlPocoOrder>.Create(manager,adapter,access,model.Result).Result;
        var order=new XmlPocoOrder{Label="delivery",Address=new XmlAddress{Zip="90210",Geo=new XmlGeo{Rank=7,Price=12.25m},Unmapped="transient"}};
        Assert.IsFalse(writer.Add(order).IsError);var id=writer.Identity(order).Result;owned.Add(id);Assert.AreEqual(Guid.Empty,order.Id);
        var saved=await Timed("poco-component-insert",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(id,order.Id);
        using var reader=CosmicPocoContext<XmlPocoOrder>.Create(manager,adapter,access,model.Result).Result;
        var projection=await Timed("poco-component-native-leaves",()=>reader.Linq().Result
            .Where(row=>row.Address!.Zip=="90210"&&row.Address.Geo!.Rank==7)
            .Select(row=>new{row.Id,row.Label,Zip=row.Address!.Zip,Price=row.Address.Geo!.Price}).ToCosmicListAsync());
        Assert.IsFalse(projection.IsError,projection.Message+projection.Exception);Assert.AreEqual(id,projection.Result.Single().Id);
        Assert.AreEqual("delivery",projection.Result[0].Label);Assert.AreEqual(12.25m,projection.Result[0].Price);
        var loaded=await Timed("poco-component-find",()=>reader.Linq().Result.Where(row=>row.Id==id).ToCosmicListAsync());
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.AreEqual("90210",loaded.Result.Single().Address!.Zip);
        Assert.AreEqual("constructor default",loaded.Result.Single().Address.Unmapped);
        loaded.Result.Single().Address.Zip="10000";loaded.Result.Single().Address.Geo!.Rank=8;
        var updated=await Timed("poco-component-update",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        using(var verify=CosmicPocoContext<XmlPocoOrder>.Create(manager,adapter,access,model.Result).Result)
        {
            var current=await Timed("poco-component-reread",()=>verify.Linq().Result.Where(row=>row.Id==id).ToCosmicListAsync());
            Assert.IsFalse(current.IsError,current.Message+current.Exception);Assert.AreEqual("10000",current.Result.Single().Address!.Zip);Assert.AreEqual(8,current.Result.Single().Address.Geo!.Rank);
        }
        loaded.Result.Single().Address.Zip=null;
        var refused=await Timed("poco-component-required",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",refused.ErrorCode);
        using(var verify=CosmicPocoContext<XmlPocoOrder>.Create(manager,adapter,access,model.Result).Result)
        {
            var unchanged=await Timed("poco-component-authority-stable",()=>verify.Linq().Result.Where(row=>row.Id==id).ToCosmicListAsync());
            Assert.IsFalse(unchanged.IsError,unchanged.Message+unchanged.Exception);Assert.AreEqual("10000",unchanged.Result.Single().Address!.Zip);
        }
        using var foreign=CosmicPocoContext<XmlPocoOrder>.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result;
        var hidden=await Timed("poco-component-foreign",()=>foreign.Linq().Result.ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        var constructorXml=xml.Replace("name='PocoOrder'","name='ConstructorOrder'").Replace("<id name='Id' generator='guid'/>","<id generator='guid'/>")
            .Replace("name='Label' column='order_label'","name='Code' column='order_code'");
        var cb=new CosmicPocoModelBuilder<XmlPocoConstructorOrder>("M04_poco_ctor_"+Guid.NewGuid().ToString("N"))
            .MaterializeUsing(values=>new XmlPocoConstructorOrder(values.Get<string>("Code"),values.Get<XmlAddress>("Address")));
        Assert.IsFalse(cb.ImportNHibernateXml(constructorXml,"ConstructorOrder").IsError);
        var constructorModel=cb.Build();Assert.IsFalse(constructorModel.IsError,constructorModel.Message+constructorModel.Exception);
        using var cw=CosmicPocoContext<XmlPocoConstructorOrder>.Create(manager,adapter,access,constructorModel.Result).Result;
        var immutable=new XmlPocoConstructorOrder("ctor",new XmlAddress{Zip="54321",Geo=new XmlGeo{Rank=1,Price=1m}});
        Assert.IsFalse(cw.Add(immutable).IsError);var constructorId=cw.Identity(immutable).Result;owned.Add(constructorId);
        var ctorSaved=await Timed("poco-component-constructor-save",()=>cw.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(ctorSaved.IsError,ctorSaved.Message+ctorSaved.Exception);Assert.IsTrue(ctorSaved.IsSaved);
        using var cr=CosmicPocoContext<XmlPocoConstructorOrder>.Create(manager,adapter,access,constructorModel.Result).Result;
        var ctorRead=await Timed("poco-component-constructor-read",()=>cr.Linq().Result.Where(row=>row.Code=="ctor").ToCosmicListAsync());
        Assert.IsFalse(ctorRead.IsError,ctorRead.Message+ctorRead.Exception);Assert.AreEqual(constructorId,cr.Identity(ctorRead.Result.Single()).Result);Assert.AreEqual("ctor",ctorRead.Result.Single().Code);Assert.AreEqual("54321",ctorRead.Result.Single().Address.Zip);
        return new NativeFixtureJourneyProof(8,3,new Dictionary<string,bool>{
            ["atomicOrdinaryComponentMapping"]=true,["generatedKeyAcknowledgement"]=true,["nativeNestedBindingAndScalarProjection"]=true,
            ["plainClassComponentHydration"]=true,["nestedMutationPersisted"]=true,["constraintsPreserveStoredAuthority"]=true,
            ["foreignCallerIsolated"]=true,["constructorFactoryReceivesTypedComponent"]=true});
    }
}
