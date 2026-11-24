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
    public sealed class XmlMappedHolon:Holon
    {
        public int Score {get;set;}
        public string? Caption {get;set;}="DEFAULT";
        public decimal Amount {get;set;}
    }

    private const string XmlModelMapping="""
        <hibernate-mapping xmlns="urn:nhibernate-mapping-2.2">
          <class name="XmlContract" mutable="true">
            <id name="Id" column="Id" type="Guid"><generator class="assigned"/></id>
            <property name="Score" column="xml_score" type="Int32"/>
            <property name="Caption" type="String">
              <column name="xml_caption" not-null="true" length="12"/>
            </property>
            <property name="Amount" column="xml_amount" type="Decimal" precision="6" scale="2"/>
          </class>
        </hibernate-mapping>
        """;

    private static Dictionary<string,Type> XmlModelContracts()=>new(){{"XmlContract",typeof(XmlMappedHolon)}};

    [TestMethod]
    public void XmlMappingsRejectUnsupportedDeclarationsAtomically()
    {
        var builder=new CosmicModelBuilder();
        var unsupported=XmlModelMapping.Replace("</class>","<bag name=\"ignored\"/></class>");
        Assert.IsTrue(builder.ImportNHibernateXml(unsupported,XmlModelContracts()).IsError);
        Assert.IsTrue(builder.Build().IsError,"Failed import must not declare a partial entity.");
        var dtd="<!DOCTYPE hibernate-mapping [<!ENTITY x SYSTEM 'file:///never-read'>]>"
            +XmlModelMapping;
        Assert.IsTrue(builder.ImportNHibernateXml(dtd,XmlModelContracts()).IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(XmlModelMapping.Replace("XmlContract","NotAllowed"),XmlModelContracts()).IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(XmlModelMapping.Replace("xml_score","CreatedByAvatarId"),XmlModelContracts()).IsError);
        Assert.IsTrue(builder.Build().IsError);
        var valid=builder.ImportNHibernateXml(XmlModelMapping,XmlModelContracts());
        Assert.IsFalse(valid.IsError,valid.Message);
        Assert.IsTrue(builder.ImportNHibernateXml(XmlModelMapping,XmlModelContracts()).IsError);
        var model=builder.Build();Assert.IsFalse(model.IsError,model.Message);Assert.AreEqual(1,model.Result.EntityTypes.Count);
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeNHibernateXmlModelsRoundtripThroughSharedOwners(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyNhXmlModelJourney,nativeJourneySuffix:"-nhibernate-xml-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyNhXmlModelJourney(
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
        var imported=builder.ImportNHibernateXml(XmlModelMapping,XmlModelContracts());
        Assert.IsFalse(imported.IsError,imported.Message);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        builder.Entity<XmlMappedHolon>().Property(row=>row.Score).HasMetadataKey("later_score");
        var lockedBuilder=new CosmicModelBuilder();
        var lockedImport=lockedBuilder.ImportNHibernateXml(XmlModelMapping.Replace("mutable=\"true\"","mutable=\"false\""),XmlModelContracts());
        Assert.IsFalse(lockedImport.IsError,lockedImport.Message);
        var locked=lockedBuilder.Build();Assert.IsFalse(locked.IsError,locked.Message);
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new XmlMappedHolon{Id=Guid.NewGuid(),Name="xml mapping",Score=10,Caption="valid",Amount=12.25m,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Assert.IsFalse(writer.Add(entity).IsError);
        var created=await Timed("create-xml-model",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.AreEqual(id,entity.Id);Assert.IsTrue(entity.MetaData.ContainsKey("xml_score"));
        Assert.IsFalse(entity.MetaData.ContainsKey("later_score"));var version=entity.Version;
        using var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var query=reader.Query<XmlMappedHolon>().Result.Where(row=>row.Id==id && row.Score==10).Result;
        var read=await Timed("read-xml-model",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        Assert.AreEqual("valid",read.Result[0].Caption);Assert.AreEqual(12.25m,read.Result[0].Amount);
        entity.Caption=null;
        var required=await Timed("refuse-xml-required",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(required.IsError);Assert.IsFalse(required.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",required.ErrorCode);Assert.AreEqual(version,entity.Version);
        entity.Caption="updated";entity.Score=20;entity.Amount=13.25m;
        var updated=await Timed("update-xml-model",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var changed=reader.Query<XmlMappedHolon>().Result.Where(row=>row.Id==id && row.Score==20 && row.Amount>=13.25m).Result;
        var fresh=await Timed("read-updated-xml-model",()=>reader.ReadAsync(changed,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.AreEqual("updated",fresh.Result[0].Caption);Assert.AreEqual(version+1,fresh.Result[0].Version);
        var source=CosmicLinq.Create(manager,adapter,changed).Result;
        var projected=await Timed("project-xml-model",()=>source.Select(row=>new{row.Caption,row.Amount}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual(1,projected.Result.Count);
        Assert.AreEqual("updated",projected.Result[0].Caption);Assert.AreEqual(13.25m,projected.Result[0].Amount);
        using var readonlySession=CosmicSession.Create(manager,adapter,access,locked.Result).Result;
        var readonlyRows=await Timed("read-xml-readonly",()=>readonlySession.ReadAsync(
            readonlySession.Query<XmlMappedHolon>().Result.Where(row=>row.Id==id).Result));
        Assert.IsFalse(readonlyRows.IsError,readonlyRows.Message+readonlyRows.Exception);Assert.AreEqual(1,readonlyRows.Result.Count);
        readonlyRows.Result[0].Score=99;
        var denied=await Timed("refuse-xml-readonly",()=>readonlySession.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",denied.ErrorCode);
        return new NativeFixtureJourneyProof(6,2,new Dictionary<string,bool>{
            ["xmlMetadataAliasesRoundtrip"]=true,["xmlNativeFilterAndProjection"]=true,
            ["xmlAssignedIdentityAndRevision"]=true,["xmlRequiredRefusedBeforeWrite"]=true,
            ["xmlReadOnlyPolicyEnforced"]=true,["xmlModelSnapshotIndependent"]=true});
    }
}
