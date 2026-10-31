using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class ConditionalHolonReceiptTests
{
    public sealed class ConstructorProbe
    {
        public static int Calls;
        public string Label { get; set; } = "owned receipt probe";
        public ConstructorProbe() { System.Threading.Interlocked.Increment(ref Calls); }
    }

    [TestMethod]
    public void WrongOperationRejectsBeforeConstructingStoredMetadata()
    {
        var authority = NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS;
        var source = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["probe"] = new ConstructorProbe() } };
        var request = new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest(
            new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), authority,
                new[] { new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(source.Id, null) }),
            NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save, new[] { source }, saveChildren: false);
        var plan = ConditionalHolonPlan.Create(request, authority);
        var ack = new Holon { Id = source.Id, Version = 1, VersionId = Guid.NewGuid(), MetaData = source.MetaData };
        CosmicConcurrency.SetProviderRevisionId(ack, authority, Guid.NewGuid());
        var document = JObject.Parse(ConditionalHolonReceipt.Write(new[] { ack }, plan));
        document["operation"] = Guid.NewGuid().ToString("D"); Rehash(document); ConstructorProbe.Calls = 0;
        Assert.ThrowsException<InvalidDataException>(() => ConditionalHolonReceipt.Read(document.ToString(), plan));
        Assert.AreEqual(0, ConstructorProbe.Calls, "Unbound stored values must not construct CLR objects.");
    }
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public void ReplayRetainsExactOriginalMetadataAfterLaterRevision(bool child,bool wide)
    {
        var original=Graph();var selected=child?original.Children[0]:original;
        selected.MetaData["precise"]=wide?(object)1e100:1.0000000000000000000000000001m;
        var version=selected.Version;var json=ConditionalHolonReceipt.Write(new[]{original});
        selected.MetaData["precise"]=0;selected.Version=99;
        var replay=ConditionalHolonReceipt.Read(json)[0];var restored=child?replay.Children[0]:replay;
        Assert.AreEqual(version,restored.Version);
        var value=restored.MetaData["precise"].ToString();
        if(wide)Assert.AreEqual(1e100,double.Parse(value,System.Globalization.CultureInfo.InvariantCulture));
        else Assert.AreEqual(1.0000000000000000000000000001m,decimal.Parse(value,System.Globalization.CultureInfo.InvariantCulture));
        restored.MetaData["precise"]="mutated replay";
        var again=ConditionalHolonReceipt.Read(json)[0];
        Assert.AreEqual(value,(child?again.Children[0]:again).MetaData["precise"].ToString());
    }

    [DataTestMethod][DataRow("{")][DataRow("[]")][DataRow("null")]
    public void MalformedReceiptIsTypedCorruptionNotTransportFailure(string json)
        =>Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(json));
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)]
    public void SelfConsistentInvalidAcknowledgementCannotBind(int field)
    {
        var source=new Holon{Id=Guid.NewGuid()};var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;
        var request=new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest(new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,new[]{new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(source.Id,null)}),NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,new[]{source});
        var plan=ConditionalHolonPlan.Create(request,authority);var ack=new Holon{Id=source.Id,Version=1,VersionId=Guid.NewGuid()};CosmicConcurrency.SetProviderRevisionId(ack,authority,Guid.NewGuid());
        if(field==0)ack.Version=0;else if(field==1)ack.VersionId=Guid.Empty;else if(field==2)CosmicConcurrency.SetProviderRevisionId(ack,authority,Guid.Empty);else if(field==3)CosmicConcurrency.SetProviderRevisionId(ack,authority,ack.VersionId);else ack.PreviousVersionId=Guid.NewGuid();
        Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(ConditionalHolonReceipt.Write(new[]{ack},plan),plan));
    }
    private static void Rehash(JObject document)=>document["digest"]=(string)typeof(ConditionalHolonReceipt).GetMethod("Digest",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.Invoke(null,new object[]{document})!;
    [TestMethod] public void ChangedRecordPayloadFailsIntegrity()
    {
        var doc=JObject.Parse(ConditionalHolonReceipt.Write(new[]{Graph()}));doc["nodes"]![0]!["record"]=doc["nodes"]![0]!["record"]!.Value<string>()!.Replace("acknowledged","corrupted");Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(doc.ToString()));
    }
    [TestMethod] public void ValidReceiptForOtherIdentityCannotBindToRequest()
    {
        var source=Graph();var request=new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest(new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS,new[]{new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(source.Id,null)}),NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,new[]{source},saveChildren:false);
        var plan=ConditionalHolonPlan.Create(request,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS);Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(ConditionalHolonReceipt.Write(new[]{Graph()},plan),plan));
    }
    private static Holon Graph(){var root=new Holon{Id=Guid.NewGuid(),Name="acknowledged",Version=2};root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=4});return root;}
    [TestMethod] public void ReplayIsDetachedAndRetainsAcknowledgedVersions()
    {
        var original=Graph();var json=ConditionalHolonReceipt.Write(new[]{original});original.Name="newer";original.Version=9;original.Children.Clear();
        var replay=ConditionalHolonReceipt.Read(json)[0];Assert.AreEqual("acknowledged",replay.Name);Assert.AreEqual(2,replay.Version);Assert.AreEqual(4,replay.Children[0].Version);Assert.AreEqual(replay.Id,replay.Children[0].ParentHolonId);
        replay.Name="mutated replay";Assert.AreEqual("acknowledged",ConditionalHolonReceipt.Read(json)[0].Name);
    }
    [TestMethod] public void MissingAcknowledgedIdentityRejects()=>Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Write(new[]{new Holon()}));
    [TestMethod] public void MissingReferenceRejects()
    {
        var doc=JObject.Parse(ConditionalHolonReceipt.Write(new[]{Graph()}));doc["nodes"]![0]!["children"]![0]=Guid.NewGuid().ToString();Rehash(doc);
        Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(doc.ToString()));
    }
    [TestMethod] public void RecordIdentityMismatchRejects()
    {
        var doc=JObject.Parse(ConditionalHolonReceipt.Write(new[]{Graph()}));doc["nodes"]![0]!["id"]=Guid.NewGuid().ToString();Rehash(doc);Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(doc.ToString()));
    }
    [TestMethod] public void UnreachableRecordRejects()
    {
        var doc=JObject.Parse(ConditionalHolonReceipt.Write(new[]{Graph()}));doc["nodes"]![0]!["children"]=new JArray();Rehash(doc);Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(doc.ToString()));
    }
}
