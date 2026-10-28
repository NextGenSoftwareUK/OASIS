using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationRouteDescriptorTests
{
    private static HolonReplicationRouteBinding[] Routes()
    {
        var source=new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS,new string('A',64),Guid.NewGuid());
        return new[] {
            new HolonReplicationRouteBinding(source,new(ProviderType.MongoDBOASIS,new string('B',64),Guid.NewGuid()),"policy-one"),
            new HolonReplicationRouteBinding(source,new(ProviderType.Neo4jOASIS,new string('C',64),"opaque-native-id:α"),"policy-one") };
    }
    private static CommittedHolonReplicaRequest Request(HolonReplicationRouteBinding[] routes,bool descriptors=true)
    {
        var row=new Holon { Id=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid(),Name="committed" };
        CosmicConcurrency.SetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
        return new(Guid.NewGuid(),routes[0].SourceId,ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),
            ConditionalHolonOperation.Save,new[]{row},false,true,0,descriptors ? routes : null);
    }

    [TestMethod]
    public void DescriptorRoundTripIsCanonicalAndEnrollmentOwnsItsCompleteFrozenRouteSet()
    {
        var routes=Routes();
        var mutable=routes.Reverse().ToList();
        var enrollment=new HolonReplicationEnrollment(routes[0].SourceId,routes.Select(route=>route.TargetId),routes[0].SourceStorage,mutable);
        mutable.Clear();
        Assert.AreEqual(2,enrollment.NativeRoutes.Count);
        var json=HolonReplicationRouteDescriptors.Capture(enrollment.NativeRoutes);
        Assert.AreEqual(json,HolonReplicationRouteDescriptors.Capture(routes.Reverse()));
        var restored=HolonReplicationRouteDescriptors.Read(json);
        Assert.AreEqual(json,HolonReplicationRouteDescriptors.Capture(restored));
        CollectionAssert.AreEqual(routes.Select(route=>route.TargetId).OrderBy(id=>id,StringComparer.Ordinal).ToArray(),restored.Select(route=>route.TargetId).ToArray());
        Assert.IsTrue(restored.Any(route=>route.TargetStorage.StoreId=="opaque-native-id:α"));
        Assert.ThrowsException<NotSupportedException>(()=>((IList<HolonReplicationRouteBinding>)enrollment.NativeRoutes).Clear());
        Assert.ThrowsException<ArgumentException>(()=>new HolonReplicationEnrollment(routes[0].SourceId,new[]{routes[0].TargetId},routes[0].SourceStorage,routes));
        Assert.ThrowsException<ArgumentException>(()=>new HolonReplicationEnrollment("different-source",routes.Select(route=>route.TargetId),routes[0].SourceStorage,routes));
    }

    [DataTestMethod]
    [DataRow("duplicate")] [DataRow("missing")] [DataRow("unknown")] [DataRow("wrong-token")]
    [DataRow("authority")] [DataRow("version")] [DataRow("mixed-source")] [DataRow("empty")]
    [DataRow("policy")] [DataRow("fingerprint")]
    public void DescriptorParserRejectsAmbiguousOrIncompleteNativeMeaning(string fault)
    {
        var document=JObject.Parse(HolonReplicationRouteDescriptors.Capture(Routes()));
        var items=(JArray)document["Routes"];
        switch(fault)
        {
            case "duplicate":items.Add(items[0].DeepClone());break;
            case "missing":((JObject)items[0]).Remove("Target");break;
            case "unknown":items[0]["secretConnectionString"]="forbidden";break;
            case "wrong-token":items[0]["Target"]["StoreId"]=123;break;
            case "authority":items[0]["Target"]["Authority"]="2";break;
            case "version":document["Format"]=2;break;
            case "mixed-source":items[1]["Source"]["StoreId"]=Guid.NewGuid().ToString("D");break;
            case "empty":items.Clear();break;
            case "policy":items[0]["PolicyBinding"]=" ";break;
            case "fingerprint":items[0]["Target"]["EndpointFingerprint"]="not-a-fingerprint";break;
        }
        Assert.ThrowsException<InvalidDataException>(()=>HolonReplicationRouteDescriptors.Read(document.ToString()));
    }

    [TestMethod]
    public void VersionTwoEnvelopeRetainsNativeRoutesAndRefusesUnenrolledTargetsOrUnboundApply()
    {
        var routes=Routes();var request=Request(routes);
        var json=CommittedHolonReplicaPayload.Capture(request);
        Assert.AreEqual(2,JObject.Parse(json)["Format"].Value<int>());
        var restored=CommittedHolonReplicaPayload.ReadForTarget(json,routes[0].TargetId);
        Assert.AreEqual(2,restored.NativeRoutes.Count);
        Assert.AreEqual(HolonReplicationRouteDescriptors.Capture(routes),HolonReplicationRouteDescriptors.Capture(restored.NativeRoutes));
        Assert.AreEqual(CommittedHolonReplicaPlan.Create(request,ProviderType.MongoDBOASIS).Fingerprint,
            CommittedHolonReplicaPlan.Create(restored,ProviderType.MongoDBOASIS).Fingerprint);
        Assert.ThrowsException<InvalidDataException>(()=>CommittedHolonReplicaPayload.ReadForTarget(json,"not-enrolled"));
        Assert.ThrowsException<ArgumentException>(()=>CommittedHolonReplicaPlan.ValidateNativeDestination(restored,null));
        Assert.ThrowsException<ArgumentException>(()=>CommittedHolonReplicaPlan.ValidateNativeDestination(restored,
            new CosmicStorageIdentity(ProviderType.MongoDBOASIS,routes[0].TargetStorage.EndpointFingerprint,Guid.NewGuid())));
        CommittedHolonReplicaPlan.ValidateNativeDestination(restored,routes[0].TargetStorage);
        var legacy=Request(routes,descriptors:false);
        var old=CommittedHolonReplicaPayload.Capture(legacy);
        Assert.AreEqual(1,JObject.Parse(old)["Format"].Value<int>());
        Assert.IsNull(JObject.Parse(old)["RouteDescriptors"]);
        Assert.AreEqual(0,CommittedHolonReplicaPayload.Read(old).NativeRoutes.Count);
    }

    [DataTestMethod]
    [DataRow("missing")] [DataRow("null")] [DataRow("version")] [DataRow("source")] [DataRow("extra")]
    public void NewEnvelopeNeverDowngradesAnInvalidDescriptorToLegacyTransport(string fault)
    {
        var document=JObject.Parse(CommittedHolonReplicaPayload.Capture(Request(Routes())));
        switch(fault)
        {
            case "missing":document.Remove("RouteDescriptors");break;
            case "null":document["RouteDescriptors"]=null;break;
            case "version":document["RouteDescriptors"]["Format"]=99;break;
            case "source":document["SourceId"]="different-source";break;
            case "extra":document["unrecognised"]=true;break;
        }
        Assert.ThrowsException<InvalidDataException>(()=>CommittedHolonReplicaPayload.Read(document.ToString()));
    }

    [TestMethod]
    public void OperationFingerprintsDistinguishDescriptorBearingAndOlderIntent()
    {
        var routes=Routes();var native=Request(routes);
        var legacy=new CommittedHolonReplicaRequest(native.OperationId,native.SourceId,native.SourceAuthority,native.ActorId,
            native.Operation,native.Roots,native.SaveChildren,native.Recursive,native.MaxChildDepth);
        Assert.AreNotEqual(CommittedHolonReplicaPlan.Create(native,ProviderType.MongoDBOASIS).Fingerprint,
            CommittedHolonReplicaPlan.Create(legacy,ProviderType.MongoDBOASIS).Fingerprint);
        var oldEnrollment=new HolonReplicationEnrollment(native.SourceId,routes.Select(route=>route.TargetId),routes[0].SourceStorage);
        var newEnrollment=new HolonReplicationEnrollment(native.SourceId,routes.Select(route=>route.TargetId),routes[0].SourceStorage,routes);
        var write=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(native.OperationId,native.ActorId,native.SourceAuthority,
            native.Roots.Select(root=>new HolonWriteCondition(root.Id,null))),ConditionalHolonOperation.Save,native.Roots,false);
        Assert.AreNotEqual(ConditionalHolonPlan.CreateReplicating(write,native.SourceAuthority,oldEnrollment).Fingerprint,
            ConditionalHolonPlan.CreateReplicating(write,native.SourceAuthority,newEnrollment).Fingerprint);
    }

    private sealed class ForbiddenReceiver : ICommittedHolonReplicaStorageProvider
    {
        public int Calls;
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        {Calls++;throw new InvalidOperationException("Unbound receiver must not be invoked.");}
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,CancellationToken token=default)
        {Calls++;throw new InvalidOperationException("Unbound receiver must not be invoked.");}
    }
    [TestMethod]
    public async Task DescriptorBearingPayloadCannotUseTheExplicitUnboundReceiverAdapter()
    {
        var routes=Routes();var request=Request(routes);var receiver=new ForbiddenReceiver();
        var target=new CommittedHolonReplicaTarget(routes[0].TargetId,receiver,ProviderType.MongoDBOASIS,request.SourceId,request.SourceAuthority);
        var result=await target.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.CaptureItem(request),default);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("COSMIC_REPLICA_PAYLOAD_INVALID",result.ErrorCode);
        Assert.AreEqual(0,receiver.Calls);
    }
}
