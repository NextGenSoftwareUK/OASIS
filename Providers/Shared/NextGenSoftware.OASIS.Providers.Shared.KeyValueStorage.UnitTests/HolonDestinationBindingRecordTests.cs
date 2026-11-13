using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonDestinationBindingRecordTests
{
    private static HolonReplicationDestinationBinding Binding()
    {
        var source=new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS,new string('A',64),Guid.NewGuid());
        var target=new CosmicStorageIdentity(ProviderType.MongoDBOASIS,new string('B',64),Guid.NewGuid());
        return new(new HolonReplicationRouteBinding(source,target,"original-policy"),Guid.NewGuid(),Guid.NewGuid());
    }
    [TestMethod]
    public void ApprovalRoundTripRetainsNativeScopeAndRevisionWithoutChangingPolicyProvenance()
    {
        var value=Binding();var json=HolonDestinationBindingRecord.Capture(value);
        var restored=HolonDestinationBindingRecord.Read(json,HolonDestinationBindingRecord.Digest(json),value.Route.SourceStorage,value.Route.TargetStorage.Authority);
        Assert.AreEqual(value.RevisionId,restored.RevisionId);
        Assert.AreEqual(value.EstablishedOperationId,restored.EstablishedOperationId);
        Assert.AreEqual(value.Route.TargetId,restored.Route.TargetId);
        Assert.AreEqual(json,HolonDestinationBindingRecord.Capture(restored));
    }
    [DataTestMethod]
    [DataRow("digest")] [DataRow("source")] [DataRow("target")] [DataRow("version")]
    [DataRow("revision")] [DataRow("unknown")] [DataRow("extra-route")]
    public void ApprovalCannotBeReinterpretedOrSilentlyRepaired(string fault)
    {
        var binding=Binding();var value=JObject.Parse(HolonDestinationBindingRecord.Capture(binding));
        var source=binding.Route.SourceStorage;var target=binding.Route.TargetStorage.Authority;
        switch(fault)
        {
            case "source":source=new(source.Authority,source.EndpointFingerprint,Guid.NewGuid());break;
            case "target":target=ProviderType.Neo4jOASIS;break;
            case "version":value["Format"]=2;break;
            case "revision":value["RevisionId"]=Guid.Empty.ToString("D");break;
            case "unknown":value["unrecognised"]=true;break;
            case "extra-route":((JArray)value["Descriptor"]["Routes"]).Add(value["Descriptor"]["Routes"][0].DeepClone());break;
        }
        var json=value.ToString();var digest=fault=="digest" ? new string('0',64):HolonDestinationBindingRecord.Digest(json);
        Assert.ThrowsException<InvalidDataException>(()=>HolonDestinationBindingRecord.Read(json,digest,source,target));
    }
}
