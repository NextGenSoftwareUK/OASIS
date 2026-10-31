using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class ConditionalHolonPlanTests
{
    [TestMethod]
    public void CanonicalFingerprintBindsEquivalentJsonNumbersWithoutMutatingInput()
    {
        var original=new Newtonsoft.Json.Linq.JObject{["number"]=1m,["other"]="kept"};var before=original.DeepClone();
        var equivalent=new Newtonsoft.Json.Linq.JObject{["other"]="kept",["number"]=new Newtonsoft.Json.Linq.JRaw("1.00")};
        Assert.AreEqual(ConditionalHolonPlan.ComputeFingerprint(original),ConditionalHolonPlan.ComputeFingerprint(equivalent));
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(before,original));
    }
    [TestMethod]
    public void CanonicalFingerprintRequiresCapturedContent()
        =>Assert.ThrowsException<ArgumentNullException>(()=>ConditionalHolonPlan.ComputeFingerprint(null));

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void DistinctExactDecimalMetadataMustBindDifferentIntent(bool child)
    {
        var root=new Holon{Id=Guid.NewGuid()};var nested=new Holon{Id=Guid.NewGuid()};root.Children.Add(nested);
        var source=child?nested:root;source.MetaData["precise"]=1.0000000000000000000000000001m;
        var authority=ProviderType.OracleDBOASIS;
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,
            new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(nested.Id,null)}),ConditionalHolonOperation.Save,new[]{root});
        var original=ConditionalHolonPlan.Create(request,authority);
        source.MetaData["precise"]=1.0000000000000000000000000002m;
        Assert.AreNotEqual(original.Fingerprint,ConditionalHolonPlan.Create(request,authority).Fingerprint,
            "Different decimal intents must never match an existing operation receipt.");
        Assert.AreEqual(1.0000000000000000000000000001m,Convert.ToDecimal(original.Nodes[child?1:0].Snapshot.MetaData["precise"]));
    }
    [TestMethod]
    public void DecimalScaleDoesNotChangeLogicalIntent()
    {
        var root=new Holon{Id=Guid.NewGuid(),MetaData=new(){["amount"]=1.0m}};var request=Request(root,Guid.NewGuid());
        var original=ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint;
        root.MetaData["amount"]=1.00m;
        Assert.AreEqual(original,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void JsonElementAndClrNumericValuesBindTheSameIntent(bool wide)
    {
        using var json=System.Text.Json.JsonDocument.Parse(wide?"1e100":"1.00");
        var root=new Holon{Id=Guid.NewGuid(),MetaData=new(){["amount"]=wide?(object)1e100:1.0m}};
        var request=Request(root,Guid.NewGuid());var original=ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint;
        root.MetaData["amount"]=json.RootElement.Clone();
        Assert.AreEqual(original,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint);
    }
    [TestMethod]
    public void WideDoubleDifferenceIsStillBoundWithoutDecimalOverflow()
    {
        var root=new Holon{Id=Guid.NewGuid(),MetaData=new(){["amount"]=1e100}};var request=Request(root,Guid.NewGuid());
        var original=ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint;
        root.MetaData["amount"]=double.BitIncrement(1e100);
        Assert.AreNotEqual(original,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint);
    }
    [TestMethod]
    public void PreciseMetadataReorderingDoesNotChangeBoundIntent()
    {
        var root=new Holon{Id=Guid.NewGuid(),MetaData=new(){["amount"]=1.0000000000000000000000000001m,["note"]="same"}};
        var request=Request(root,Guid.NewGuid());var original=ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint;
        root.MetaData=new(){["note"]="same",["amount"]=1.0000000000000000000000000001m};
        Assert.AreEqual(original,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint);
    }
    [DataTestMethod]
    [DataRow("source")] [DataRow("target")] [DataRow("plain")] [DataRow("reordered")]
    public void CapablePlanBindsReplicationEnrollmentWithoutChangingUnenrolledIntent(string changed)
    {
        var root=new Holon{Id=Guid.NewGuid()};var authority=ProviderType.MongoDBOASIS;
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,
            new[]{new HolonWriteCondition(root.Id,null)}),ConditionalHolonOperation.Save,new[]{root});
        var baseline=ConditionalHolonPlan.CreateReplicating(request,authority,new HolonReplicationEnrollment("source",new[]{"a","b"}));
        var actual=changed=="plain" ? ConditionalHolonPlan.Create(request,authority)
            :ConditionalHolonPlan.CreateReplicating(request,authority,new HolonReplicationEnrollment(changed=="source" ? "other":"source",
                changed=="target" ? new[]{"a","c"}:new[]{"b","a"}));
        if(changed=="reordered")Assert.AreEqual(baseline.Fingerprint,actual.Fingerprint);else Assert.AreNotEqual(baseline.Fingerprint,actual.Fingerprint);
    }

    [TestMethod]
    public void ExplicitReplicationPlanRejectsDisagreeingEmbeddedEnrollment()
    {
        var root=new Holon{Id=Guid.NewGuid()};var authority=ProviderType.MongoDBOASIS;
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,
            new[]{new HolonWriteCondition(root.Id,null)},new HolonReplicationEnrollment("source",new[]{"a"})),ConditionalHolonOperation.Save,new[]{root});
        Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonPlan.CreateReplicating(request,authority,new HolonReplicationEnrollment("other",new[]{"a"})));
        Assert.ThrowsException<ArgumentNullException>(()=>ConditionalHolonPlan.CreateReplicating(request,authority,null));
    }

    [TestMethod]
    public void PlainConditionalPlanCannotDiscardRequiredReplicationEnrollment()
    {
        var root=new Holon{Id=Guid.NewGuid()};var authority=ProviderType.MongoDBOASIS;
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,
            new[]{new HolonWriteCondition(root.Id,null)},new HolonReplicationEnrollment("source",new[]{"target"})),
            ConditionalHolonOperation.Save,new[]{root});
        Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonPlan.Create(request,authority));
    }

    [TestMethod]
    public void AuthorityMetadataOtherThanAssignedRevisionRemainsBound()
    {
        var authority=ProviderType.OracleDBOASIS;var root=new Holon{Id=Guid.NewGuid()};
        CosmicConcurrency.SetProviderRevisionId(root,authority,Guid.NewGuid());
        root.ProviderMetaData[authority]["caller-field"]="before";
        var request=Request(root,Guid.NewGuid());var before=ConditionalHolonPlan.Create(request,authority).Fingerprint;
        root.ProviderMetaData[authority]["caller-field"]="after";
        Assert.AreNotEqual(before,ConditionalHolonPlan.Create(request,authority).Fingerprint);
    }
    [TestMethod]
    public void MetadataDictionaryInsertionOrderDoesNotChangeIntent()
    {
        var authority=ProviderType.OracleDBOASIS;var root=new Holon{Id=Guid.NewGuid()};
        CosmicConcurrency.SetProviderRevisionId(root,authority,Guid.NewGuid());
        var fields=root.ProviderMetaData[authority];fields["alpha"]="one";fields["omega"]="two";
        var request=Request(root,Guid.NewGuid());var before=ConditionalHolonPlan.Create(request,authority).Fingerprint;
        fields.Remove("alpha");fields.Add("alpha","one");
        Assert.AreEqual(before,ConditionalHolonPlan.Create(request,authority).Fingerprint);
        Assert.AreEqual("one",fields["alpha"]); // Planning must not strip caller metadata.
        Assert.IsTrue(fields.ContainsKey(CosmicConcurrency.ProviderRevisionMetadataKey));
    }
    [TestMethod]
    public void ForeignRevisionForwardedUnchangedIsBoundToIntent()
    {
        var root=new Holon{Id=Guid.NewGuid()};var request=Request(root,Guid.NewGuid());var authority=ProviderType.OracleDBOASIS;
        CosmicConcurrency.SetProviderRevisionId(root,ProviderType.MongoDBOASIS,Guid.NewGuid());var before=ConditionalHolonPlan.Create(request,authority).Fingerprint;
        CosmicConcurrency.SetProviderRevisionId(root,ProviderType.MongoDBOASIS,Guid.NewGuid());Assert.AreNotEqual(before,ConditionalHolonPlan.Create(request,authority).Fingerprint);
    }
    [DataTestMethod][DataRow(true)][DataRow(false)]
    public void OwnRevisionPublicationKeepsIntentWithNullOrEmptyMetadata(bool missing)
    {
        var root=new Holon{Id=Guid.NewGuid()};if(missing)root.ProviderMetaData=null;var request=Request(root,Guid.NewGuid());var authority=ProviderType.OracleDBOASIS;var before=ConditionalHolonPlan.Create(request,authority).Fingerprint;
        CosmicConcurrency.SetProviderRevisionId(root,authority,Guid.NewGuid());Assert.AreEqual(before,ConditionalHolonPlan.Create(request,authority).Fingerprint);
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void OnlyAuthorityAssignedCurrentKeyIsExcluded(int field)
    {
        var root=new Holon{Id=Guid.NewGuid()};var request=Request(root,Guid.NewGuid());var authority=ProviderType.OracleDBOASIS;
        if(field==1)root.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="foreign-a";if(field==2)root.PreviousVersionProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="previous-a";
        var before=ConditionalHolonPlan.Create(request,authority).Fingerprint;
        if(field==0)root.ProviderUniqueStorageKey[authority]=root.Id.ToString();else if(field==1)root.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="foreign-b";else root.PreviousVersionProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="previous-b";
        var after=ConditionalHolonPlan.Create(request,authority).Fingerprint;if(field==0)Assert.AreEqual(before,after);else Assert.AreNotEqual(before,after);
    }
    private static ConditionalHolonWriteRequest Request(Holon root,Guid actor,Guid? revision=null,ConditionalHolonOperation operation=ConditionalHolonOperation.Save)
        =>new(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.OracleDBOASIS,new[]{new HolonWriteCondition(root.Id,revision)}),operation,new[]{root});
    [TestMethod] public void MissingSelectedChildConditionRejects()
    {
        var root=new Holon{Id=Guid.NewGuid()};root.Children.Add(new Holon{Id=Guid.NewGuid()});
        Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonPlan.Create(Request(root,Guid.NewGuid()),ProviderType.OracleDBOASIS));
    }
    [TestMethod] public void OtherAuthorityRejects()=>Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonPlan.Create(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid()),ProviderType.SQLLiteDBOASIS));
    [TestMethod] public void DeleteCreateConditionRejects()=>Assert.ThrowsException<ArgumentException>(()=>ConditionalHolonPlan.Create(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid(),operation:ConditionalHolonOperation.HardDelete),ProviderType.OracleDBOASIS));
    [TestMethod] public void EmptyLegacyRevisionDiffersFromCreateOnly()
    {
        var root=new Holon{Id=Guid.NewGuid()};var actor=Guid.NewGuid();
        Assert.AreNotEqual(ConditionalHolonPlan.Create(Request(root,actor),ProviderType.OracleDBOASIS).Fingerprint,ConditionalHolonPlan.Create(Request(root,actor,Guid.Empty),ProviderType.OracleDBOASIS).Fingerprint);
    }
    [TestMethod] public void ContentAndActorBoundButPersistencePublicationExcluded()
    {
        var root=new Holon{Id=Guid.NewGuid(),Name="before"};var actor=Guid.NewGuid();var request=Request(root,actor);var original=ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS);
        root.Version=7;root.VersionId=Guid.NewGuid();Assert.AreEqual(original.Fingerprint,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint);
        root.Name="after";Assert.AreNotEqual(original.Fingerprint,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint);Assert.AreEqual("before",original.Nodes[0].Snapshot.Name);
        Assert.AreNotEqual(original.Fingerprint,ConditionalHolonPlan.Create(Request(new Holon{Id=root.Id,Name="before"},Guid.NewGuid()),ProviderType.OracleDBOASIS).Fingerprint);
    }
}
