using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class XmlPlainStock
    {public Guid Id {get;set;}public string Label {get;set;}="";public int Stock {get;set;}public string Display=>Label+Stock;}
    public sealed class XmlAssignedStock {public int Number {get;set;}public string Label {get;set;}="";}
    public sealed class XmlCompositeStock {public string Region {get;set;}="";public int Number {get;set;}public string Label {get;set;}="";}
    public sealed record XmlConstructorStock(Guid Id,string Label,int Stock);
    private const string OrdinaryGuidXml="""
        <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'>
          <class name='XmlStock'>
            <id name='Id' type='Guid' column='Id'><generator class='guid'/></id>
            <property name='Label' type='String' column='xml_label' not-null='true' length='20'/>
            <property name='Stock' type='Int32' column='xml_stock'/>
          </class>
        </hibernate-mapping>
        """;
    private const string OrdinaryAssignedXml="""
        <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'>
          <class name='AssignedStock'>
            <id name='Number' type='Int32' column='account_number'><generator class='assigned'/></id>
            <property name='Label' type='String' column='account_label'/>
          </class>
        </hibernate-mapping>
        """;
    private const string OrdinaryCompositeXml="""
        <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'>
          <class name='CompositeStock'>
            <composite-id><key-property name='Region' type='String' column='region_code'/>
              <key-property name='Number' type='Int32' column='account_number'/></composite-id>
            <property name='Label' type='String' column='account_label'/>
          </class>
        </hibernate-mapping>
        """;
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeOrdinaryClassXmlMappingsUseNativeIdentityAndReadPolicies(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyOrdinaryXmlMapping,
            nativeJourneySuffix:"-ordinary-xml-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyOrdinaryXmlMapping(IOASISStorageProvider provider,
        List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        var contract="M04_"+Guid.NewGuid().ToString("N");var builder=new CosmicPocoModelBuilder<XmlPlainStock>(contract);
        var imported=builder.ImportNHibernateXml(OrdinaryGuidXml,"XmlStock");Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message+built.Exception);var model=built.Result;
        Assert.IsFalse(model.Properties.Contains("Display"));
        var malformed=OrdinaryGuidXml.Replace("xml_stock","wrong_partial_key").Replace("</class>","<many-to-one name='Missing'/></class>");
        Assert.IsTrue(builder.ImportNHibernateXml(malformed,"XmlStock").IsError);
        var dtd="""<!DOCTYPE hibernate-mapping [<!ENTITY ext SYSTEM 'file:///never-read'>]><hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='XmlStock'>&ext;</class></hibernate-mapping>""";
        Assert.IsTrue(builder.ImportNHibernateXml(dtd,"XmlStock").IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(OrdinaryGuidXml,"NotAllowed").IsError);
        using var writer=CosmicPocoContext<XmlPlainStock>.Create(manager,adapter,access,model).Result;
        var entity=new XmlPlainStock{Label="xml start",Stock=4};Assert.IsFalse(writer.Add(entity).IsError);
        var id=writer.Identity(entity).Result;owned.Add(id);Assert.AreEqual(Guid.Empty,entity.Id);
        var saved=await Timed("create-xml-poco",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(id,entity.Id);
        using var reader=CosmicPocoContext<XmlPlainStock>.Create(manager,adapter,access,model).Result;
        var read=await Timed("read-xml-poco",()=>reader.Linq().Result.Where(row=>row.Id==id&&row.Stock==4).ToCosmicSingleAsync());
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual("xml start",read.Result.Label);
        var projected=await Timed("project-xml-poco",()=>reader.Linq().Result.Select(row=>new{row.Label,row.Stock}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual(4,projected.Result.Single().Stock);
        read.Result.Stock=9;read.Result.Label="xml changed";
        var updated=await Timed("update-xml-poco",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var frozen=builder.Build();Assert.IsFalse(frozen.IsError,frozen.Message+frozen.Exception);
        using var preserved=CosmicPocoContext<XmlPlainStock>.Create(manager,adapter,access,frozen.Result).Result;
        var preservedRead=await Timed("read-after-import-refusal",()=>preserved.Linq().Result.Where(row=>row.Stock==9).ToCosmicSingleAsync());
        Assert.IsFalse(preservedRead.IsError,preservedRead.Message+preservedRead.Exception);Assert.AreEqual(id,preservedRead.Result.Id);
        var readOnlyBuilder=new CosmicPocoModelBuilder<XmlPlainStock>(contract);
        var readOnlyImported=readOnlyBuilder.ImportNHibernateXml(OrdinaryGuidXml.Replace("<class name='XmlStock'>","<class name='XmlStock' mutable='false'>"),"XmlStock");
        Assert.IsFalse(readOnlyImported.IsError,readOnlyImported.Message+readOnlyImported.Exception);
        using var readOnly=CosmicPocoContext<XmlPlainStock>.Create(manager,adapter,access,readOnlyBuilder.Build().Result).Result;
        var readOnlyEntity=await Timed("read-immutable-xml-model",()=>readOnly.Linq().Result.ToCosmicSingleAsync());
        Assert.IsFalse(readOnlyEntity.IsError,readOnlyEntity.Message+readOnlyEntity.Exception);readOnlyEntity.Result.Stock=50;
        var denied=await Timed("refuse-immutable-xml-write",()=>readOnly.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);
        var assignedBuilder=new CosmicPocoModelBuilder<XmlAssignedStock>(contract+"_assigned");
        Assert.IsFalse(assignedBuilder.ImportNHibernateXml(OrdinaryAssignedXml,"AssignedStock").IsError);
        using var assigned=CosmicPocoContext<XmlAssignedStock>.Create(manager,adapter,access,assignedBuilder.Build().Result).Result;
        var account=new XmlAssignedStock{Number=42,Label="assigned"};Assert.IsFalse(assigned.Add(account).IsError);owned.Add(assigned.Identity(account).Result);
        var assignedSave=await Timed("create-assigned-xml-key",()=>assigned.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(assignedSave.IsError,assignedSave.Message+assignedSave.Exception);
        var assignedRead=await Timed("lookup-assigned-xml-key",()=>assigned.FindByPrimaryKeyAsync(new object[]{42}));
        Assert.IsFalse(assignedRead.IsError,assignedRead.Message+assignedRead.Exception);Assert.AreSame(account,assignedRead.Result);
        var compositeBuilder=new CosmicPocoModelBuilder<XmlCompositeStock>(contract+"_composite");
        Assert.IsFalse(compositeBuilder.ImportNHibernateXml(OrdinaryCompositeXml,"CompositeStock").IsError);
        using var composite=CosmicPocoContext<XmlCompositeStock>.Create(manager,adapter,access,compositeBuilder.Build().Result).Result;
        var pair=new XmlCompositeStock{Region="west",Number=7,Label="composite"};Assert.IsFalse(composite.Add(pair).IsError);owned.Add(composite.Identity(pair).Result);
        var compositeSave=await Timed("create-composite-xml-key",()=>composite.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(compositeSave.IsError,compositeSave.Message+compositeSave.Exception);
        var compositeRead=await Timed("lookup-composite-xml-key",()=>composite.FindByPrimaryKeyAsync(new object[]{"west",7}));
        Assert.IsFalse(compositeRead.IsError,compositeRead.Message+compositeRead.Exception);Assert.AreSame(pair,compositeRead.Result);
        var ctorBuilder=new CosmicPocoModelBuilder<XmlConstructorStock>(contract+"_ctor")
            .MaterializeUsing(v=>new XmlConstructorStock(v.Id,v.Get<string>("Label"),v.Get<int>("Stock")));
        Assert.IsFalse(ctorBuilder.ImportNHibernateXml(OrdinaryGuidXml,"XmlStock").IsError);
        using var ctor=CosmicPocoContext<XmlConstructorStock>.Create(manager,adapter,access,ctorBuilder.Build().Result).Result;
        var immutable=new XmlConstructorStock(Guid.NewGuid(),"constructor",6);Assert.IsFalse(ctor.Add(immutable).IsError);owned.Add(ctor.Identity(immutable).Result);
        var ctorSaved=await Timed("create-xml-constructor",()=>ctor.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(ctorSaved.IsError,ctorSaved.Message+ctorSaved.Exception);
        using var ctorReader=CosmicPocoContext<XmlConstructorStock>.Create(manager,adapter,access,ctorBuilder.Build().Result).Result;
        var ctorRead=await Timed("read-xml-constructor",()=>ctorReader.Linq().Result.ToCosmicSingleAsync());
        Assert.IsFalse(ctorRead.IsError,ctorRead.Message+ctorRead.Exception);Assert.AreEqual(immutable,ctorRead.Result);
        using var foreign=CosmicPocoContext<XmlPlainStock>.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var hidden=await Timed("foreign-xml-visibility",()=>foreign.Linq().Result.ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        read.Result.Label=new string('x',21);
        var invalid=await Timed("refuse-xml-constraint",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(invalid.IsError);Assert.IsFalse(invalid.IsSaved);
        return new NativeFixtureJourneyProof(10,5,new Dictionary<string,bool>{
            ["secureSharedXmlReader"]=true,["ordinaryScalarAliasesNative"]=true,["generatedGuidAcknowledgement"]=true,
            ["assignedLogicalIdentityNative"]=true,["compositeXmlIdentityNative"]=true,["constructorFactoryMaterialization"]=true,
            ["xmlReadOnlyPolicy"]=true,["atomicImportPreservesModel"]=true,["xmlScalarConstraints"]=true,["callerVisibility"]=true});
    }
}
