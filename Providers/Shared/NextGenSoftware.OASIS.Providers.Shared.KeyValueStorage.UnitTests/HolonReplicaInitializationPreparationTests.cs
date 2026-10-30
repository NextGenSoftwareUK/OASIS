using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class HolonReplicaInitializationPreparationTests
{
    [TestMethod]
    public async Task PreparationBindsFrozenSourceOperatorRouteAndPolicyWithoutReturningPrivatePayload()
    {
        var source=new Source();var policy=new Policy();var prepare=Prepare(source,policy);
        var actor=Guid.NewGuid();var operation=Guid.NewGuid();
        var result=await prepare.PrepareAsync(actor,6,operation);
        Assert.IsFalse(result.IsError,result.Message+result.Exception);
        Assert.AreEqual(actor,result.Result.OperatorId);Assert.AreEqual(operation,result.Result.InitializationOperationId);
        Assert.AreEqual(source.Observation.Inspection.OperationId,result.Result.SourceOperationId);
        Assert.AreEqual(source.Observation.PayloadDigest,result.Result.SourcePayloadDigest);
        Assert.AreEqual(2,policy.Calls);Assert.AreEqual(1,source.Reads);Assert.AreEqual(1,source.Peeks);Assert.AreEqual(0,source.Mutations);
        Assert.AreEqual(result.Result.Fingerprint,(await prepare.PrepareAsync(actor,6,operation)).Result.Fingerprint);
        var metadata=System.Text.Json.JsonSerializer.Serialize(result.Result);
        Assert.IsFalse(metadata.Contains("private-source-value"));Assert.IsFalse(metadata.Contains("PayloadJson"));
        Assert.AreNotEqual(result.Result.Fingerprint,(await prepare.PrepareAsync(Guid.NewGuid(),6,operation)).Result.Fingerprint);
    }

    [DataTestMethod]
    [DataRow("read")] [DataRow("approval")] [DataRow("binding")] [DataRow("error")] [DataRow("warning")] [DataRow("null")]
    public async Task AuthorizationIsExplicitAtBothStagesAndBindingChangesInvalidatePreparation(string deny)
    {
        var source=new Source();var policy=new Policy{Deny=deny};
        var result=await Prepare(source,policy).PrepareAsync(Guid.NewGuid(),6,Guid.NewGuid());
        Assert.AreEqual(deny=="binding" ? "COSMIC_REPLICA_INITIALIZATION_POLICY_CHANGED"
            :deny is "error" or "warning" or "null" ? "COSMIC_REPLICA_INITIALIZATION_AUTHORIZATION_FAILED":"COSMIC_REPLICA_INITIALIZATION_DENIED",result.ErrorCode);
        Assert.AreEqual(deny is "approval" or "binding" ? 1:0,source.Reads);Assert.AreEqual(0,source.Mutations);
        if(deny is "error" or "warning")Assert.IsNotNull(result.Exception);
    }

    [DataTestMethod]
    [DataRow("digest")] [DataRow("state")] [DataRow("target")] [DataRow("attempts")]
    [DataRow("error")] [DataRow("native-unknown")] [DataRow("missing-evidence")]
    public async Task UnboundOrUncertainSourceEvidenceNeverProducesAnInitializationIntent(string invalid)
    {
        var source=new Source(invalid);var result=await Prepare(source,new()).PrepareAsync(Guid.NewGuid(),6,Guid.NewGuid());
        Assert.AreEqual("COSMIC_REPLICA_INITIALIZATION_SOURCE_INVALID",result.ErrorCode);
        Assert.AreEqual(0,source.Peeks);Assert.AreEqual(0,source.Mutations);
    }

    [TestMethod]
    public async Task AStaleQueueHeadCannotBePrepared()
    {
        var source=new Source{DifferentHead=true};var result=await Prepare(source,new()).PrepareAsync(Guid.NewGuid(),6,Guid.NewGuid());
        Assert.AreEqual("COSMIC_REPLICA_INITIALIZATION_NOT_HEAD",result.ErrorCode);Assert.AreEqual(0,source.Mutations);
    }

    [TestMethod]
    public async Task PolicyCannotRewriteTheSourceSnapshotItApproves()
    {
        var source=new Source();var result=await Prepare(source,new Policy{Deny="mutate"}).PrepareAsync(Guid.NewGuid(),6,Guid.NewGuid());
        Assert.AreEqual("COSMIC_REPLICA_INITIALIZATION_POLICY_MUTATED_INTENT",result.ErrorCode);
        Assert.AreEqual(0,source.Mutations);
    }

    [TestMethod]
    public async Task CancellationBeforeInspectionDoesNotReadOrMutateSource()
    {
        var source=new Source();using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>Prepare(source,new()).PrepareAsync(Guid.NewGuid(),6,Guid.NewGuid(),cancelled.Token));
        Assert.AreEqual(0,source.Reads);Assert.AreEqual(0,source.Mutations);
    }

    private static HolonReplicaInitializationPreparation Prepare(Source source,Policy policy)=>new("owned-catalog",
        Guid.Parse("c0214c8d-f075-4d78-90c3-3c6a4b94a30c"),"source-v1","owned-mongo-store",ProviderType.MongoDBOASIS,source,
        "target-v1","owned-sqlite-store",ProviderType.SQLLiteDBOASIS,policy);

    private sealed class Policy : IHolonReplicaInitializationPolicy
    {
        public string BindingIdentity {get;private set;}="policy-v1";
        public string Deny;public int Calls;
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicaInitializationAuthorizationContext context,CancellationToken token)
        {
            Calls++;var read=context.Stage==HolonReplicaInitializationAuthorizationStage.ReadHeldSource;
            if(Deny=="null")return Task.FromResult<OASISResult<bool>>(null);
            if(Deny is "error" or "warning")return Task.FromResult(new OASISResult<bool>(true){IsError=Deny=="error",IsWarning=Deny=="warning",Exception=new InvalidOperationException("Controlled authorization failure")});
            if(!read && Deny=="binding")BindingIdentity="policy-v2";
            if(!read && Deny=="mutate")context.Mutation.Roots[0].Name="policy must not replace this";
            return Task.FromResult(new OASISResult<bool>(!(read && Deny=="read" || !read && Deny=="approval")));
        }
    }

    private sealed class Source : IHolonReplicationObligationReader
    {
        public int Reads,Peeks,Mutations;public bool DifferentHead;
        public HolonReplicationObligation Observation {get;}
        public Source(string invalid=null)
        {
            var record=new Holon{Id=Guid.NewGuid(),Name="private-source-value",Version=4,VersionId=Guid.NewGuid(),PreviousVersionId=Guid.NewGuid(),
                CreatedDate=DateTime.UtcNow,CreatedByAvatarId=Guid.NewGuid()};
            CosmicConcurrency.SetProviderRevisionId(record,ProviderType.MongoDBOASIS,Guid.NewGuid());
            var operation=Guid.NewGuid();var payload=CommittedHolonReplicaPayload.Capture(new(operation,"source-v1",ProviderType.MongoDBOASIS,
                record.CreatedByAvatarId,ConditionalHolonOperation.Save,new[]{record}));
            var inspection=new HolonDeliveryInspection(6,operation,invalid=="target" ? "wrong-target":"target-v1",
                invalid=="state" ? HolonDeliveryState.Pending:HolonDeliveryState.ReconciliationRequired,invalid=="attempts" ? 0:1,
                invalid=="error" ? "OTHER_ERROR":"COSMIC_REPLICA_LINEAGE_CONFLICT",null,
                invalid=="missing-evidence" ? null:new HolonDeliveryRecoveryEvidence(invalid=="native-unknown",Array.Empty<string>()));
            Observation=new(inspection,payload,invalid=="digest" ? new string('0',64):Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))));
        }
        public Task<OASISResult<HolonReplicationObligation>> ReadObligationAsync(long sequence,CancellationToken token=default)
        {Reads++;return Task.FromResult(new OASISResult<HolonReplicationObligation>(Observation));}
        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string target,CancellationToken token=default)
        {Peeks++;var old=Observation.Inspection;return Task.FromResult(new OASISResult<HolonDeliveryInspection>(DifferentHead
            ?new HolonDeliveryInspection(7,old.OperationId,target,old.State,old.AttemptCount,old.ErrorCode,null,old.RecoveryEvidence):old));}
        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken token=default)=>throw Mutation();
        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string target,string worker,TimeSpan lease,CancellationToken token=default)=>throw Mutation();
        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan duration,CancellationToken token=default)=>throw Mutation();
        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string code=null,string message=null,DateTime? retry=null,CancellationToken token=default)=>throw Mutation();
        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,HolonDeliveryRecoveryEvidence evidence,string code=null,string message=null,DateTime? retry=null,CancellationToken token=default)=>throw Mutation();
        private Exception Mutation(){Mutations++;return new InvalidOperationException("Read-only preparation must not call another source operation.");}
    }
}
