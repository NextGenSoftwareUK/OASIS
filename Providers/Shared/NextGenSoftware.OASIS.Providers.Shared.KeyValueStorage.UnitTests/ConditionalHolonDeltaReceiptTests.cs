using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ConditionalHolonDeltaReceiptTests
{
    private static (ConditionalHolonPlan Plan,Holon Ack,HolonCommittedDelta Delta) Fixture(bool values)
    {
        var before=new Holon{Id=Guid.NewGuid(),Name="before",Version=1,VersionId=Guid.NewGuid()};
        CosmicConcurrency.SetProviderRevisionId(before,ProviderType.MongoDBOASIS,Guid.NewGuid());
        var after=new Holon{Id=before.Id,Name="after",Version=2,VersionId=Guid.NewGuid(),PreviousVersionId=before.VersionId};
        CosmicConcurrency.SetProviderRevisionId(after,ProviderType.MongoDBOASIS,Guid.NewGuid());
        var actor=Guid.NewGuid();var operation=Guid.NewGuid();
        var plan=ConditionalHolonPlan.Create(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.MongoDBOASIS,
            new[]{new HolonWriteCondition(after.Id,CosmicConcurrency.GetProviderRevisionId(before,ProviderType.MongoDBOASIS))}),
            ConditionalHolonOperation.Save,new[]{after},saveChildren:false),ProviderType.MongoDBOASIS);
        var delta=HolonCommittedDelta.Capture(operation,actor,plan.Authority,plan.Operation,new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc),new[]{before},new[]{after},values);
        return(plan,after,delta);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void VersionedReceiptRetainsOriginalDeltaAndOldReceiptExplicitlyHasNone(bool values)
    {
        var data=Fixture(values);var json=ConditionalHolonReceipt.Write(new[]{data.Ack},data.Plan,data.Delta);
        data.Ack.Name="later unsaved change";
        var roots=ConditionalHolonReceipt.Read(json,data.Plan,out var replay);
        Assert.AreEqual("after",roots.Single().Name);Assert.AreEqual(data.Delta.StoredJson,replay.StoredJson);
        Assert.AreEqual(data.Delta.SummaryJson,replay.SummaryJson);Assert.AreEqual(values,replay.ValuesRetained);
        var old=ConditionalHolonReceipt.Write(roots,data.Plan);
        Assert.AreEqual(3,JObject.Parse(old).Value<int>("format"));
        Assert.AreEqual("after",ConditionalHolonReceipt.Read(old,data.Plan,out var absent).Single().Name);Assert.IsNull(absent);
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public void CorruptOrReboundDeltaNeverBecomesAnAcknowledgement(int change)
    {
        var data=Fixture(true);var document=JObject.Parse(ConditionalHolonReceipt.Write(new[]{data.Ack},data.Plan,data.Delta));
        using var text=new StringReader(document.Value<string>("delta")!);using var reader=new JsonTextReader(text){DateParseHandling=DateParseHandling.None};
        var delta=JObject.Load(reader);
        switch(change)
        {
            case 0:delta["actor"]=Guid.NewGuid().ToString("D");break;
            case 1:delta["records"]!.First!["committedRevision"]=Guid.NewGuid().ToString("D");break;
            case 2:delta["records"]!.First!["previousVersionId"]=Guid.NewGuid().ToString("D");break;
            case 3:delta["records"]!.First!["previousVersion"]=99;break;
            case 4:delta["operationId"]=Guid.NewGuid().ToString("D");break;
            default:document.Remove("delta");break;
        }
        if(change!=5)document["delta"]=delta.ToString(Formatting.None);
        document["digest"]=(string)typeof(ConditionalHolonReceipt).GetMethod("Digest",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.Invoke(null,new object[]{document})!;
        Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(document.ToString(),data.Plan,out _));
    }
}
